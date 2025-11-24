using System.Collections;
using Unity.Collections; // NativeSlice를 위해 필수
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class SafeCameraSwapper : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ARSession m_Session;
        [SerializeField] private ARCameraManager m_CameraManager;
        [SerializeField] private ARTrackedImageManager m_ImageManager;

        private bool m_IsSwapping = false;

        // 수정된 Chooser 인스턴스
        private readonly ConfigurationChooser m_SimpleChooser = new SimpleFirstAvailableChooser();

        void Awake()
        {
            if (!m_Session) m_Session = FindFirstObjectByType<ARSession>();
            if (!m_CameraManager) m_CameraManager = FindFirstObjectByType<ARCameraManager>();
            if (!m_ImageManager) m_ImageManager = FindFirstObjectByType<ARTrackedImageManager>();
        }

        public void SwapCamera()
        {
            if (m_IsSwapping) return;
            StartCoroutine(SwapSequence());
        }

        private IEnumerator SwapSequence()
        {
            m_IsSwapping = true;
            
            // 1. 방향 결정
            var currentFacing = m_CameraManager.requestedFacingDirection;
            var newFacing = (currentFacing == CameraFacingDirection.World) 
                            ? CameraFacingDirection.User 
                            : CameraFacingDirection.World;

            Debug.Log($"[Swap] Start Sequence: {currentFacing} -> {newFacing}");

            // ----------------------------------------------------------------
            // STEP 1: 안전 종료
            // ----------------------------------------------------------------
            if (m_ImageManager) m_ImageManager.enabled = false;
            if (m_Session) m_Session.enabled = false;
            
            yield return new WaitForSeconds(0.2f);

            // ----------------------------------------------------------------
            // STEP 2: 설정 변경 (Chooser 주입)
            // ----------------------------------------------------------------
            if (m_Session && m_Session.subsystem != null)
            {
                m_Session.subsystem.configurationChooser = m_SimpleChooser;
            }

            m_CameraManager.requestedFacingDirection = newFacing;

            // ----------------------------------------------------------------
            // STEP 3: 재시작
            // ----------------------------------------------------------------
            if (m_Session) m_Session.enabled = true;
            
            yield return new WaitForSeconds(0.5f);

            // ----------------------------------------------------------------
            // STEP 4: 기능 복구
            // ----------------------------------------------------------------
            if (newFacing == CameraFacingDirection.World)
            {
                if (m_ImageManager) m_ImageManager.enabled = true;
            }

            m_IsSwapping = false;
        }
    }

    /// <summary>
    /// [Unity 6 호환] NativeSlice로 매개변수 타입 수정 완료
    /// </summary>
    public class SimpleFirstAvailableChooser : ConfigurationChooser
    {
        // 여기 타입을 NativeArray -> NativeSlice로 변경했습니다.
        public override Configuration ChooseConfiguration(NativeSlice<ConfigurationDescriptor> descriptors, Feature requestedFeatures)
        {
            // 유효한 구성이 하나라도 있으면
            if (descriptors.Length > 0)
            {
                // 첫 번째(0번) 구성을 기반으로 Configuration 객체를 생성하여 반환
                // 보통 0번이 시스템이 추천하는 가장 적절한 해상도입니다.
                return new Configuration(descriptors[0], requestedFeatures);
            }

            // 없으면 기본값
            return default;
        }
    }
}