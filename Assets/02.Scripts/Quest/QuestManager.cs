using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // 1. Singleton 패턴 적용
    public static QuestManager Instance { get; private set; }

    [Header("Quest Flow")]
    
    public int currentQuestIndex = 0; // 현재 역할 없음

    // 2. 현재 활성화된 퀘스트를 추적하기 위한 참조
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

        // 3. 자식에서 퀘스트를 찾는 로직 모두 제거
        //    매니저는 이제 퀘스트가 스스로 등록하기를 기다림.
    }

    private void Start()
    {
        currentQuestIndex = 0;
    }

    #region 이벤트 구독/해제
    private void OnEnable()
    {
        // 5. onStartQuest 구독 제거
        // EventManager.onStartQuest += StartQuest; 
        EventManager.onAdvanceQuest += AdvanceQuest;
        EventManager.onFinishQuest += FinishQuest;
    }
    private void OnDisable()
    {
        // EventManager.onStartQuest -= StartQuest;
        EventManager.onAdvanceQuest -= AdvanceQuest;
        EventManager.onFinishQuest -= FinishQuest;
    }
    #endregion

    #region 퀘스트 등록 및 흐름 제어

    /// <summary>
    /// 6. (핵심) Quest 프리팹이 생성될 때 이 함수를 호출하여 자신을 등록합니다.
    /// </summary>
    public void RegisterAndAttemptStart(Quest quest)
    {
            
            currentActiveQuest = quest;
            
            // Quest가 활성화되어 있지 않다면 활성화
            if (!currentActiveQuest.gameObject.activeInHierarchy)
            {
                currentActiveQuest.gameObject.SetActive(true);
            }

            // Quest 내부의 시작 로직 호출
            currentActiveQuest.StartQuestInternal();
        
    }

    // 7. StartQuest(int index) 이벤트 핸들러 제거 (또는 다른 용도로 수정)
    //    이벤트 자체가 제거되었다면 이 함수도 필요 없음.

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

            // 다음 퀘스트 인덱스로 이동
            currentQuestIndex++;
            Debug.Log($"[QuestManager] 다음 퀘스트 (Index: {currentQuestIndex})를 기다립니다.");
        }
    }
    #endregion
}