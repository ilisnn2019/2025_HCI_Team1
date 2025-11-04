using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest : MonoBehaviour
{
    [field: SerializeField] public string id { get; private set; }

    [Header("General")]
    public string displayName; // UI 등에 표시될 퀘스트 이름
    
    // 1. (핵심) 이 퀘스트가 몇 번째 퀘스트인지 인스펙터에서 설정
    [Tooltip("이 퀘스트의 순서 인덱스 (0부터 시작)")]
    public int questIndex = 0;

    [Header("Steps(Auto Fill)")]
    public GameObject[] questSteps; // 퀘스트를 구성하는 단계(스텝) 오브젝트들
    
    [Header("Etc")]
    public int currentQuestStepIndex; // 현재 진행 중인 퀘스트 스텝의 인덱스
    
    private void Awake()
    {
        // 3. 자식 스텝들만 찾도록 필터링 (환경, 캐릭터 등은 제외)
        List<GameObject> steps = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // QuestStep 컴포넌트를 가진 자식만 리스트에 추가
            if (child.GetComponent<QuestStep>() != null)
            {
                steps.Add(child.gameObject);
            }
        }
        questSteps = steps.ToArray();
    }

    /// <summary>
    /// 5. (핵심) 오브젝트가 활성화될 때 (AR 마커가 인식될 때) 호출됨
    /// </summary>
    private void OnEnable()
    {
        // 매니저에게 자신을 등록하고 시작을 시도
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.RegisterAndAttemptStart(this);
        }
        else
        {
            Debug.LogError("[Quest] QuestManager Singleton 인스턴스를 찾을 수 없습니다! 씬에 매니저가 배치되었는지 확인하세요.");
        }
    }

    /// <summary>
    /// 6. (신규) QuestManager가 호출하는 실제 퀘스트 시작 로직
    /// </summary>
    public void StartQuestInternal()
    {
        // Awake가 OnEnable보다 늦게 호출될 경우를 대비해 스텝 재검색
        if (questSteps == null || questSteps.Length == 0)
        {
            List<GameObject> steps = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<QuestStep>() != null)
                    steps.Add(child.gameObject);
            }
            questSteps = steps.ToArray();
        }

        // 모든 스텝 초기화 및 비활성화
        for (int i = 0; i < questSteps.Length; i++)
        {
            questSteps[i].GetComponent<QuestStep>().InitializeQuestStep(this);
            questSteps[i].SetActive(false);
        }

        // 첫 번째 스텝부터 시작
        currentQuestStepIndex = 0;
        if (CurrentStepExists())
        {
            Debug.Log($"[Quest] '{displayName}'의 첫 번째 스텝 시작 (Index: {currentQuestStepIndex})");
            questSteps[currentQuestStepIndex].SetActive(true);
            // 스텝 시작 이벤트 호출
            questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepStart.Invoke();
            ChangeGameObjectsActive(true);
        }
        else
        {
            // 스텝이 하나도 없으면 바로 퀘스트 종료
            Debug.LogWarning($"[Quest] '{displayName}'에 스텝이 없습니다. 바로 종료합니다.");
            EventManager.FinishQuest(this);
        }
    }

    // 7. OnEnable에 있던 퀘스트 시작 로직들은 StartQuestInternal로 이동했으므로 제거

    // 다음 퀘스트 스텝으로 이동하는 함수
    public void MoveToNextStep()
    {
        // 현재 스텝 완료 처리
        questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepFinished.Invoke();
        ChangeGameObjectsActive(false);
        questSteps[currentQuestStepIndex].SetActive(false);

        // 인덱스 증가 및 다음 스텝 진행
        currentQuestStepIndex++;
        if (CurrentStepExists())
        {
            Debug.Log($"[Quest] '{displayName}'의 다음 스텝 시작 (Index: {currentQuestStepIndex})");
            questSteps[currentQuestStepIndex].SetActive(true);

            // 다음 스텝 시작 이벤트 호출
            questSteps[currentQuestStepIndex].GetComponent<QuestStep>().OnStepStart.Invoke();
            ChangeGameObjectsActive(true);
        }
        else
        {
            // 모든 스텝 완료 시 퀘스트 종료 이벤트 호출
            Debug.Log($"[Quest] '{displayName}'의 모든 스텝 완료.");
            EventManager.FinishQuest(this);
        }
    }
    
    public bool CurrentStepExists()
    {
        return (currentQuestStepIndex < questSteps.Length);
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(id))
            id = this.name;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    private void ChangeGameObjectsActive(bool enable)
    {
        GameObject[] gameObjects = questSteps[currentQuestStepIndex].GetComponent<QuestStep>().EnableDisableObjects;
        if (gameObjects != null)
        {
            foreach (var obj in gameObjects)
            {
                if (obj != null)
                    obj.SetActive(enable);
            }
        }
    }
}