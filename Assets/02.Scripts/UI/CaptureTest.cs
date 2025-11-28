using System.Collections;
using UnityEngine;
using UnityEngine.UI; // UI 관련 기능을 위해 필수
using NativeGalleryNamespace;

public class ScreenCaptureManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] GameObject[] uiToHide;        // 캡처 시 숨길 UI (버튼 등)
    [SerializeField] RawImage previewImage;      // 캡처된 화면을 보여줄 UI (RawImage 권장)
    [SerializeField] GameObject previewPanel;    // 캡처 후 활성화될 프리뷰 패널 (저장/취소 버튼 포함)

    // 캡처된 텍스처를 임시 저장할 변수
    private Texture2D cachedTexture;
    private bool isCapturing = false;

    // ==========================================
    // 1단계: 화면 캡처 및 미리보기 (Capture & Preview)
    // ==========================================
    public void OnClickCaptureButton()
    {
        if (isCapturing) return;
        StartCoroutine(CRCaptureAndPreview());
    }

    IEnumerator CRCaptureAndPreview()
    {
        isCapturing = true;

        if (uiToHide != null) {
            foreach(var ui in uiToHide) {
                if (ui != null) ui.SetActive(false);
            }
        }
        
        yield return new WaitForEndOfFrame();

        // 1. 메모리 정리
        if (cachedTexture != null) Destroy(cachedTexture);

        // 2. 캡처 (S21 Ultra의 해상도 그대로 캡처됨)
        cachedTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        cachedTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cachedTexture.Apply();

        // 3. 프리뷰 UI에 적용 및 비율 보정 (핵심 부분)
        if (previewImage != null)
        {
            previewImage.texture = cachedTexture;

            // [추가된 로직] RawImage에 붙어있는 AspectRatioFitter 컴포넌트를 가져옴
            AspectRatioFitter fitter = previewImage.GetComponent<AspectRatioFitter>();
            
            // 만약 컴포넌트가 없다면 코드로 즉석에서 추가 (안전장치)
            if (fitter == null)
            {
                fitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();
                // 부모 크기 안에서 비율을 유지하며 최대 크기로 맞춤
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; 
            }

            // 현재 화면의 가로/세로 비율을 계산 (S21 Ultra라면 약 0.45)
            float screenRatio = (float)Screen.width / Screen.height;
            
            // 프리뷰 UI의 비율을 캡처된 화면 비율과 강제로 일치시킴
            fitter.aspectRatio = screenRatio;
        }

        if (uiToHide != null) {
            foreach(var ui in uiToHide) {
                if (ui != null) ui.SetActive(true);
            }
        }
        if (previewPanel != null) previewPanel.SetActive(true);

        isCapturing = false;
    }

    // ==========================================
    // 2단계: 갤러리에 저장 (Save to Gallery)
    // ==========================================
    public void OnClickSaveButton()
    {
        if (cachedTexture == null)
        {
            Debug.LogWarning("[ScreenCapture] 저장할 캡처 이미지가 없습니다.");
            return;
        }

        // Native Gallery를 통해 저장
        NativeGallery.SaveImageToGallery(
            cachedTexture,
            "AR_Storybook",
            "Screenshot_{0}.png",
            (success, path) =>
            {
                if (success)
                {
                    Debug.Log($"[ScreenCapture] 갤러리 저장 성공: {path}");
                    ClosePreview(); // 저장 후 프리뷰 닫기
                }
                else
                {
                    Debug.LogError("[ScreenCapture] 갤러리 저장 실패");
                }
            }
        );
    }

    // ==========================================
    // 3단계: 취소 및 메모리 정리 (Discard)
    // ==========================================
    public void OnClickDiscardButton()
    {
        Debug.Log("[ScreenCapture] 저장 취소");
        ClosePreview();
    }

    private void ClosePreview()
    {
        if (previewPanel != null) previewPanel.SetActive(false);

        // 선택 사항: 프리뷰를 닫을 때 텍스처를 바로 메모리에서 날릴지, 
        // 아니면 다음 캡처 때 덮어쓸지 결정해야 합니다.
        // 모바일은 메모리가 중요하므로 닫을 때 날리는 것을 추천합니다.
        if (cachedTexture != null)
        {
            Destroy(cachedTexture);
            cachedTexture = null;
        }
    }

    // 객체가 파괴될 때 남은 텍스처 정리 (안전장치)
    private void OnDestroy()
    {
        if (cachedTexture != null)
        {
            Destroy(cachedTexture);
        }
    }
}