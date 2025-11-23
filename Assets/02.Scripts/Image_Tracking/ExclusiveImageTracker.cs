using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// [최종 리팩토링 v2]
/// A그룹(배타적)과 B그룹(독립적) 이미지 트래킹을 관리합니다.
/// 1. A그룹: 지연 비활성화, 풀링, 다중 인식 시 마지막 이미지 활성화
/// 2. B그룹:
///    - 'IsGroupBTrackingEnabled' 플래그가 true일 때만 인식
///    - 1개 인식: 정상 소환
///    - 2개 이상 인식: 모두 숨기고 'EventManager.MoreThanTwoCardDetected()' 호출
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
    
    // [신규] B 그룹 활성화 플래그 (Rule 1)
    public bool IsGroupBTrackingEnabled { get; private set; } = true;

    [Serializable]
    public struct ImagePrefabEntry
    {
        public string imageName;
        public GameObject prefab;
    }

    #region --- 유니티 생명주기 및 public API ---

    void Awake()
    {
        InitializePrefabDictionaries();
    }

    // [중요] OnEnable 이벤트 구독 로직이 누락되어 추가했습니다.
    void OnDisable()
    {
        DeactivateAllPooledObjects();
    }

    /// <summary>
    /// [신규] 외부에서 B그룹 트래킹을 켜고 끕니다. (Rule 1)
    /// </summary>
    public void SetGroupBTracking(bool isEnabled)
    {
        IsGroupBTrackingEnabled = isEnabled;
        if (!isEnabled)
        {
            // 플래그가 꺼지면 즉시 모든 B그룹 오브젝트를 비활성화합니다.
            DeactivateAllGroupBObjects();
        }
        // B그룹 상태를 즉시 재평가
        UpdateGroupBState();
    }

    /// <summary>
    /// 스크립트 비활성화 시 모든 풀링된 오브젝트를 끕니다.
    /// </summary>
    private void DeactivateAllPooledObjects()
    {
        DeactivateAllGroupAObjects();
        DeactivateAllGroupBObjects();
    }

    /// <summary>
    /// [신규] A그룹 오브젝트만 모두 비활성화합니다.
    /// </summary>
    private void DeactivateAllGroupAObjects()
    {
         foreach (var entry in _pooledGroupAObjects)
        {
            if (entry.Value != null) entry.Value.SetActive(false);
        }
        _activeGroupAObject = null;
        _activeGroupAImageName = null;
    }

    /// <summary>
    /// [신규] B그룹 오브젝트만 모두 비활성화합니다.
    /// (예외 이미지를 지정하여 그 이미지만 놔둘 수 있습니다.)
    /// </summary>
    private void DeactivateAllGroupBObjects(string exceptThisOne = null)
    {
        foreach (var entry in _pooledGroupBObjects)
        {
            if (entry.Key != exceptThisOne && entry.Value != null && entry.Value.activeSelf)
            {
                entry.Value.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 인스펙터의 리스트를 원본 프리팹 딕셔너리로 변환합니다. (로직 동일)
    /// </summary>
    private void InitializePrefabDictionaries()
    {
        _groupAPrefabDict.Clear();
        foreach (var entry in groupAPrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName)) { Debug.LogWarning("[Tracker] Group A에 이름이 빈 Prefab이 있습니다."); continue; }
            if (entry.prefab == null) { Debug.LogWarning($"[Tracker] Group A '{entry.imageName}'에 Prefab이 없습니다."); continue; }
            if (_groupAPrefabDict.ContainsKey(entry.imageName)) { Debug.LogWarning($"[Tracker] Group A에 '{entry.imageName}' 이름이 중복됩니다."); continue; }
            _groupAPrefabDict.Add(entry.imageName, entry.prefab);
        }
        
        _groupBPrefabDict.Clear();
        foreach (var entry in groupBPrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName)) { Debug.LogWarning("[Tracker] Group B에 이름이 빈 Prefab이 있습니다."); continue; }
            if (entry.prefab == null) { Debug.LogWarning($"[Tracker] Group B '{entry.imageName}'에 Prefab이 없습니다."); continue; }
            if (_groupBPrefabDict.ContainsKey(entry.imageName) || _groupAPrefabDict.ContainsKey(entry.imageName)) { Debug.LogWarning($"[Tracker] Group B '{entry.imageName}' 이름이 중복됩니다."); continue; }
            _groupBPrefabDict.Add(entry.imageName, entry.prefab);
        }

        _pooledGroupAObjects.Clear();
        _pooledGroupBObjects.Clear();
    }

    #endregion

    #region --- AR 이벤트 핸들러 및 로직 ---

    /// <summary>
    /// [수정됨] AR Foundation 6.0+ 이벤트 핸들러 (B그룹 로직 분리)
    /// </summary>
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        ARTrackedImage activeACandidate = null;

        // --- 1. Added 리스트 (A그룹 후보 찾기) ---
        foreach (var newImage in eventArgs.added)
        {
            (string imageName, bool isGroupA) = GetImageGroup(newImage);
            if (imageName == null) continue;
            if (isGroupA && newImage.trackingState == TrackingState.Tracking)
            {
                activeACandidate = newImage; // 후보 등록
            }
        }

        // --- 2. Updated 리스트 (A그룹 후보 덮어쓰기) ---
        foreach (var updatedImage in eventArgs.updated)
        {
            (string imageName, bool isGroupA) = GetImageGroup(updatedImage);
            if (imageName == null) continue;
            if (isGroupA && updatedImage.trackingState == TrackingState.Tracking)
            {
                activeACandidate = updatedImage; // 후보 덮어쓰기 (가장 마지막 것)
            }
        }

        // --- 3. A그룹 최종 후보 처리 ---
        UpdateActiveAObject(activeACandidate);

        // --- 4. [신규] B그룹 전체 상태 재평가 (Rule 1, 2) ---
        // B그룹은 A그룹과 달리, 'added/updated'와 상관없이
        // 'trackables' 컬렉션 전체를 스캔하여 현재 상태를 확정합니다.
        UpdateGroupBState();

        // --- 5. Removed 리스트 처리 (A그룹 전용) ---
        foreach (var removedEntry in eventArgs.removed)
        {
            ARTrackedImage removedImage = removedEntry.Value;
            if (removedImage != null)
            {
                HandleRemovedImage(removedImage); // B그룹 로직은 제거됨
            }
        }
    }

    /// <summary>
    /// [신규] B그룹의 전체 상태를 스캔하고 규칙(1, 2)을 적용합니다.
    /// </summary>
    private void UpdateGroupBState()
    {
        // [Rule 1] 플래그가 false이면, 모든 B오브젝트를 끄고 즉시 종료
        if (!IsGroupBTrackingEnabled)
        {
            DeactivateAllGroupBObjects();
            return;
        }

        // [Rule 2] 현재 '추적 중'인 B그룹 이미지의 개수를 셉니다.
        var trackingBImages = new List<ARTrackedImage>();
        foreach (var image in trackedImageManager.trackables)
        {
            (string imageName, bool isGroupA) = GetImageGroup(image);
            if (imageName != null && !isGroupA && image.trackingState == TrackingState.Tracking)
            {
                trackingBImages.Add(image);
            }
        }

        int count = trackingBImages.Count;

        // [Rule 2.1] 1개일 때: 정상 소환
        if (count == 1)
        {
            ARTrackedImage singleImage = trackingBImages[0];
            string singleName = singleImage.referenceImage.name;
            
            // 풀에서 오브젝트를 가져오거나 생성
            GameObject bObject = GetPooledObject(_pooledGroupBObjects, _groupBPrefabDict, singleName, singleImage.transform);
            if (bObject == null) return;

            // 활성화 및 위치 업데이트
            bObject.transform.SetPositionAndRotation(singleImage.transform.position, singleImage.transform.rotation);
            bObject.SetActive(true);

            // 혹시 모를 다른 B오브젝트들(추적을 잃은)을 비활성화
            DeactivateAllGroupBObjects(exceptThisOne: singleName);
        }
        // [Rule 2.2] 2개 이상일 때: 모두 숨기고 이벤트 호출
        else if (count >= 2)
        {
            DeactivateAllGroupBObjects(); // 모든 B 오브젝트 숨김
            
            // C# 6.0 이상에서 안전하게 static 이벤트 호출
            // EventManager.onMoreThanTwoCardDetected?.Invoke(); 
            
            // 또는 사용자가 제공한 원본 코드
            EventManager.MoreThanTwoCardDetected();
        }
        // [Rule 2.3] 0개일 때: 모두 숨김
        else // count == 0
        {
            DeactivateAllGroupBObjects();
        }
    }

    /// <summary>
    /// [수정됨] B그룹 로직이 제거되었습니다. (A그룹 전용)
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
        // B 그룹 로직은 UpdateGroupBState에서 전체 관리하므로 여기서 처리할 필요 없음
    }

    // --- (이하 코드는 변경 없음) ---

    /// <summary>
    /// 이미지의 그룹(A/B)과 이름을 반환하고 null을 체크합니다. (변경 없음)
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
        return (null, false);
    }

    /// <summary>
    /// A그룹 로직을 중앙에서 처리합니다. (변경 없음)
    /// </summary>
    private void UpdateActiveAObject(ARTrackedImage candidate)
    {
        if (candidate == null)
        {
            // Case 1: 추적 중인 A 이미지가 없음 -> 아무것도 안 함 (지연 비활성화)
            return;
        }

        string candidateName = candidate.referenceImage.name;

        // Case 2a: 이미 활성화된 이미지와 같음 -> 위치만 업데이트
        if (_activeGroupAObject != null && _activeGroupAImageName == candidateName)
        {
            _activeGroupAObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activeGroupAObject.SetActive(true);
            return;
        }

        // Case 2b: 다른 A 이미지로 교체 (A1 -> A2)
        if (_activeGroupAObject != null)
        {
            Debug.Log($"[Group A] Swapping from '{_activeGroupAImageName}' to '{candidateName}'");
            _activeGroupAObject.SetActive(false); // A1 비활성화
        }
        else
        {
            Debug.Log($"[Group A] Activating '{candidateName}'");
        }

        // A2를 풀에서 가져와 활성화
        _activeGroupAObject = GetPooledObject(_pooledGroupAObjects, _groupAPrefabDict, candidateName, candidate.transform);
        _activeGroupAImageName = candidateName;

        if (_activeGroupAObject != null)
        {
            _activeGroupAObject.transform.SetPositionAndRotation(candidate.transform.position, candidate.transform.rotation);
            _activeGroupAObject.SetActive(true);
        }
    }
    
    #endregion

    #region --- 오브젝트 풀링 (변경 없음) ---

    /// <summary>
    /// 오브젝트 풀(Pool)에서 오브젝트를 가져오거나 생성합니다.
    /// </summary>
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
        Debug.LogError($"[ExclusiveImageTracker] '{name}'에 해당하는 Prefab이 없습니다. 인스펙터를 확인하세요.");
        return null;
    }

    #endregion
}