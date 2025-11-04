using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// [최종 리팩토링]
/// A그룹(배타적)과 B그룹(독립적) 이미지 트래킹을 관리합니다.
/// 1. A그룹은 새 이미지가 인식될 때만 이전 오브젝트가 비활성화됩니다. (지연 비활성화)
/// 2. 모든 오브젝트는 풀링(Pooling)되어 재활용됩니다.
/// 3. A그룹 이미지가 여러 장 동시 인식되면, 가장 마지막에 처리된 이미지만 활성화됩니다.
/// </summary>
public class ExclusiveImageTracker : MonoBehaviour
{
    // --- 인스펙터 할당 ---
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;
    [SerializeField]
    private List<ImagePrefabEntry> groupAPrefabs;
    [SerializeField]
    private List<ImagePrefabEntry> groupBPrefabs;

    // --- 프리팹 원본 딕셔너리 ---
    private readonly Dictionary<string, GameObject> _groupAPrefabDict = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _groupBPrefabDict = new Dictionary<string, GameObject>();
    
    // --- 오브젝트 풀(Pool) ---
    private readonly Dictionary<string, GameObject> _pooledGroupAObjects = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> _pooledGroupBObjects = new Dictionary<string, GameObject>();

    // --- A 그룹 상태 변수 ---
    private GameObject _activeGroupAObject = null;
    private string _activeGroupAImageName = null;
    
    [Serializable]
    public struct ImagePrefabEntry
    {
        public string imageName; // XRReferenceImageLibrary의 이름과 일치해야 함
        public GameObject prefab;
    }

    #region --- 유니티 생명주기 (Awake, OnEnable, OnDisable) ---

    void Awake()
    {
        InitializePrefabDictionaries();
    }

    void OnDisable()
    {
        DeactivateAllPooledObjects();
    }

    /// <summary>
    /// 스크립트 비활성화 시 모든 풀링된 오브젝트를 끕니다.
    /// </summary>
    private void DeactivateAllPooledObjects()
    {
        foreach (var entry in _pooledGroupAObjects)
        {
            if (entry.Value != null) entry.Value.SetActive(false);
        }
        foreach (var entry in _pooledGroupBObjects)
        {
            if (entry.Value != null) entry.Value.SetActive(false);
        }
        _activeGroupAObject = null;
        _activeGroupAImageName = null;
    }

    /// <summary>
    /// 인스펙터의 리스트를 원본 프리팹 딕셔너리로 변환합니다.
    /// </summary>
    private void InitializePrefabDictionaries()
    {
        _groupAPrefabDict.Clear();
        foreach (var entry in groupAPrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName))
            {
                Debug.LogWarning("[ExclusiveImageTracker] Group A에 이름이 비어있는 Prefab 항목이 있습니다.");
                continue;
            }
            if (entry.prefab == null)
            {
                 Debug.LogWarning($"[ExclusiveImageTracker] Group A의 '{entry.imageName}' 항목에 Prefab이 할당되지 않았습니다.");
                 continue;
            }
            if (_groupAPrefabDict.ContainsKey(entry.imageName))
            {
                Debug.LogWarning($"[ExclusiveImageTracker] Group A에 '{entry.imageName}' 이름이 중복됩니다.");
                continue;
            }
            _groupAPrefabDict.Add(entry.imageName, entry.prefab);
        }
        
        _groupBPrefabDict.Clear();
        foreach (var entry in groupBPrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName))
            {
                Debug.LogWarning("[ExclusiveImageTracker] Group B에 이름이 비어있는 Prefab 항목이 있습니다.");
                continue;
            }
            if (entry.prefab == null)
            {
                 Debug.LogWarning($"[ExclusiveImageTracker] Group B의 '{entry.imageName}' 항목에 Prefab이 할당되지 않았습니다.");
                 continue;
            }
            if (_groupBPrefabDict.ContainsKey(entry.imageName) || _groupAPrefabDict.ContainsKey(entry.imageName))
            {
                Debug.LogWarning($"[ExclusiveImageTracker] Group B의 '{entry.imageName}' 이름이 중복됩니다. (A 또는 B 그룹)");
                continue;
            }
            _groupBPrefabDict.Add(entry.imageName, entry.prefab);
        }

        _pooledGroupAObjects.Clear();
        _pooledGroupBObjects.Clear();
    }

    #endregion

    #region --- AR 이벤트 핸들러 및 로직 ---

    /// <summary>
    /// [수정됨] AR Foundation 6.0+ 이벤트 핸들러 (로직 재구성)
    /// </summary>
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // [요구사항 3] 이번 프레임에 활성화할 A그룹 후보.
        // 루프를 돌면서 가장 마지막에 감지된 이미지로 계속 덮어써집니다.
        ARTrackedImage activeACandidate = null;

        // --- 1. Added 리스트 처리 ---
        foreach (var newImage in eventArgs.added)
        {
            (string imageName, bool isGroupA) = GetImageGroup(newImage);
            if (imageName == null) continue; // null이거나 그룹 없음

            if (isGroupA)
            {
                if (newImage.trackingState == TrackingState.Tracking)
                {
                    activeACandidate = newImage; // A그룹 후보로 등록
                }
            }
            else // B 그룹
            {
                HandleGroupBImage(newImage, imageName); // B그룹은 즉시 처리
            }
        }

        // --- 2. Updated 리스트 처리 ---
        foreach (var updatedImage in eventArgs.updated)
        {
            (string imageName, bool isGroupA) = GetImageGroup(updatedImage);
            if (imageName == null) continue;

            if (isGroupA)
            {
                if (updatedImage.trackingState == TrackingState.Tracking)
                {
                    activeACandidate = updatedImage; // A그룹 후보 덮어쓰기 (가장 마지막 것)
                }
            }
            else // B 그룹
            {
                HandleGroupBImage(updatedImage, imageName); // B그룹은 즉시 처리
            }
        }

        // --- 3. A그룹 최종 후보 처리 ---
        // 루프가 끝난 후, 단 한 번만 A그룹 로직을 실행합니다.
        UpdateActiveAObject(activeACandidate);

        // --- 4. Removed 리스트 처리 ---
        foreach (var removedEntry in eventArgs.removed)
        {
            ARTrackedImage removedImage = removedEntry.Value;
            if (removedImage != null)
            {
                HandleRemovedImage(removedImage); // 풀링을 위한 비활성화
            }
        }
    }

    /// <summary>
    /// [신규] 이미지의 그룹(A/B)과 이름을 반환하고 null을 체크합니다.
    /// </summary>
    private (string imageName, bool isGroupA) GetImageGroup(ARTrackedImage trackedImage)
    {
        if (trackedImage == null || trackedImage.referenceImage == null)
        {
            Debug.LogWarning("Tracked image or its reference image is null.");
            return (null, false);
        }

        string imageName = trackedImage.referenceImage.name;

        if (string.IsNullOrEmpty(imageName))
        {
            Debug.LogWarning("Detected an image with a null or empty name in the library.");
            return (null, false);
        }

        if (_groupAPrefabDict.ContainsKey(imageName))
        {
            return (imageName, true); // Group A
        }
        
        if (_groupBPrefabDict.ContainsKey(imageName))
        {
            return (imageName, false); // Group B
        }

        // 어느 그룹에도 속하지 않음
        return (null, false);
    }

    /// <summary>
    /// [신규] A그룹 로직을 중앙에서 처리합니다. (기존 HandleGroupAImage 대체)
    /// </summary>
    private void UpdateActiveAObject(ARTrackedImage candidate)
    {
        // Case 1: 이번 프레임에 추적 중인 A그룹 이미지가 '없음'.
        if (candidate == null)
        {
            // [요구사항 1: 지연 비활성화]
            // 아무것도 하지 않습니다.
            // _activeGroupAObject는 다음 A그룹 이미지가 인식될 때까지 활성 상태를 유지합니다.
            return;
        }

        // Case 2: 추적 중인 A그룹 이미지가 '있음'.
        string candidateName = candidate.referenceImage.name;

        // Case 2a: 이미 활성화된 이미지와 '같은' 이미지임.
        if (_activeGroupAObject != null && _activeGroupAImageName == candidateName)
        {
            // 위치만 업데이트합니다.
            _activeGroupAObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activeGroupAObject.SetActive(true); // 혹시 모르니 활성화 보장
            return;
        }

        // Case 2b: '다른' A그룹 이미지로 교체해야 함. (A1 -> A2)
        // [요구사항 1] 여기서 이전 오브젝트(A1)를 비활성화합니다.
        if (_activeGroupAObject != null)
        {
            Debug.Log($"[Group A] Swapping from '{_activeGroupAImageName}' to '{candidateName}'");
            _activeGroupAObject.SetActive(false);
        }
        else
        {
            Debug.Log($"[Group A] Activating '{candidateName}'");
        }

        // [요구사항 2] 새 오브젝트(A2)를 풀에서 가져옵니다.
        _activeGroupAObject = GetPooledObject(_pooledGroupAObjects, _groupAPrefabDict, candidateName, candidate.transform);
        _activeGroupAImageName = candidateName;

        if (_activeGroupAObject != null)
        {
            // 새 오브젝트(A2)의 위치를 설정하고 활성화합니다.
            _activeGroupAObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activeGroupAObject.SetActive(true);
        }
    }

    /// <summary>
    /// B 그룹 (독립적, 풀링) 로직. (시그니처 변경 없음)
    /// </summary>
    private void HandleGroupBImage(ARTrackedImage image, string name)
    {
        GameObject bObject = GetPooledObject(_pooledGroupBObjects, _groupBPrefabDict, name, image.transform);
        if (bObject == null) return; // 프리팹 없음

        bObject.transform.SetPositionAndRotation(image.transform.position, image.transform.rotation);
        
        // B그룹은 추적 상태에 따라 즉시 활성화/비활성화됩니다.
        bObject.SetActive(image.trackingState == TrackingState.Tracking);
    }

    /// <summary>
    /// 추적 목록에서 '제거된' 이미지를 처리합니다. (풀링을 위해 비활성화)
    /// </summary>
    private void HandleRemovedImage(ARTrackedImage removedImage)
    {
        (string imageName, bool isGroupA) = GetImageGroup(removedImage);
        if (imageName == null) return; // null이거나 그룹 없음

        // A 그룹 처리
        if (isGroupA)
        {
            if (_activeGroupAObject != null && _activeGroupAImageName == imageName)
            {
                Debug.Log($"[Group A] Deactivating '{imageName}' (Removed from tracking)");
                _activeGroupAObject.SetActive(false);
                _activeGroupAObject = null;
                _activeGroupAImageName = null;
            }
        }
        // B 그룹 처리
        else
        {
            if (_pooledGroupBObjects.TryGetValue(imageName, out GameObject bObject) && bObject != null)
            {
                if (bObject.activeSelf)
                {
                    Debug.Log($"[Group B] Deactivating '{imageName}' (Removed from tracking)");
                    bObject.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region --- 오브젝트 풀링 ---

    /// <summary>
    /// 오브젝트 풀(Pool)에서 오브젝트를 가져오거나 생성합니다.
    /// </summary>
    private GameObject GetPooledObject(Dictionary<string, GameObject> pool, Dictionary<string, GameObject> prefabDict, string name, Transform parent)
    {
        // 1. 풀에 이미 생성된 인스턴스가 있는지 확인
        if (pool.TryGetValue(name, out GameObject pooledObject) && pooledObject != null)
        {
            // 재활용: 부모만 최신 ARTrackedImage로 교체
            pooledObject.transform.SetParent(parent);
            return pooledObject;
        }

        // 2. 풀에 없음 (최초 생성)
        if (prefabDict.TryGetValue(name, out GameObject prefab))
        {
            GameObject newObject = Instantiate(prefab, parent.position, parent.rotation);
            newObject.transform.SetParent(parent);
            
            // 새 인스턴스를 풀에 등록
            pool[name] = newObject;
            
            return newObject;
        }

        // 3. 예외: 원본 프리팹이 없음 (설정 오류)
        Debug.LogError($"[ExclusiveImageTracker] '{name}'에 해당하는 Prefab이 없습니다. 인스펙터를 확인하세요.");
        return null;
    }

    #endregion
}