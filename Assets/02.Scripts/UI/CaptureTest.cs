using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using NativeGalleryNamespace;
using DG.Tweening; // DOTween 필수

public class ScreenCaptureManager : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] GameObject[] uiToHide;        
    [SerializeField] RawImage previewImage;      
    [SerializeField] GameObject previewPanel;    

    [Header("Countdown Settings")]
    [Tooltip("카운트다운 이미지를 표시할 UI Image 컴포넌트")]
    [SerializeField] Image countdownImage; 
    
    [Tooltip("3, 2, 1 순서대로 이미지를 넣어주세요 (Index 0 = 3, Index 1 = 2, Index 2 = 1)")]
    [SerializeField] Sprite[] countdownSprites; 

    [Tooltip("숫자가 나타나는 애니메이션 시간")]
    [SerializeField] float animationDuration = 0.6f;
    [Tooltip("이미지가 커지는 최대 크기 (1이면 원본 크기, 1.2면 1.2배까지 커졌다 돌아옴)")]
    [SerializeField] float scaleFactor = 1.0f; 

    [Header("Audio Settings")]
    [Tooltip("효과음을 재생할 오디오 소스")]
    [SerializeField] AudioSource audioSource;
    [Tooltip("찰칵! 하는 셔터 효과음 파일")]
    [SerializeField] AudioClip[] shutterSounds;

    private Texture2D cachedTexture;
    private bool isCapturing = false;

    // ==========================================
    // 1단계: 화면 캡처 및 미리보기
    // ==========================================
    public void OnClickCaptureButton()
    {
        if (isCapturing) return;
        StartCoroutine(CRCaptureAndPreview());
    }

    IEnumerator CRCaptureAndPreview()
    {
        isCapturing = true;

        // =================================================================
        // [수정됨] 이미지 기반 카운트다운 로직
        // =================================================================
        if (countdownImage != null && countdownSprites != null && countdownSprites.Length > 0)
        {
            countdownImage.gameObject.SetActive(true);
            
            // 혹시라도 숨길 UI 목록에 포함되어 있다면 잠시 켜줍니다.
            if (!countdownImage.gameObject.activeSelf) 
                countdownImage.gameObject.SetActive(true);

            // 배열에 들어있는 순서대로 (3 -> 2 -> 1) 이미지를 교체하며 재생
            foreach (Sprite sprite in countdownSprites)
            {
                // 1. 이미지 교체
                countdownImage.sprite = sprite;
                // (옵션) 원본 비율 유지가 필요하면 아래 주석 해제
                // countdownImage.SetNativeSize(); 

                // 2. 초기 상태 설정: 아주 작고 투명하게
                countdownImage.transform.localScale = Vector3.one * 0.1f;
                Color c = countdownImage.color;
                c.a = 0f;
                countdownImage.color = c;

                // 3. DOTween 시퀀스 생성
                Sequence seq = DOTween.Sequence();

                // - 페이드 인 (투명도 0 -> 1)
                seq.Join(countdownImage.DOFade(1f, animationDuration * 0.5f).SetEase(Ease.OutQuad));
                // - 스케일 업 (작음 -> 목표 크기보다 살짝 커졌다가 돌아옴: Ease.OutBack)
                seq.Join(countdownImage.transform.DOScale(Vector3.one * scaleFactor, animationDuration).SetEase(Ease.OutBack));

                // 4. 1초 대기 (애니메이션 보는 시간)
                yield return new WaitForSeconds(1.0f);
            }

            // 카운트다운 종료 후 이미지 비활성화
            countdownImage.gameObject.SetActive(false);
        }
        // =================================================================
        if (audioSource != null && shutterSounds != null && shutterSounds.Length > 0)
        {
            // 0부터 (배열길이 - 1) 사이의 랜덤한 정수 인덱스 추출
            int randomIndex = Random.Range(0, shutterSounds.Length);
            
            // 랜덤하게 선택된 클립 재생
            if (shutterSounds[randomIndex] != null)
            {
                audioSource.PlayOneShot(shutterSounds[randomIndex]);
            }
        }

        if (uiToHide != null) {
            foreach(var ui in uiToHide) {
                if (ui != null) ui.SetActive(false);
            }
        }
        
        yield return new WaitForEndOfFrame();

        // 1. 메모리 정리
        if (cachedTexture != null) Destroy(cachedTexture);

        // 2. 캡처
        cachedTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        cachedTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cachedTexture.Apply();

        // 3. 프리뷰 UI 설정
        if (previewImage != null)
        {
            previewImage.texture = cachedTexture;
            AspectRatioFitter fitter = previewImage.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                fitter = previewImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; 
            }
            float screenRatio = (float)Screen.width / Screen.height;
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
    // 2단계: 갤러리에 저장
    // ==========================================
    public void OnClickSaveButton()
    {
        if (cachedTexture == null) return;

        NativeGallery.SaveImageToGallery(
            cachedTexture, "AR_Storybook", "Screenshot_{0}.png",
            (success, path) => {
                if (success) {
                    Debug.Log($"저장 성공: {path}");
                    ClosePreview();
                }
            }
        );
    }

    // ==========================================
    // 3단계: 취소 및 정리
    // ==========================================
    public void OnClickDiscardButton()
    {
        ClosePreview();
    }

    private void ClosePreview()
    {
        if (previewPanel != null) previewPanel.SetActive(false);
        if (cachedTexture != null)
        {
            Destroy(cachedTexture);
            cachedTexture = null;
        }
    }

    private void OnDestroy()
    {
        if (cachedTexture != null) Destroy(cachedTexture);
    }
}