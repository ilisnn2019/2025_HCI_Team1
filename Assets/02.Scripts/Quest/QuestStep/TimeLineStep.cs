using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineStep : QuestStep
{
    [Header("Timeline Settings")]
    [Tooltip("이 스텝에서 재생할 타임라인(PlayableDirector)")]
    [SerializeField]
    private PlayableDirector playableDirector;

    [Tooltip("스텝이 비활성화될 때(완료되거나 중단될 때) 타임라인을 정지시킬지 여부")]
    [SerializeField]
    private bool stopOnDisable = true;

    // 1. 코루틴 참조 제거

    private void Awake()
    {
        if (playableDirector == null)
        {
            playableDirector = GetComponent<PlayableDirector>();
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        if (playableDirector == null)
        {
            Debug.LogError($"[TimelineStep] '{name}': PlayableDirector 할당 안됨", this);
            FinishQuestStep(); // 즉시 종료
            return;
        }

        // 타임라인 정지 이벤트에 구독
        playableDirector.stopped += OnTimelineStopped;

        playableDirector.Play();
        
    }

    private void OnDisable()
    {
        // 메모리 누수 방지 구독 해제
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineStopped;
        }

        if (stopOnDisable && playableDirector != null)
        {
            playableDirector.Stop();
        }
    }
    private void OnTimelineStopped(PlayableDirector director)
    {
        // 우리가 관리하는 타임라인이 맞다면
        if (director == playableDirector)
        {
            Debug.Log($"[TimelineStep] '{name}': 타임라인 재생 완료");
            FinishQuestStep();
        }
    }
}