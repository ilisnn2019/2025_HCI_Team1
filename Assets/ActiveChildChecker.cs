using UnityEngine;

public class ActiveChildChecker : MonoBehaviour
{
    private Transform targetRoot;

    [Tooltip("최적화를 위한 검사 간격 (초 단위). 0이면 매 프레임 검사.")]
    [SerializeField] private float checkInterval = 0.2f;

    [Header("제어할 오브젝트")]
    [Tooltip("활성화된 자식이 '하나도 없으면' 켜질 오브젝트")]
    [SerializeField] private GameObject informationText;

    private float _timer;

    private void Start()
    {
        // targetRoot가 할당되지 않았으면 자기 자신을 기준으로 함
        if (targetRoot == null)
        {
            targetRoot = transform;
        }
        
        // 시작하자마자 한 번 실행
        CheckChildrenState();
    }

    private void Update()
    {
        // 타이머 로직 (최적화)
        _timer += Time.deltaTime;
        if (_timer >= checkInterval)
        {
            _timer = 0f;
            CheckChildrenState();
        }
    }

    private void CheckChildrenState()
    {
        bool hasActiveChild = false;
        int childCount = targetRoot.childCount;

        // 자식들을 순회하며 활성화된 녀석이 있는지 확인
        for (int i = 0; i < childCount; i++)
        {
            Transform child = targetRoot.GetChild(i);
            
            // 만약 자식이 활성화(ActiveSelf) 상태라면
            if (child.gameObject.activeSelf)
            {
                hasActiveChild = true;
                break; // 하나라도 찾았으니 더 이상 검사할 필요 없음 (최적화)
            }
        }

        // 상태에 따른 오브젝트 제어
        // 불필요한 SetActive 호출을 막기 위해 현재 상태와 다를 때만 호출 (최적화)
        if (hasActiveChild)
        {
            // 활성화된 자식이 있음 -> 끄기로 한 건 끄고, 켜기로 한 건 (조건에 맞지 않으니) 끔?
            // 요청하신 내용: "있다면 끄고(Disable), 없다면 키고(Enable)"
            
            if (informationText != null && informationText.activeSelf)
                informationText.SetActive(false);
            
            if (informationText != null && informationText.activeSelf) 
                informationText.SetActive(false); // *보통 반대되는 오브젝트는 끕니다.
        }
        else
        {
            // 활성화된 자식이 없음 -> 켜기로 한 건 킴
            
            if (informationText != null && !informationText.activeSelf)
                informationText.SetActive(true); // *상황이 해제되었으니 다시 켜거나 유지

            if (informationText != null && !informationText.activeSelf)
                informationText.SetActive(true);
        }
    }
}