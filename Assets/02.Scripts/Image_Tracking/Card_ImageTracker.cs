using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // UnityEvent를 사용하기 위해 추가
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// [수정] 카드(B그룹)의 인식 로직을 담당합니다.
/// 상태 변경을 UnityEvent로 외부에 '방송'합니다.
/// </summary>
public class Card_ImageTracker : MonoBehaviour
{
    private ARTrackedImageManager trackedImageManager;

    [SerializeField]
    private List<ImagePrefabEntry> cardPrefabs; 

    // --- 프리팹 딕셔너리 ---
    private readonly Dictionary<string, GameObject> _cardPrefabDict = new Dictionary<string, GameObject>();
    // --- 오브젝트 풀 ---
    private readonly Dictionary<string, GameObject> _pooledCardObjects = new Dictionary<string, GameObject>();
    // --- 임시 리스트 ---
    private readonly List<ARTrackedImage> _trackingCardImagesCache = new List<ARTrackedImage>();

    // [신규] 현재 상태를 추적하여 이벤트 중복 호출을 방지
    private enum CardTrackingState { None, Single, Multiple }
    private CardTrackingState _currentState = CardTrackingState.None;

    [Serializable]
    public struct ImagePrefabEntry
    {
        public string imageName;
        public GameObject prefab;
    }

    #region --- 유니티 생명주기 ---

    void Awake()
    {
        InitializePrefabDictionaries();
        trackedImageManager = FindAnyObjectByType<ARTrackedImageManager>();
        if (trackedImageManager == null)
            Debug.LogError("[Card_ImageTracker] 씬에 ARTrackedImageManager가 없습니다!");
    }

    void OnEnable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
            Debug.Log("[Card_ImageTracker] 카드 이미지 인식 시작.");
        }
        // [신규] 활성화 시 상태 초기화
        _currentState = CardTrackingState.None;
    }

    void OnDisable()
    {
        if (trackedImageManager != null)
        {
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            Debug.Log("[Card_ImageTracker] 카드 이미지 인식 중지.");
        }
        DeactivateAllCardObjects();
    }

    // ( ... DeactivateAllCardObjects, InitializePrefabDictionaries ... )
    // ... 이전 코드와 동일 ...
    private void DeactivateAllCardObjects(string exceptThisOne = null)
    {
        foreach (var entry in _pooledCardObjects)
        {
            if (entry.Key != exceptThisOne && entry.Value != null && entry.Value.activeSelf)
            {
                entry.Value.SetActive(false);
            }
        }
    }
    private void InitializePrefabDictionaries()
    {
        _cardPrefabDict.Clear();
        foreach (var entry in cardPrefabs)
        {
            if (string.IsNullOrEmpty(entry.imageName)) { Debug.LogWarning("[Card_ImageTracker] 카드 그룹에 이름이 빈 Prefab이 있습니다."); continue; }
            if (entry.prefab == null) { Debug.LogWarning($"[Card_ImageTracker] 카드 그룹 '{entry.imageName}'에 Prefab이 없습니다."); continue; }
            if (_cardPrefabDict.ContainsKey(entry.imageName)) { Debug.LogWarning($"[Card_ImageTracker] 카드 그룹에 '{entry.imageName}' 이름이 중복됩니다."); continue; }
            _cardPrefabDict.Add(entry.imageName, entry.prefab);
        }
        _pooledCardObjects.Clear();
    }
    #endregion

    #region --- AR 이벤트 핸들러 및 B그룹 로직 ---

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        UpdateCardState();
    }

    /// <summary>
    /// [수정됨] 카드(B그룹)의 전체 상태를 스캔하고 '이벤트'를 호출합니다.
    /// </summary>
    private void UpdateCardState()
    {
        _trackingCardImagesCache.Clear(); 
        foreach (var image in trackedImageManager.trackables)
        {
            (string imageName, bool isCard) = GetImageGroup(image);
            if (isCard && image.trackingState == TrackingState.Tracking)
            {
                _trackingCardImagesCache.Add(image);
            }
        }

        int count = _trackingCardImagesCache.Count;
        CardTrackingState newState; // 이번 프레임의 새로운 상태

        // [Rule 1] 1개일 때: 정상 소환
        if (count == 1)
        {
            newState = CardTrackingState.Single;
            ARTrackedImage singleImage = _trackingCardImagesCache[0];
            string singleName = singleImage.referenceImage.name;
            
            GameObject cardObject = GetPooledObject(_pooledCardObjects, _cardPrefabDict, singleName, singleImage.transform);
            if (cardObject == null) return;

            cardObject.transform.SetPositionAndRotation(singleImage.transform.position, singleImage.transform.rotation);
            if (!cardObject.activeSelf)
            {
                cardObject.SetActive(true); // -> CardBehavior.OnEnable() 트리거
            }
            DeactivateAllCardObjects(exceptThisOne: singleName);
        }
        // [Rule 2] 2개 이상일 때: 모두 숨기고 이벤트 호출
        else if (count >= 2)
        {
            newState = CardTrackingState.Multiple;
            DeactivateAllCardObjects(); // 모든 카드 오브젝트 숨김
        }
        // [Rule 3] 0개일 때: 모두 숨김
        else // count == 0
        {
            newState = CardTrackingState.None;
            DeactivateAllCardObjects();
        }

        // --- [신규] 상태가 '변경'되었을 때만 이벤트 호출 ---
        if (newState != _currentState)
        {
            Debug.Log($"[Card_ImageTracker] 상태 변경: {_currentState} -> {newState}");
            _currentState = newState;

            if (newState == CardTrackingState.Single)
            {
                EventManager.SingleCardTracked();
            }
            else if (newState == CardTrackingState.Multiple)
            {
                EventManager.MultipleCardsTracked();
            }
            else // newState == CardTrackingState.None
            {
                EventManager.NoCardsTracked();
            }
        }
    }

    // ( ... GetImageGroup, GetPooledObject ... )
    // ... 이전 코드와 동일 ...
    private (string imageName, bool isCard) GetImageGroup(ARTrackedImage trackedImage)
    {
        if (trackedImage == null || trackedImage.referenceImage == null) return (null, false);
        string imageName = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(imageName)) return (null, false);
        if (_cardPrefabDict.ContainsKey(imageName))
            return (imageName, true);
        return (null, false);
    }
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
        Debug.LogError($"[Card_ImageTracker] '{name}'에 해당하는 Prefab이 없습니다.");
        return null;
    }
    #endregion
}