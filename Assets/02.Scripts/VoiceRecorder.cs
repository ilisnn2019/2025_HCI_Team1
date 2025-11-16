using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class VoiceRecorder : MonoBehaviour
{
    [Header("UI Components")]
    public Image img_record;
    public Sprite spt_record;
    public Sprite spt_stop;
    public Sprite spt_re_record;

    private enum RecordState { Idle, Recording, Finished }
    private RecordState currentState = RecordState.Idle;

    private const int MAX_RECORD_TIME = 30;
    private const float SILENCE_THRESHOLD = 0.01f;
    private const float SILENCE_DURATION = 4f;
    private const int SAMPLE_SIZE = 256;
    private const string FILE_NAME = "voice_record.wav";
   

    private string filepath;
    private bool isRecording = false;
    private bool isMicOpen = false;
    private bool micDetected = false;
    private bool exception1Enabled = true;
    private float currentVolume = 0f;
    private float silenceTimer = 0f;
    private float maxTimer = 0f;

    // === 마이크 관련 ===
    private AudioClip micClip;
    private string micDevice;
    private Coroutine micMonitorCoroutine;

    // 녹음 구간
    private int recordStartPos = 0;
    private int recordEndPos = 0;

    public UnityEvent<string> onTimelineHandler;

    private void Awake()
    {
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        filepath = Path.Combine(dir, FILE_NAME);
    }

    private void Start()
    {
        img_record.sprite = spt_record;

        exception1Enabled = true;
    }

    // --- 마이크 열기 (감지용 + 녹음용 통합) ---
    public void OpenMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        if (isMicOpen)
        {
            Debug.LogWarning("Microphone already open!");
            return;
        }

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, 10, 16000); // 루프 버퍼 10초
        isMicOpen = true;

        Debug.Log($"Microphone opened for monitoring: {micDevice}");
        micMonitorCoroutine = StartCoroutine(MonitorMicLevel());
    }

    public void CloseMicrophone()
    {
        if (!isMicOpen) return;

        if (Microphone.IsRecording(micDevice))
            Microphone.End(micDevice);

        if (micMonitorCoroutine != null)
            StopCoroutine(micMonitorCoroutine);

        micClip = null;
        isMicOpen = false;
        micDetected = false;

        Debug.Log("Microphone monitoring stopped.");
    }


    // --- 실시간 볼륨 감지 ---
    private float voiceDetectTimer = 0f;
    private float exceptionCooldownTimer = 0f;
    private const float DETECT_DURATION = 2f;    // 2초 연속 감지 필요
    private const float EXCEPTION_COOLDOWN = 2f; // Exception UI 유지 시간


    private IEnumerator MonitorMicLevel()
    {
        float[] samples = new float[128];

        while (isMicOpen)
        {
            int micPos = Microphone.GetPosition(micDevice) - samples.Length;
            if (micPos < 0)
            {
                yield return null;
                continue;
            }

            micClip.GetData(samples, micPos);

            //RMS
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            float rms = Mathf.Sqrt(sum / samples.Length);
            currentVolume = Mathf.Clamp01(rms * 10f);

            micDetected = rms > SILENCE_THRESHOLD;

            // 예외 처리 동작할 동안 쿨타임
            //녹음 중이 아닐 때 감지
            if (!isRecording && exception1Enabled)
            {
                if (micDetected)
                {
                    voiceDetectTimer += 0.4f;

                    if (voiceDetectTimer >= DETECT_DURATION)
                    {
                        Debug.Log("Exception1 Triggered: 2초 이상 연속 감지됨");

                        onTimelineHandler?.Invoke("Exception1");

                        // 최초 안내 1회만 발생하도록 OFF
                        exception1Enabled = false;

                        exceptionCooldownTimer = EXCEPTION_COOLDOWN;
                        voiceDetectTimer = 0f;
                    }
                }
                else
                {
                    voiceDetectTimer -= 0.1f;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // --- 버튼 핸들러 ---
    public void OnRecordButtonPressed()
    {
        switch (currentState)
        {
            case RecordState.Idle:
                StartRecording();
                break;

            case RecordState.Recording:
                StopRecording();
                break;

            case RecordState.Finished:
                StartRecording(); // 다시 녹음
                break;
        }
    }

    // --- 녹음 시작 ---
    public void StartRecording()
    {
        if (!isMicOpen || micClip == null)
        {
            Debug.LogError("Microphone not open! Call OpenMicrophone() first.");
            return;
        }

        isRecording = true;
        silenceTimer = 0f;
        maxTimer = 0f;

        recordStartPos = Microphone.GetPosition(micDevice);
        currentState = RecordState.Recording;
        img_record.sprite = spt_stop;

        Debug.Log($"Recording started at {recordStartPos}");
        StartCoroutine(CheckSilenceAndStop());
    }

    // --- 무음 감지 ---
    private IEnumerator CheckSilenceAndStop()
    {
        float[] samples = new float[SAMPLE_SIZE];

        while (isRecording)
        {
            int position = Microphone.GetPosition(micDevice);
            if (position < SAMPLE_SIZE)
            {
                yield return null;
                continue;
            }

            micClip.GetData(samples, position - SAMPLE_SIZE);
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            float rms = Mathf.Sqrt(sum / samples.Length);
            currentVolume = Mathf.Clamp01(rms * 10f);

            bool hasSound = rms > SILENCE_THRESHOLD;
            silenceTimer = hasSound ? 0f : silenceTimer + 0.1f;
            maxTimer += 0.1f;

            if (silenceTimer >= SILENCE_DURATION || maxTimer >= MAX_RECORD_TIME)
            {
                Debug.Log("Auto stop: silence or max time reached");
                StopRecording();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    // --- 녹음 종료 ---
    public void StopRecording()
    {
        if (!isRecording)
            return;

        isRecording = false;
        recordEndPos = Microphone.GetPosition(micDevice);

        // 루프 버퍼 회전 보정
        if (recordEndPos < recordStartPos)
            recordEndPos += micClip.samples;

        int length = recordEndPos - recordStartPos;
        float[] samples = new float[length * micClip.channels];

        int startPosMod = recordStartPos % micClip.samples;
        micClip.GetData(samples, startPosMod);

        AudioClip clip = AudioClip.Create("RecordedClip", length, micClip.channels, micClip.frequency, false);
        clip.SetData(samples, 0);

        // 무음 제거 후 저장
        AudioClip trimmedClip = TrimSilence(clip, SILENCE_THRESHOLD);
        byte[] wavData = AudioClipToWav(trimmedClip);
        File.WriteAllBytes(filepath, wavData);

        Debug.Log("Saved from open mic buffer: " + filepath);

        currentState = RecordState.Finished;
        img_record.sprite = spt_re_record;
        onTimelineHandler?.Invoke("Re-Record");
    }

    // --- 페이지 전환 시 강제 중단 ---
    public void ForceStopWithoutSave()
    {
        if (isRecording)
        {
            Debug.LogWarning("[VoiceRecord] Recording interrupted by page change.");
            isRecording = false;
            currentState = RecordState.Idle;
            img_record.sprite = spt_record;
        }
    }

    // --- 무음 제거 ---
    private AudioClip TrimSilence(AudioClip clip, float threshold)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int start = 0;
        int end = samples.Length - 1;

        while (start < samples.Length && Mathf.Abs(samples[start]) < threshold)
            start++;
        while (end > start && Mathf.Abs(samples[end]) < threshold)
            end--;

        int length = end - start + 1;
        float[] trimmedSamples = new float[length];
        Array.Copy(samples, start, trimmedSamples, 0, length);

        AudioClip trimmedClip = AudioClip.Create("Trimmed", length / clip.channels, clip.channels, clip.frequency, false);
        trimmedClip.SetData(trimmedSamples, 0);
        return trimmedClip;
    }

    // --- WAV 변환 ---
    private byte[] AudioClipToWav(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int headerSize = 44;
            int fileSize = clip.samples * clip.channels * 2 + headerSize;
            WriteWavHeader(stream, clip, fileSize);

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            foreach (var sample in samples)
            {
                short intSample = (short)Mathf.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
                stream.Write(BitConverter.GetBytes(intSample), 0, 2);
            }

            return stream.ToArray();
        }
    }

    private void WriteWavHeader(Stream stream, AudioClip clip, int fileSize)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(fileSize - 8), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);
        int byteRate = hz * channels * 2;
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        short blockAlign = (short)(channels * 2);
        stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);
        stream.Write(BitConverter.GetBytes((short)16), 0, 2);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);
    }
}
