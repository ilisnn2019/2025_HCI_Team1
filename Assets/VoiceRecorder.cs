using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VoiceRecorder
{
    private const int MAX_RECORD_TIME = 60;
    private const float SILENCE_THRESHOLD = 0.01f;
    private const float SILENCE_DURATION = 2.5f;

    private AudioClip recordedClip;
    private string filePath;
    private bool isRecording = false;
    private float silenceTimer = 0f;
    private float maxTimer = 0f;
    private int sampleSize = 256;

    public event Action OnRecordingStopped;

    public VoiceRecorder()
    {
        filePath = Path.Combine(Application.dataPath, "Personals/audio.wav");
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("<color=red>[VoiceRecord]</color> No microphone found!");
            return;
        }

        Debug.Log("<color=cyan>[VoiceRecord]</color> Recording started!");
        isRecording = true;
        silenceTimer = 0f;
        maxTimer = 0f;
        recordedClip = Microphone.Start(null, false, MAX_RECORD_TIME, 16000);
        //UnityMainThreadDispatcher.Instance().Enqueue(CheckSilenceAndStop());
    }

    private IEnumerator CheckSilenceAndStop()
    {
        float[] samples = new float[sampleSize];

        while (isRecording)
        {
            if (recordedClip == null || !Microphone.IsRecording(null))
                yield break;

            int position = Microphone.GetPosition(null);
            if (position < sampleSize) continue;

            recordedClip.GetData(samples, position - sampleSize);

            bool hasSound = false;
            foreach (float sample in samples)
            {
                if (Mathf.Abs(sample) > SILENCE_THRESHOLD)
                {
                    hasSound = true;
                    break;
                }
            }

            maxTimer += 0.1f;

            if (hasSound)
            {
                silenceTimer = 0f;
            }
            else
            {
                silenceTimer += 0.1f;
            }

            if (silenceTimer >= SILENCE_DURATION || maxTimer >= 15f)
            {
                StopRecording();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    public void StopRecording()
    {
        if (!Microphone.IsRecording(null))
        {
            return;
        }

        isRecording = false;
        Microphone.End(null);

        //무음 제거 후 WAV 저장
        var trimmed = AudioUtility.TrimSilence(recordedClip, SILENCE_THRESHOLD);
        byte[] wavData = AudioUtility.SaveWav(Path.GetFileName(filePath), trimmed);

        // 파일 저장
        File.WriteAllBytes(filePath, wavData);

        OnRecordingStopped?.Invoke();
    }
}
