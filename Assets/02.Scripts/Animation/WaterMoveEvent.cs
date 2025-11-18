using UnityEngine;
using DG.Tweening; // DOTween 네임스페이스

/// <summary>
/// DOTween을 사용하여 오브젝트의 로컬 위치와 크기를 애니메이션하는 컴포넌트입니다.
/// </summary>
public class LocalDotweenAnimator : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [Tooltip("애니메이션 지속 시간 (초)")]
    [SerializeField]
    private float duration = 8f;

    // 로컬 좌표 기반의 시작/종료 값 정의
    [Tooltip("시작 로컬 Y 위치")]
    [SerializeField]
    private float startLocalY = -100f;

    [Tooltip("종료 로컬 Y 위치")]
    [SerializeField]
    private float endLocalY = 100f;

    [Tooltip("시작 스케일 (균일 스케일)")]
    [SerializeField]
    private float startScale = 0.000001f;

    [Tooltip("종료 스케일 (균일 스케일)")]
    [SerializeField]
    private float endScale = 0.1f;

    // 1. 중복 호출 방지를 위한 플래그
    private bool isAnimating = false;

    /// <summary>
    /// 오브젝트의 로컬 위치와 크기를 DOTween을 이용하여 동시에 애니메이션합니다.
    /// </summary>
    public void AnimateObjectLocal()
    {
        // 1. 중복 호출 방지: 이미 애니메이션 중이면 실행하지 않음
        if (isAnimating) return;
        
        isAnimating = true; // 애니메이션 시작 상태로 변경

        // 초기 상태 설정
        Transform targetTransform = transform;
        Vector3 currentLocalPosition = targetTransform.localPosition;
        targetTransform.localPosition = new Vector3(currentLocalPosition.x, startLocalY, currentLocalPosition.z);
        targetTransform.localScale = Vector3.one * startScale;

        // 2. 로컬 위치 애니메이션 (Y축 이동)
        targetTransform.DOLocalMoveY(endLocalY, duration)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnComplete(() => 
            {
                Debug.Log("Local Y Movement Complete");
                // 애니메이션이 끝나면 다시 실행 가능하도록 플래그 해제
                isAnimating = false; 
            });

        // 3. 크기 애니메이션 (로컬 스케일 증가)
        targetTransform.DOScale(endScale, duration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);
    }

    public void Reset()
    {
        // 2. 기존 실행 중인 DOTween 중지 (Kill)
        // 이 Transform에 연결된 모든 트윈을 즉시 멈춥니다.
        transform.DOKill();
        
        // 강제 리셋되었으므로 애니메이션 상태도 초기화
        isAnimating = false;

        // 초기 상태로 즉시 되돌리기
        Transform targetTransform = transform;
        Vector3 currentLocalPosition = targetTransform.localPosition;
        transform.localPosition = new Vector3(currentLocalPosition.x, startLocalY, currentLocalPosition.z);
        targetTransform.localScale = Vector3.one * startScale;
    }
}