using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// QuestStep 스크립트는 상속해서 각 단계의 퀘스트를 구현할 수 있는 클래스
public abstract class QuestStep : MonoBehaviour
{
    [SerializeField] private bool isFinished = false; // 퀘스트 완료 여부
    public Quest quest; // 소속된 퀘스트
    public GameObject[] EnableDisableObjects;
    public AudioClip stepAudioClip;

    public UnityEvent OnStepStart;
    public UnityEvent OnStepFinished;
    //private
    private AudioSource audioSource;

    // 초기화
    public void InitializeQuestStep(Quest quest)
    {
        isFinished = false;
        this.quest = quest;
        audioSource = quest.GetComponent<AudioSource>() ?? quest.gameObject.AddComponent<AudioSource>();
    }

    // 단계 시작
    public virtual void OnEnable()
    {
        if(audioSource != null && stepAudioClip != null)
        {
            audioSource.PlayOneShot(stepAudioClip);
        }
            
    }

    // 단계 완료
    protected void FinishQuestStep()
    {
        if (!isFinished)
        {
            isFinished = true;
            EventManager.AdvanceQuest(quest);
            //if (needSyncReset) ScenarioSync.Main.RPC_ResetAllState();
            gameObject.SetActive(false);
        }
    }
}
