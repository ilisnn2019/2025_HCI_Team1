using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using static UnityEditor.Rendering.CameraUI;

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
        public Transform targetObject;      // 움직일 오브젝트
        public Vector3 relativePosition;    // 카메라 기준 상대 위치
        public Vector3 rotation;            // 목표 회전 (EulerAngles)
        public float time;                 // 이동 속도 (units per second)

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
        // Dictionary 캐싱
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

    public Movement[] movements; // 여러 개의 움직임을 저장

    // 움직임 호출 함수
    public void PlayMovement(int currentIndex)
    {
        if (currentIndex >= movements.Length)
        {
            Debug.Log("모든 움직임 완료");
            return;
        }

        Movement m = movements[currentIndex];
        ExecuteMovement(m);
        currentIndex++;
    }

    // 타겟 위치로 일정 속도로 이동하고 회전하기
    private void ExecuteMovement(Movement m)
    {
        if (m.targetObject == null)
        {
            Debug.LogWarning("타겟 오브젝트가 없습니다.");
            return;
        }

        // 카메라 기준 상대좌표 → 월드좌표 변환
        Vector3 worldTargetPos = CalculateWorldPosition(m.relativePosition);

        // 회전 변환
        Quaternion targetRot = Quaternion.Euler(m.rotation);

        // 이동 거리
        float distance = Vector3.Distance(m.targetObject.position, worldTargetPos);

        // 속도 기반 이동 시간
        float moveDuration = m.time;

        // DoTween 이동
        m.targetObject.DOMove(worldTargetPos, moveDuration).SetEase(Ease.Linear);

        // DoTween 회전
        m.targetObject.DORotateQuaternion(targetRot, moveDuration).SetEase(Ease.Linear);
    }

    Vector3 CalculateWorldPosition(Vector3 relativePos)
    {
        Camera cam = Camera.main;
        return cam.transform.position + cam.transform.rotation * relativePos;
    }

    // ────────────────────────────────────────────────
    // 유틸성 함수 : 카메라 기준 상대 좌표 → 월드 좌표 변환
    // ────────────────────────────────────────────────
    public Transform obj;
    public Vector3 output;
    [ContextMenu("Get")]
    public void CalculateRelativeToCamera()
    {
        Camera cam = Camera.main;

        // 월드 공간의 오브젝트 위치 → 카메라 로컬 공간 좌표로 변환
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
