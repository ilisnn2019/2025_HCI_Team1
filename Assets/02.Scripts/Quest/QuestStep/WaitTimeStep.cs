using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitTimeStep : QuestStep
{
    [SerializeField] private float time = 5f; // 사운드 없으면 기본대기시간
    Coroutine timerCoroutine;
    float alphaTime = 1f;   // 사운드 재생시간에 더할 시간

    public override void OnEnable()
    {
        if(stepAudioClip != null)
        {
            time = stepAudioClip.length + alphaTime;
        }
        timerCoroutine = StartCoroutine(StartTimer());
    }
    private void OnDisable()
    {
        StopCoroutine(timerCoroutine);
    }
    IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(time);

        FinishQuestStep();
    }
}
