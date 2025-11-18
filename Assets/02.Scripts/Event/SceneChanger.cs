using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [SerializeField]
    private float fadeDuration = 1f;

    CanvasGroup canvasGroup;

    // 1. 중복 호출 방지를 위한 상태 변수 선언
    private bool isTransitioning = false;

    public void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        
        // 시작할 때는 페이드 인이므로 전환 상태가 아님 (혹은 전환 중으로 뒀다가 끝난 후 풀어줘도 됨)
        // 여기서는 씬 로드 버튼 클릭을 막는 것이 목적이므로 false로 시작합니다.
        isTransitioning = false; 
        
        AwakeFadeIn();
    }

    public void StartLoadScene(string sceneName)
    {
        // 2. 이미 전환 중(true)이라면 함수를 즉시 종료하여 중복 실행 방지
        if (isTransitioning) return;

        // 3. 전환 시작 상태로 변경 (이제 다른 호출은 무시됨)
        isTransitioning = true;

        StartCoroutine(FadeLoadScene(sceneName));
    }

    IEnumerator FadeLoadScene(string sceneName) // (오타 수정: FadeLoacdScene -> FadeLoadScene)
    {
        float elapsedTime = 0f;

        // 페이드 아웃 (화면이 점점 어두워짐/불투명해짐)
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }

        // 보간 오차 방지를 위해 루프 후 알파값 1로 확정
        canvasGroup.alpha = 1f;

        // 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    private void AwakeFadeIn()
    {
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        // 페이드 인 (화면이 점점 밝아짐/투명해짐)
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
            yield return null;
        }
        
        // 보간 오차 방지를 위해 0으로 확정
        canvasGroup.alpha = 0f;
    }

    public void Quit()
    {
        // 종료 버튼도 중복 방지가 필요하다면 여기에 체크 추가 가능
        if (isTransitioning) return;
        isTransitioning = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}