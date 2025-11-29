using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordVoiceContent : MonoBehaviour
{
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI replyText;
    public Button playButton;
    public AudioSource audioSource;

    private AudioClip clip;

    public void Init(string date, string title, string content,string reply, AudioClip audioClip, AudioSource audiosource)
    {
        dateText.text = date;
        titleText.text = title;
        contentText.text = content;
        replyText.text = reply;

        clip = audioClip;

        audioSource = audiosource;

        if (playButton != null)
            playButton.onClick.AddListener(OnPlay);
    }

    private void OnPlay()
    {
        if (clip == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            return;
        }

        audioSource.clip = clip;
        audioSource.Play();
    }

}
