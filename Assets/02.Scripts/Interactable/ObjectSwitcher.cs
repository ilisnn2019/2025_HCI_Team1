using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARInteractionManager : MonoBehaviour
{
    [Header("상호작용 대상 설정")]
    [Tooltip("상호작용할 AR 오브젝트(로봇)에 설정된 태그 이름입니다.")]
    [SerializeField]
    private string interactableTag = "ARObject";

    [Header("프리팹 내부 구성")]
    [Tooltip("소환된 프리팹 내부에 있는 환경 오브젝트의 이름입니다.")]
    [SerializeField]
    private string environmentChildName = "환경";
    
    [Header("UI 연결")]
    [Tooltip("이미지 트래킹 성공 시 활성화될 버튼 패널입니다.")]
    [SerializeField]
    private GameObject interactionPanel;

    // --- 내부 관리 변수 ---
    private Camera m_ArCamera;
    private GameObject m_TrackedObject; // 현재 추적 및 제어 대상인 AR 오브젝트 (로봇)
    private Animator m_Animator;
    private bool m_IsRotated = false;

    private GameObject m_EnvironmentObject; // 로봇의 형제 오브젝트인 환경 오브젝트를 참조
    private Vector3 originalPosition;

    void Start()
    {
        m_ArCamera = Camera.main;
        if(interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (m_TrackedObject == null)
        {
            // 태그로 로봇 오브젝트를 찾습니다.
            GameObject robotObject = GameObject.FindWithTag(interactableTag);
            
            if(robotObject != null)
            {
                // 로봇을 찾았으면, 그 부모(소환된 전체 프리팹)를 기준으로 초기 설정을 진행합니다.
                InitializeTrackedObject(robotObject);
            }
        }

        HandleTouch();
    }
    
    /// <summary>
    /// 추적된 로봇 오브젝트를 기준으로 초기 설정을 진행합니다.
    /// </summary>
    private void InitializeTrackedObject(GameObject robotObject)
    {
        m_TrackedObject = robotObject;
        m_Animator = m_TrackedObject.GetComponent<Animator>();
        originalPosition = m_TrackedObject.transform.localPosition;
        
        // 로봇의 부모(전체 프리팹)를 찾습니다.
        Transform parentPrefab = m_TrackedObject.transform.parent.parent;
        if(parentPrefab != null)
        {
            // 부모 안에서 이름으로 환경 오브젝트를 찾습니다.
            Transform envTransform = parentPrefab.Find(environmentChildName);
            if (envTransform != null)
            {
                m_EnvironmentObject = envTransform.gameObject;
                // 환경은 처음에는 비활성화 상태로 시작합니다.
                m_EnvironmentObject.SetActive(false);
            }
        }
        
        if(interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
        Debug.Log($"'{m_TrackedObject.name}'을 제어 대상으로 설정했습니다. UI를 활성화합니다.");
    }

    private void HandleTouch()
    {
        if (m_TrackedObject == null || Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began)
        {
            return;
        }

        Ray ray = m_ArCamera.ScreenPointToRay(Input.GetTouch(0).position);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform.CompareTag(interactableTag))
        {
            if (m_Animator != null && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
            {
                m_Animator.SetTrigger("Hello");
                Debug.Log("Hello 애니메이션 실행!");
            }
        }
    }

    #region UI 버튼 Public 함수들

    public void ToggleRotation()
    {
        if (m_TrackedObject == null) return;
        m_IsRotated = !m_IsRotated;
        m_TrackedObject.transform.localRotation = m_IsRotated ? Quaternion.Euler(-90, 0, 0) : Quaternion.Euler(0, 0, 0);
    }

    /// <summary>
    /// 주변 환경 오브젝트를 껐다 켭니다. (토글)
    /// </summary>
    public void ToggleEnvironment()
    {
        // 이제 Instantiate 대신, 찾아둔 환경 오브젝트를 SetActive로 제어합니다.
        if (m_EnvironmentObject != null)
        {
            m_EnvironmentObject.SetActive(!m_EnvironmentObject.activeSelf);
            Debug.Log($"환경 활성화 상태: {m_EnvironmentObject.activeSelf}");
        }
    }

    public void PlayDanceAnimation()
    {
        if (m_Animator != null) m_Animator.SetTrigger("Dance");
    }

    public void PlayWalkAnimation()
    {
        if (m_Animator != null) m_Animator.SetTrigger("Walk");
    }

    public void ResetObjectState()
    {
        if (m_TrackedObject == null) return;
        m_TrackedObject.transform.localPosition = originalPosition;
        m_TrackedObject.transform.localRotation = Quaternion.identity;
        m_IsRotated = false;

        if (m_Animator != null) m_Animator.SetTrigger("ResetToIdle");
        Debug.Log("캐릭터 상태를 초기화했습니다.");
    }

    #endregion
}

