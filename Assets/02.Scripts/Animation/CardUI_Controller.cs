using UnityEngine;

/// <summary>
/// [신규] Card_ImageTracker의 이벤트를 구독(구독)하여
/// 실제 UI를 제어하는 'UI 담당' 스크립트입니다.
/// </summary>
public class Card_UI_Controller : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField]
    private GameObject multipleCardsCanvas; // 2개 이상일 때 켤 캔버스

    private void OnEnable()
    {
        EventManager.onSingleCardTracked += HideMultipleCardUI;
        EventManager.onMultipleCardsTracked += ShowMultipleCardUI;
        EventManager.onNoCardsTracked += HideMultipleCardUI;
    }

    private void OnDisable()
    {
        EventManager.onSingleCardTracked -= HideMultipleCardUI;
        EventManager.onMultipleCardsTracked -= ShowMultipleCardUI;
        EventManager.onNoCardsTracked -= HideMultipleCardUI;
    }

    public void ShowMultipleCardUI()
    {
        if (multipleCardsCanvas != null)
            multipleCardsCanvas.SetActive(true);
    }

    /// <summary>
    /// '2개 이상' 캔버스를 비활성화합니다.
    /// (onSingleCardTracked, onNoCardsTracked 이벤트에 연결)
    /// </summary>
    public void HideMultipleCardUI()
    {
        if (multipleCardsCanvas != null)
            if(multipleCardsCanvas.GetComponent<CanvasGroupFader>() != null)
                multipleCardsCanvas.GetComponent<CanvasGroupFader>().FadeOut();
            else
                multipleCardsCanvas.SetActive(false);
    }
}