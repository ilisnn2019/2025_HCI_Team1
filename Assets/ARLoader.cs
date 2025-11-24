using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

public class ARLoaderController : MonoBehaviour
{
    [Header("초기화 후 켜질 AR 오브젝트들")]
    [Tooltip("AR Session과 XR Origin을 여기에 할당하세요")]
    [SerializeField] private GameObject[] arObjectsToActivate;
    [SerializeField] bool activateOnStart = true;

    void Awake()
    {
        // 시작하자마자 코루틴 실행
        if(activateOnStart)
            StartCoroutine(InitializeXR());
    }

    public void StartXR()
    {
        StartCoroutine(InitializeXR());
    }

    IEnumerator InitializeXR()
    {
        // 1. XR Manager 확인
        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            // 2. 로더가 꺼져있다면 초기화 시도
            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.Log("[ARLoader] XR 로더 초기화 중...");
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            }

            // 3. 로더가 성공적으로 켜졌다면 서브시스템 시작
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                Debug.Log("[ARLoader] 서브시스템 시작");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                
                // [핵심 수정] 시스템 준비가 끝난 뒤에 AR 오브젝트들을 켜줍니다.
                yield return null; // 한 프레임 대기 (안전장치)
                foreach (var obj in arObjectsToActivate)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("[ARLoader] XR 로더 초기화 실패! Project Settings > XR Plug-in Management를 확인하세요.");
            }
        }
    }
}