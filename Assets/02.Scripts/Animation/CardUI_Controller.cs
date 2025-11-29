using UnityEngine;

/// <summary>
/// [수정됨] Card_ImageTracker의 이벤트를 구독하여
/// '스캔 대기 UI'와 '다중 인식 UI'를 상태에 맞게 제어하는 스크립트입니다.
/// </summary>
public class Card_UI_Controller : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField]
    private GameObject multipleCardsUI; // 2개 이상일 때 켤 캔버스

    [SerializeField]
    private GameObject scanningGuideUI;      // [신규] 아무것도 인식 안 될 때 켤 캔버스 (스캔 가이드 등)

    private void OnEnable()
    {
        // 이벤트 발생 시 실행할 동작을 명확한 상태 처리 메서드로 연결
        EventManager.onSingleCardTracked += HandleSingleCard;
        EventManager.onMultipleCardsTracked += HandleMultipleCards;
        EventManager.onNoCardsTracked += HandleNoCards;
    }

    private void OnDisable()
    {
        EventManager.onSingleCardTracked -= HandleSingleCard;
        EventManager.onMultipleCardsTracked -= HandleMultipleCards;
        EventManager.onNoCardsTracked -= HandleNoCards;
    }

    // ========================================================================
    // 상태 처리 핸들러 (State Handlers)
    // ========================================================================

    /// <summary>
    /// 아무것도 인식되지 않았을 때: 스캔 UI 켜기 / 나머지 끄기
    /// </summary>
    private void HandleNoCards()
    {
        SetUIState(scanningGuideUI, true);       // 스캔 UI 보임
        SetUIState(multipleCardsUI, false); // 다중 UI 숨김
    }

    /// <summary>
    /// 카드 1장 인식 시: 모든 오버레이 UI 끄기 (AR 오브젝트 집중)
    /// </summary>
    private void HandleSingleCard()
    {
        SetUIState(scanningGuideUI, false);      // 스캔 UI 숨김
        SetUIState(multipleCardsUI, false); // 다중 UI 숨김
    }

    /// <summary>
    /// 카드 2장 이상 인식 시: 다중 인식 UI 켜기 / 스캔 UI 끄기
    /// </summary>
    private void HandleMultipleCards()
    {
        SetUIState(scanningGuideUI, false);      // 스캔 UI 숨김
        SetUIState(multipleCardsUI, true);  // 다중 UI 보임
    }

    // ========================================================================
    // 유틸리티 메서드 (Helper Methods)
    // ========================================================================

    /// <summary>
    /// 대상 UI 오브젝트를 켜거나 끕니다. 
    /// CanvasGroupFader가 있다면 페이드 효과를 사용하고, 없다면 SetActive를 사용합니다.
    /// </summary>
    /// <param name="uiObject">제어할 UI 게임오브젝트</param>
    /// <param name="isActive">활성화 여부</param>
    private void SetUIState(GameObject uiObject, bool isActive)
    {
        if (uiObject == null) return;

        // Fader 컴포넌트 확인
        var fader = uiObject.GetComponent<CanvasGroupFader>();

        if (isActive)
        {
            // 켜는 로직
            uiObject.SetActive(true); // Fader가 있어도 일단 켜야 함
            if (fader != null)
            {
                // Fader가 있다면 FadeIn (혹은 Reset) 로직이 필요할 수 있음. 
                // 여기서는 Fader 스크립트 구현에 따라 다르겠지만, 보통 켤때는 그냥 켜거나 FadeIn을 호출
                // fader.FadeIn(); // 만약 FadeIn 기능이 있다면 사용
            }
        }
        else
        {
            // 끄는 로직
            if (fader != null)
            {
                fader.FadeOut(); // Fader가 있으면 페이드 아웃
            }
            else
            {
                uiObject.SetActive(false); // 없으면 즉시 비활성화
            }
        }
    }
}