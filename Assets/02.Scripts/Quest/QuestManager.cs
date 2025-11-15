using UnityEngine;

/// <summary>
/// [수정됨] 퀘스트 매니저
/// 퀘스트의 '순서(Index)'를 강제하지 않고, '현재 활성화된 단일 퀘스트'만
/// 추적하여 비순차적 실행을 지원합니다.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // 1. Singleton 패턴 적용
    public static QuestManager Instance { get; private set; }

    [Header("Quest Flow")]
    
    // 2. [제거됨] currentQuestIndex: 퀘스트 순서를 강제하므로 제거합니다.
    // public int currentQuestIndex = 0; 

    // 3. 현재 활성화된 '단일' 퀘스트를 추적합니다.
    [SerializeField] private Quest currentActiveQuest = null;

    private void Awake()
    {
        // Singleton 인스턴스 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // 4. [제거됨] Start() 함수: currentQuestIndex를 사용했으므로 필요 없습니다.

    #region 이벤트 구독/해제
    private void OnEnable()
    {
        // 5. StartQuest 이벤트는 Quest의 OnEnable에서 직접 처리하므로 제거
        EventManager.onAdvanceQuest += AdvanceQuest;
        EventManager.onFinishQuest += FinishQuest;
    }
    private void OnDisable()
    {
        EventManager.onAdvanceQuest -= AdvanceQuest;
        EventManager.onFinishQuest -= FinishQuest;
    }
    #endregion

    #region 퀘스트 등록 및 흐름 제어

    /// <summary>
    /// 6. (핵심) Quest가 OnEnable될 때 이 함수를 호출하여 자신을 '활성 퀘스트'로 등록합니다.
    /// 퀘스트 순서(index)에 상관없이 실행됩니다.
    /// </summary>
    public void RegisterAndAttemptStart(Quest quest)
    {
        // 이미 이 퀘스트가 활성 퀘스트라면 아무것도 하지 않음 (중복 호출 방지)
        if (quest == currentActiveQuest)
        {
            return;
        }

        // 7. [신규] '다른' 퀘스트가 이미 활성화되어 있다면 (예: 1번 -> 2번으로 전환 시)
        // GroupA_Tracker가 이전 퀘스트의 OnDisable을 호출했겠지만,
        // 안전을 위해 여기서 한 번 더 기존 퀘스트를 강제 비활성화(정리)합니다.
        if (currentActiveQuest != null)
        {
            Debug.LogWarning($"[QuestManager] '{currentActiveQuest.displayName}' 퀘스트가 '{quest.displayName}'에 의해 중단됩니다.");
            currentActiveQuest.gameObject.SetActive(false); 
            // (이전 Quest의 OnDisable이 호출되어 UnregisterQuest가 실행됨)
        }
        
        // 8. [수정됨] 퀘스트 순서와 상관없이, 새로 등록된 퀘스트를 '현재 활성 퀘스트'로 설정합니다.
        currentActiveQuest = quest;
        
        // Quest가 (GroupA_Tracker에 의해) 이미 활성화되어 있지만, 다시 한번 보장
        if (!currentActiveQuest.gameObject.activeInHierarchy)
        {
            currentActiveQuest.gameObject.SetActive(true);
        }

        // Quest 내부의 시작 로직 호출
        Debug.Log($"[QuestManager] 퀘스트 '{quest.displayName}' (Index: {quest.questIndex}) 시작.");
        currentActiveQuest.StartQuestInternal();
    }
    
    /// <summary>
    /// 9. (신규) Quest가 OnDisable될 때 호출되어, 자신이 활성 퀘스트였는지 확인하고 정리합니다.
    /// </summary>
    public void UnregisterQuest(Quest quest)
    {
        // 비활성화된 퀘스트가 현재 활성 퀘스트인지 확인
        if (quest == currentActiveQuest)
        {
            Debug.Log($"[QuestManager] 활성 퀘스트 '{quest.displayName}' 등록 해제.");
            currentActiveQuest = null;
        }
    }

    // 퀘스트가 다음 단계로 진행
    private void AdvanceQuest(Quest quest)
    {
        // 현재 활성화된 퀘스트가 맞는지 확인
        if (quest == currentActiveQuest)
        {
            quest.MoveToNextStep();
        }
    }

    // 퀘스트 완료 처리
    private void FinishQuest(Quest quest)
    {
        if (quest == currentActiveQuest)
        {
            Debug.Log($"[QuestManager] 퀘스트 '{quest.displayName}' (Index: {quest.questIndex}) 완료.");
            quest.gameObject.SetActive(false);
            currentActiveQuest = null;
        }
    }
    #endregion
}