using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
public class TimelineController : MonoBehaviour
{
    public PlayableDirector director;

    [Serializable]
    class Signal
    {
        public string sigName;
        public float sigTime;
    }

    [Serializable]
    public class Movement
    {
        public Transform targetObject;      // ������ ������Ʈ
        public Vector3 relativePosition;    // ī�޶� ���� ��� ��ġ
        public Vector3 rotation;            // ��ǥ ȸ�� (EulerAngles)
        public float time;                 // �̵� �ӵ� (units per second)

        public Movement(Transform targetObject, Vector3 relativePosition, Vector3 rotation, float time)
        {
            this.targetObject = targetObject;
            this.relativePosition = relativePosition;
            this.rotation = rotation;
            this.time = time;
        }
    }

    [SerializeField] List<Signal> signals = new();
    private Dictionary<string, float> signalLookup;

    private void Awake()
    {
        // Dictionary ĳ��
        signalLookup = new Dictionary<string, float>();

        foreach (var s in signals)
        {
            if (!signalLookup.ContainsKey(s.sigName))
            {
                signalLookup.Add(s.sigName, s.sigTime);
            }
            else
            {
                Debug.LogWarning($"Duplicate signal name detected: {s.sigName}");
            }
        }
    }

    public Movement[] movements; // ���� ���� �������� ����

    // ������ ȣ�� �Լ�
    public void PlayMovement(int currentIndex)
    {
        if (currentIndex >= movements.Length)
        {
            Debug.Log("��� ������ �Ϸ�");
            return;
        }

        Movement m = movements[currentIndex];
        ExecuteMovement(m);
        currentIndex++;
    }
    public float scaler = 0.014f;
    // Ÿ�� ��ġ�� ���� �ӵ��� �̵��ϰ� ȸ���ϱ�
    private void ExecuteMovement(Movement m)
    {
        if (m.targetObject == null)
        {
            Debug.LogWarning("Ÿ�� ������Ʈ�� �����ϴ�.");
            return;
        }

        // ī�޶� ���� �����ǥ �� ������ǥ ��ȯ
        Vector3 worldTargetPos = CalculateWorldPosition(m.relativePosition * scaler);

        // ȸ�� ��ȯ
        Quaternion targetRot = Quaternion.Euler(m.rotation);

        // �̵� �Ÿ�
        float distance = Vector3.Distance(m.targetObject.position, worldTargetPos);

        // �ӵ� ��� �̵� �ð�
        float moveDuration = m.time;

        // DoTween �̵�
        m.targetObject.DOMove(worldTargetPos, moveDuration).SetEase(Ease.Linear);

        // DoTween ȸ��
        m.targetObject.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.Linear);
    }

    Vector3 CalculateWorldPosition(Vector3 relativePos)
    {
        Transform cam = GetActiveCamera();
        return cam.transform.position + cam.transform.rotation * relativePos;
    }

    Transform GetActiveCamera()
    {
        // 그 외(빌드, 디바이스)는 기본 카메라 사용
        return Camera.main.transform;
    }


    public Transform obj;
    public Vector3 output;
    [ContextMenu("Get")]
    public void CalculateRelativeToCamera()
    {
        Camera cam = Camera.main;

        // ���� ������ ������Ʈ ��ġ �� ī�޶� ���� ���� ��ǥ�� ��ȯ
        output =  cam.transform.InverseTransformPoint(obj.position);
    }

    public void PlayFromMarker(string sigName)
    {
        if (signalLookup.TryGetValue(sigName, out float time))
        {
            director.time = time;
            director.Play();
        }
        else
        {
            Debug.LogWarning($"Signal not found: {sigName}");
        }
    }
}
