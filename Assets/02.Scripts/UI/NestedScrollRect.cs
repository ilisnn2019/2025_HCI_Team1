using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


// 가로 이동의 편의성을 위한 ScrollRect
public class NestedScrollRect : ScrollRect
{
    [SerializeField] private ScrollRect parentScrollRect;

    protected override void Start()
    {
        base.Start();
        if (parentScrollRect == null)
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                parentScrollRect = parent.GetComponent<ScrollRect>();
                if (parentScrollRect != null && parentScrollRect.horizontal)
                {
                    break;
                }
                parent = parent.parent;
            }
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnBeginDrag(eventData);
            }
        }
        else
        {
            base.OnBeginDrag(eventData);
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
        else
        {
            base.OnDrag(eventData);
        }
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y))
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
            }
        }
        else
        {
            base.OnEndDrag(eventData);
        }
    }
}