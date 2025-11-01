using UnityEngine;

public class TouchStep : QuestStep
{
    [Header("Target Interaction")]
    [Tooltip("사용자가 클릭/터치해야 할 대상 오브젝트의 Collider")]
    [SerializeField]
    private Collider targetCollider;

    private Camera raycastCamera;

    private void Awake()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;

            if (raycastCamera == null)
            {
                Debug.LogError($"'{this.name}': 카메라 찾을 수 없음");
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // 3. 타겟 콜라이더가 설정되었는지 확인
            if (targetCollider == null)
            {
                Debug.LogWarning($"'{this.name}' :'Target Collider' 설정안됨");
                return;
            }

            if (raycastCamera == null) return;

            // 스크린 좌표 가져오기
            Vector3 inputPosition = Vector3.zero;
            if (Input.GetMouseButtonDown(0))
            {
                inputPosition = Input.mousePosition; // 마우스 위치
            }
            else
            {
                inputPosition = Input.GetTouch(0).position; // 터치 위치
            }

            Ray ray = raycastCamera.ScreenPointToRay(inputPosition);

            // Raycast 수행

            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // UI를 클릭한 것이므로 3D 오브젝트 반응 안 함
            }


            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Raycast에 맞은 Collider가 우리가 지정한 'targetCollider'인지 확인
                if (hit.collider == targetCollider)
                {
                    Debug.Log($"[TouchOrClickStep] Target '{targetCollider.name}'이(가) 클릭/터치되었습니다.");
                    
                    // 조건 충족! 퀘스트 스텝 완료
                    FinishQuestStep();
                }
            }
        }
    }
}