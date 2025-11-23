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
    // 이 값들은 부모 오브젝트의 Transform을 기준으로 합니다.
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

    /// <summary>
    /// 오브젝트의 로컬 위치와 크기를 DOTween을 이용하여 동시에 애니메이션합니다.
    /// </summary>
    public void AnimateObjectLocal()
    {
        // 1. 초기 상태 설정: 애니메이션 시작 전 로컬 위치와 크기를 설정합니다.
        Transform targetTransform = transform;

        // 시작 로컬 위치 설정 (Y축만 설정하고 X, Z는 현재 로컬 위치를 유지하도록 합니다.)
        // 중요한 점: DOLocalMove를 사용할 것이므로, 초기 위치를 로컬 좌표로 설정해야 합니다.
        Vector3 currentLocalPosition = targetTransform.localPosition;
        targetTransform.localPosition = new Vector3(currentLocalPosition.x, startLocalY, currentLocalPosition.z);
        
        // 시작 스케일 설정 (localScale은 기본적으로 로컬 스케일입니다.)
        targetTransform.localScale = Vector3.one * startScale;

        // 2. 로컬 위치 애니메이션 (Y축 이동)
        // DOMove 대신 DOLocalMove를 사용하여 로컬 좌표를 기준으로 이동합니다.
        targetTransform.DOLocalMoveY(endLocalY, duration)
            .SetEase(Ease.InOutSine) 
            .SetLink(gameObject) // 오브젝트 파괴 시 트윈 자동 정리
            .OnComplete(() => Debug.Log("Local Y Movement Complete")); // 콜백 예시

        // 3. 크기 애니메이션 (로컬 스케일 증가)
        // DOScale은 기본적으로 로컬 스케일을 변경합니다.
        targetTransform.DOScale(endScale, duration)
            .SetEase(Ease.OutBack);
    }

    public void Reset()
    {
        Transform targetTransform = transform;
        Vector3 currentLocalPosition = targetTransform.localPosition;
        transform.localPosition = new Vector3(currentLocalPosition.x, startLocalY, currentLocalPosition.z);
        targetTransform.localScale = Vector3.one * startScale;
    }
}