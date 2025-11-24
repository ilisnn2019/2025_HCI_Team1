using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// AR 세션의 ConfigurationChooser(하드웨어 구성 선택 전략)를 동적으로 교체하는 클래스
    /// </summary>
    public class CustomConfigurationChooser : MonoBehaviour
    {
        [Header("Target AR Session")]
        [SerializeField]
        ARSession m_Session;

        /// <summary>
        /// 외부에서 접근 가능한 ARSession 프로퍼티
        /// </summary>
        public ARSession session
        {
            get => m_Session;
            set => m_Session = value;
        }

        // 메모리 최적화를 위해 전략 인스턴스를 static readonly로 미리 생성
        static readonly ConfigurationChooser m_DefaultConfigurationChooser = new DefaultConfigurationChooser();
        static readonly ConfigurationChooser m_PreferCameraConfigurationChooser = new PreferCameraConfigurationChooser();

        void Start()
        {
            // 시작 시 ARSession이 할당되지 않았다면 컴포넌트 검색
            if (m_Session == null)
                m_Session = GetComponent<ARSession>();
            
            // 초기 상태는 PreferCamera로 강제 설정하거나, 필요에 따라 Default로 둠
            // 여기서는 요청하신 대로 'PreferCamera'를 기본으로 적용해 둠
            ApplyPreferCamera();
        }

        /// <summary>
        /// [핵심 기능] AR 세션의 구성을 '카메라 성능 우선(Prefer Camera)' 모드로 변경합니다.
        /// 버튼 이벤트나 다른 스크립트에서 이 함수를 직접 호출하세요.
        /// </summary>
        public void ApplyPreferCamera()
        {
            if (!ValidateSession()) return;

            Debug.Log("[ConfigurationChooser] Switching to 'Prefer Camera' mode.");
            m_Session.subsystem.configurationChooser = m_PreferCameraConfigurationChooser;
        }

        /// <summary>
        /// AR 세션의 구성을 '기본(Default)' 모드로 복구합니다.
        /// </summary>
        public void ApplyDefault()
        {
            if (!ValidateSession()) return;

            Debug.Log("[ConfigurationChooser] Switching to 'Default' mode.");
            m_Session.subsystem.configurationChooser = m_DefaultConfigurationChooser;
        }

        /// <summary>
        /// 세션 유효성 검사 (방어 코드)
        /// </summary>
        private bool ValidateSession()
        {
            if (m_Session == null || m_Session.subsystem == null)
            {
                Debug.LogWarning("[ConfigurationChooser] ARSession or Subsystem is null. Cannot change configuration.");
                return false;
            }
            return true;
        }
    }
}