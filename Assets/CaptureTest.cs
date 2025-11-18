using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System.IO; // System.IO 네임스페이스 추가 (폴더/경로 관리를 위해)

public class CaptureTest : MonoBehaviour
{
    [Header("스크린샷 설정")]
    // 1. Inspector에서 폴더 이름을 설정할 수 있도록 public 변수로 변경
    public string screenshotFolderName = "Screenshots";
    // 2. 스크린샷 기본 파일 이름을 설정할 수 있는 변수
    public string baseFileName = "Screenshot";

    // 3. PlayerPrefs에 사용할 키 (상수)
    private const string countKey = "ScreenshotCounter";

    // 4. 실제 저장될 폴더의 전체 경로
    private string screenshotFolderPath;

    void Awake()
    {
        // 앱이 시작될 때 플랫폼에 맞춰 저장 폴더 경로를 설정
#if UNITY_EDITOR
        // (요청사항) PC(에디터) 환경: 프로젝트 루트 (GetCurrentDirectory) 기준
        string projectRoot = Directory.GetCurrentDirectory();
        screenshotFolderPath = Path.Combine(projectRoot, screenshotFolderName);
#else
        // 모바일 환경: 앱의 영구 데이터 경로 기준
        screenshotFolderPath = Path.Combine(Application.persistentDataPath, screenshotFolderName);
#endif

        // 5. 해당 폴더가 존재하지 않으면 생성
        if (!Directory.Exists(screenshotFolderPath))
        {
            Directory.CreateDirectory(screenshotFolderPath);
            Debug.Log($"스크린샷 폴더 생성 완료: {screenshotFolderPath}");
        }
    }

    // --- 캡처 기능 (넘버링 기능 추가) ---

    public void CaptureTestMethod()
    {
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private IEnumerator CaptureScreenshotCoroutine()
    {
        yield return new WaitForEndOfFrame();

        // 6. PlayerPrefs에서 현재 카운트 불러오기 (기본값 1)
        int fileCount = PlayerPrefs.GetInt(countKey, 1);

        // 7. 파일 이름 생성 (예: "Screenshot_1.png", "Screenshot_2.png" ...)
        string fileName = $"{baseFileName}_{fileCount}.png";

        // 8. 최종 저장 경로 조합 (폴더 + 파일 이름)
        string fullSavePath = Path.Combine(screenshotFolderPath, fileName);

        // 9. 스크린샷 캡처 (파일 이름 대신 '전체 경로'를 전달)
        ScreenCapture.CaptureScreenshot(fullSavePath);

        // 10. 다음 파일 번호를 위해 카운트 증가 후 PlayerPrefs에 저장
        fileCount++;
        PlayerPrefs.SetInt(countKey, fileCount);

        // 11. 로그 출력
        Debug.Log($"스크린샷 저장 완료: {fullSavePath}");
    }

    // --- 카메라 전환 기능 (이전과 동일) ---
    public void TurnCamera()
    {
#if UNITY_EDITOR
        Debug.LogWarning("AR 카메라 전환은 PC(에디터)에서 지원되지 않습니다.");
#elif UNITY_ANDROID || UNITY_IOS
        var cameraManager = Camera.main.GetComponent<ARCameraManager>();
        
        if (cameraManager == null)
        {
            Debug.LogError("ARCameraManager를 찾을 수 없습니다.");
            return;
        }

        cameraManager.requestedFacingDirection =
            cameraManager.currentFacingDirection == CameraFacingDirection.World
            ? CameraFacingDirection.User
            : CameraFacingDirection.World;
#endif
    }
    
    // [유틸리티] 
    // 테스트 중 카운트를 리셋하고 싶을 때 사용
    [ContextMenu("스크린샷 카운트 리셋")]
    public void ResetScreenshotCount()
    {
        PlayerPrefs.SetInt(countKey, 1);
        Debug.Log("스크린샷 카운트가 1로 리셋되었습니다.");
    }
}