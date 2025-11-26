using UnityEngine;
using UnityEngine.Playables;

public class TimelineSignalLooper : MonoBehaviour
{
    public PlayableDirector director;

    [SerializeField]private double loopStartTime = 0;
    [SerializeField]private double loopEndTime = 0;
    [SerializeField]private bool looping = false;
    [SerializeField]private bool stopLoop = false;

    public void OnLoopStart()
    {
        loopStartTime = director.time; // start 될때 기록
        looping = true;
        stopLoop = false;
        Debug.Log($"[SignalLooper] Loop Start at {loopStartTime}");
    }

    public void OnLoopEnd()
    {
        loopEndTime = director.time;
        if (looping && !stopLoop)
        {
            // 루프 계속 — 다시 시작 위치로 이동
            director.time = loopStartTime;
            director.Evaluate(); // 즉시 반영
            director.Play();
            Debug.Log("[SignalLooper] Looping again...");
        }
        else
        {
            // 루프 종료
            looping = false;
            Debug.Log("[SignalLooper] Loop ended, timeline continues.");
        }
    }

    // 외부 이벤트로 호출
    [ContextMenu("Stop")]
    public void StopLoop()
    {
        stopLoop = true;
        director.time = loopEndTime;
        Debug.Log("[SignalLooper] StopLoop called — will exit after current cycle.");
    }
}
