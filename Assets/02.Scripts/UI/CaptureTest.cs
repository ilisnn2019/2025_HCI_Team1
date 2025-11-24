using System.Collections;
using UnityEngine;
// Native Gallery 네임스페이스 추가
using NativeGalleryNamespace; 

public class ScreenCaptureManager : MonoBehaviour
{
    [SerializeField]
    GameObject canvasUI;
    [SerializeField]
    bool withUI = true;

    // 중복 캡처 방지용 플래그
    private bool onCapture = false;

    // UI 버튼에 연결할 함수
    public void PressBtnCapture()
    {
        // 캡처 중이면 중복 실행 방지
        if (onCapture) return;

        StartCoroutine(CRSaveScreenshot());
    }

    IEnumerator CRSaveScreenshot()
    {
        onCapture = true;

        // 1. UI를 숨겨야 한다면 여기서 숨김 처리 (Canvas.enabled = false 등)
        if(!withUI)
        {
            canvasUI.SetActive(false);
            yield return null; // UI가 사라질 때까지 한 프레임 대기
        }

        // 2. 렌더링이 끝날 때까지 대기 (필수)
        yield return new WaitForEndOfFrame();

        // 3. 스크린샷을 담을 텍스처 생성
        // Screen.width, Screen.height 크기의 RGB24 포맷 텍스처 생성
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        
        // 4. 현재 화면의 픽셀을 읽어와 텍스처에 씀
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        // 5. 갤러리에 저장 (Native Gallery 핵심 기능)
        // - "AR_Storybook": 갤러리에 생성될 앨범(폴더) 이름
        // - "Screenshot_{0}.png": 파일명 ({0}은 자동으로 타임스탬프 숫자로 치환됨)
        // - callback: 저장 완료 후 실행될 람다 함수
        bool permission = false;
        NativeGallery.SaveImageToGallery(
            ss, 
            "AR_Storybook", 
            "Screenshot_{0}.png", 
            (success, path) => 
            {
                if (success)
                {
                    Debug.Log($"[ScreenCapture] 저장 성공: {path}");
                    // 여기에 "저장되었습니다" 토스트 메시지 등을 띄우는 로직 추가 가능
                }
                else
                {
                    Debug.LogError("[ScreenCapture] 저장 실패");
                }
                permission = success;
            }
        );

        // 6. 메모리 누수 방지를 위해 텍스처 파괴
        Destroy(ss);

        // 7. UI를 다시 켜야 한다면 여기서 복구
        if(!withUI)
        {
            canvasUI.SetActive(true);
        }

        Debug.Log($"[ScreenCapture] 권한 상태: {permission}");

        // 캡처 로직 종료
        onCapture = false;
    }
}