using UnityEngine;
using UnityEngine.Events;

public class ObjectTouchHandler : MonoBehaviour
{
    private Camera arCamera;
    public UnityEvent onObjectTouched;

    private void Start()
    {
        arCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);
        RaycastHit hit;

        // 3D Collider와 충돌 체크
        if (Physics.Raycast(ray, out hit))
        {
            GameObject touchedObject = hit.collider.gameObject;
            Debug.Log("Touched: " + touchedObject.name);

            onObjectTouched?.Invoke();
        }
    }
}

