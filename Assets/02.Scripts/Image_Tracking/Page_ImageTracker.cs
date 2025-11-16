using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// [A그룹 전용]
/// A그룹(페이지)의 배타적, 지연 비활성화, 풀링 로직을 담당합니다.
/// 씬에 하나만 존재해야 합니다. (예: XROrigin)
/// </summary>
public class Page_ImageTracker : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;
    [SerializeField]
    private List<ImagePrefabEntry> pagePrefabs;

    // --- 프리팹 원본 딕셔너리 ---
    private readonly Dictionary<string, GameObject> _pagePrefabDict = new Dictionary<string, GameObject>();
    
    // --- 오브젝트 풀(Pool) ---
    private readonly Dictionary<string, GameObject> _pooledPageObjects = new Dictionary<string, GameObject>();

    // --- A 그룹 상태 변수 ---
    private GameObject _activePageObject = null;
    private string _activePageImageName = null;
    
    [Serializable]
    public struct ImagePrefabEntry
    {
        public string imageName; // XRReferenceImageLibrary의 이름과 일치해야 함
        public GameObject prefab;
    }

    #region --- 유니티 생명주기 ---
    void Awake()
    {
        InitializePrefabDictionaries();
    }

    void OnEnable()
    {
        if (trackedImageManager == null)
        {
            trackedImageManager = FindAnyObjectByType<ARTrackedImageManager>();
            Debug.LogWarning("[GroupA_Tracker] ARTrackedImageManager가 null이라 자동으로 찾았습니다.");
        }
        
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }
        DeactivateAllGroupAObjects();
    }

    private void DeactivateAllGroupAObjects()
    {
         foreach (var entry in _pooledPageObjects)
        {
            if (entry.Value != null) entry.Value.SetActive(false);
        }
        _activePageObject = null;
        _activePageImageName = null;
    }

    private void InitializePrefabDictionaries()
    {
        _pagePrefabDict.Clear();
        foreach (var entry in pagePrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName)) { Debug.LogWarning("[Tracker_A] Group A에 이름이 빈 Prefab이 있습니다."); continue; }
            if (entry.prefab == null) { Debug.LogWarning($"[Tracker_A] Group A '{entry.imageName}'에 Prefab이 없습니다."); continue; }
            if (_pagePrefabDict.ContainsKey(entry.imageName)) { Debug.LogWarning($"[Tracker_A] Group A에 '{entry.imageName}' 이름이 중복됩니다."); continue; }
            _pagePrefabDict.Add(entry.imageName, entry.prefab);
        }
        _pooledPageObjects.Clear();
    }
    #endregion

    #region --- AR 이벤트 핸들러 및 A그룹 로직 ---

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        ARTrackedImage activeACandidate = null;

        // Added 리스트
        foreach (var newImage in eventArgs.added)
        {
            (string imageName, bool isGroupA) = GetImageGroup(newImage);
            if (isGroupA && newImage.trackingState == TrackingState.Tracking)
            {
                activeACandidate = newImage; // 후보 등록
            }
        }

        // Updated 리스트
        foreach (var updatedImage in eventArgs.updated)
        {
            (string imageName, bool isGroupA) = GetImageGroup(updatedImage);
            if (isGroupA && updatedImage.trackingState == TrackingState.Tracking)
            {
                activeACandidate = updatedImage; // 후보 덮어쓰기 (가장 마지막 것)
            }
        }

        // A그룹 최종 후보 처리
        UpdateActiveAObject(activeACandidate);

        // Removed 리스트
        foreach (var removedEntry in eventArgs.removed)
        {
            ARTrackedImage removedImage = removedEntry.Value;
            if (removedImage != null)
            {
                HandleRemovedImage(removedImage);
            }
        }
    }

    private (string imageName, bool isGroupA) GetImageGroup(ARTrackedImage trackedImage)
    {
        if (trackedImage == null || trackedImage.referenceImage == null) return (null, false);
        string imageName = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(imageName)) return (null, false);

        if (_pagePrefabDict.ContainsKey(imageName))
        {
            return (imageName, true); // Group A
        }
        
        return (null, false); // Not Group A
    }

    private void UpdateActiveAObject(ARTrackedImage candidate)
    {
        if (candidate == null) return; // 추적 중인 A 이미지가 없으면 지연 비활성화

        string candidateName = candidate.referenceImage.name;

        // 같은 이미지면 위치만 업데이트
        if (_activePageObject != null && _activePageImageName == candidateName)
        {
            _activePageObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activePageObject.SetActive(true);
            return;
        }

        // 다른 이미지로 교체 (A1 -> A2)
        if (_activePageObject != null)
        {
            Debug.Log($"[Group A] Swapping from '{_activePageImageName}' to '{candidateName}'");
            _activePageObject.SetActive(false); // A1 비활성화
        }
        else
        {
            Debug.Log($"[Group A] Activating '{candidateName}'");
        }

        // A2를 풀에서 가져와 활성화
        _activePageObject = GetPooledObject(_pooledPageObjects, _pagePrefabDict, candidateName, candidate.transform);
        _activePageImageName = candidateName;

        if (_activePageObject != null)
        {
            _activePageObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activePageObject.SetActive(true);
        }
    }
    
    private void HandleRemovedImage(ARTrackedImage removedImage)
    {
        (string imageName, bool isGroupA) = GetImageGroup(removedImage);
        if (!isGroupA || imageName == null) return;

        if (_activePageObject != null && _activePageImageName == imageName)
        {
            Debug.Log($"[Group A] Deactivating '{imageName}' (Removed from tracking)");
            _activePageObject.SetActive(false);
            _activePageObject = null;
            _activePageImageName = null;
        }
    }

    #endregion

    #region --- 오브젝트 풀링 ---
    private GameObject GetPooledObject(Dictionary<string, GameObject> pool, Dictionary<string, GameObject> prefabDict, string name, Transform parent)
    {
        if (pool.TryGetValue(name, out GameObject pooledObject) && pooledObject != null)
        {
            pooledObject.transform.SetParent(parent);
            return pooledObject;
        }
        if (prefabDict.TryGetValue(name, out GameObject prefab))
        {
            GameObject newObject = Instantiate(prefab, parent.position, parent.rotation);
            newObject.transform.SetParent(parent);
            pool[name] = newObject;
            return newObject;
        }
        Debug.LogError($"[GroupA_Tracker] '{name}'에 해당하는 Prefab이 없습니다.");
        return null;
    }
    #endregion
}