using UnityEngine;
using DG.Tweening; // DOTween 사용

/// <summary>
/// Canvas Group의 알파값을 DOTween으로 제어하여 UI를 페이드 인/아웃시키는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))] // 이 컴포넌트가 붙으려면 CanvasGroup이 필수임을 명시
public class CanvasGroupFader : MonoBehaviour
{
    // 필수로 필요한 CanvasGroup 컴포넌트에 대한 참조
    private CanvasGroup canvasGroup;

    [Header("페이드 설정")]
    [Tooltip("페이드 인/아웃에 걸리는 시간 (초)")]
    [SerializeField]
    private float fadeDuration = 0.5f;

    [Tooltip("페이드에 사용할 Easing 함수")]
    [SerializeField]
    private Ease easeType = Ease.InOutSine;

    private void Awake()
    {
        // CanvasGroup 컴포넌트를 가져옵니다. RequireComponent를 사용했으므로 null일 염려는 없습니다.
        canvasGroup = GetComponent<CanvasGroup>();

        // DOTween 초기화 (선택 사항: 만약 DOTween 설정 창에서 Auto-Initialization을 꺼뒀다면 필요)
        // DOTween.Init();
    }

    private void OnEnable()
    {
        FadeIn();
    }

    /// <summary>
    /// Canvas Group을 페이드 인(Alpha 0 -> 1) 시키는 함수.
    /// 페이드 인 후에는 상호작용 가능 상태(Interactable, BlocksRaycasts)로 설정합니다.
    /// </summary>
    /// <param name="onComplete">페이드 완료 후 실행할 액션 (콜백)</param>
    public void FadeIn()
    {
        // 0. 초기화: 상호작용 관련 플래그를 미리 설정하여 애니메이션 중 클릭이 가능하도록 합니다.
        // BlocksRaycasts는 애니메이션이 완료될 때까지 잠시 꺼두는 것이 일반적이지만, 
        // 여기서는 In이 시작될 때 켜고, 완료 시 Interactable을 켜는 방식으로 구성합니다.
        SetTransparentImmediately();
        // 1. DOTween 트윈 생성 및 실행
        // DOFade(목표 알파값, 지속 시간)
        canvasGroup.DOFade(1f, fadeDuration)
            .SetEase(easeType) // 설정된 이징 타입 적용
            .SetLink(gameObject) // 오브젝트 파괴 시 트윈 자동 정리
            .OnComplete(() => // 2. 완료 시 실행되는 콜백
            {
                // 페이드 인 완료 후 상호작용 가능하게 설정
                canvasGroup.interactable = true; 
                canvasGroup.blocksRaycasts = true;
                // 외부에서 전달된 콜백 함수 실행
            });
    }

    /// <summary>
    /// Canvas Group을 페이드 아웃(Alpha 1 -> 0) 시키는 함수.
    /// 페이드 아웃 시작 시 상호작용 불가능 상태로 설정합니다.
    /// </summary>
    /// <param name="onComplete">페이드 완료 후 실행할 액션 (콜백)</param>
    public void FadeOut()
    {
        // 0. 초기화: 애니메이션 시작과 동시에 상호작용을 불가능하게 설정
        // Raycast 차단 및 Interactable 해제
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 1. DOTween 트윈 생성 및 실행
        // DOFade(목표 알파값, 지속 시간)
        canvasGroup.DOFade(0f, fadeDuration)
            .SetEase(easeType) // 설정된 이징 타입 적용
            .SetLink(gameObject) // 오브젝트 파괴 시 트윈 자동 정리
            .OnComplete(() => // 2. 완료 시 실행되는 콜백
            {
                // 외부에서 전달된 콜백 함수 실행
                gameObject.SetActive(false);
                // 일반적으로 Alpha가 0이 되면 오브젝트를 비활성화하거나 할 수 있습니다. (옵션)
            });
    }

    /// <summary>
    /// UI를 즉시 투명하게 만들고 상호작용을 비활성화합니다.
    /// </summary>
    public void SetTransparentImmediately()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}