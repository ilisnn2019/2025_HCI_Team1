using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;

// XR Origin 오브젝트에 이 스크립트를 추가하세요.
public class ImageDetectionLogger : MonoBehaviour
{
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;

    void OnEnable() => m_TrackedImageManager.trackablesChanged.AddListener(OnChanged);

    void OnDisable() => m_TrackedImageManager.trackablesChanged.RemoveListener(OnChanged);

    void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            Debug.LogFormat("[Image Tracking] ADDED: '{0}' | Position: {1} | Tracking State: {2}", 
                newImage.referenceImage.name, 
                newImage.transform.position, 
                newImage.trackingState);
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            // Handle updated event
        }

        foreach (var removedImage in eventArgs.removed)
        {
            // Handle removed event
        }
    }
}