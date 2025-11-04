using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// ARAnchorManager는 씬에 존재해야 하지만, 이 스크립트에서 직접 참조할 필요는 없습니다.
public class ImageToAnchorCreator : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager m_TrackedImageManager;

    [SerializeField]
    private GameObject m_PrefabToAnchor;

    private readonly Dictionary<TrackableId, bool> m_HasAnchorForImage = new Dictionary<TrackableId, bool>();

    void OnEnable()
    {
        m_TrackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    void OnDisable()
    {
        m_TrackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var updatedImage in args.updated)
        {
            if (m_HasAnchorForImage.TryGetValue(updatedImage.trackableId, out bool hasAnchor) && hasAnchor)
            {
                continue;
            }

            if (updatedImage.trackingState == TrackingState.Tracking)
            {
                // --- AR Foundation 6.0 방식의 앵커 생성 ---

                // 1. 앵커 역할을 할 빈 게임 오브젝트를 이미지의 위치에 생성합니다.
                GameObject anchorGO = new GameObject("ImageAnchor_" + updatedImage.referenceImage.name);
                anchorGO.transform.SetPositionAndRotation(updatedImage.transform.position, updatedImage.transform.rotation);

                // 2. [핵심] 생성된 게임 오브젝트에 ARAnchor 컴포넌트를 추가합니다.
                //    이것만으로 AR 시스템이 이 오브젝트를 앵커로 인식하고 추적을 시작합니다.
                var anchor = anchorGO.AddComponent<ARAnchor>();

                // 3. 앵커가 유효하다면(컴포넌트 추가 성공), 그 앵커의 자식으로 프리팹을 생성합니다.
                if (anchor != null)
                {
                    Instantiate(m_PrefabToAnchor, anchor.transform);

                    m_HasAnchorForImage[updatedImage.trackableId] = true;

                    Debug.Log($"'{updatedImage.referenceImage.name}' 위치에 ARAnchor 컴포넌트를 추가하여 오브젝트를 고정했습니다!");
                }
            }
        }
    }
}