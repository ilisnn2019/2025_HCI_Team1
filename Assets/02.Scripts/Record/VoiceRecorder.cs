using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

public class VoiceRecorder : MonoBehaviour
{
    [Header("UI Components")]
    public Image img_record;
    public Sprite spt_record;
    public Sprite spt_stop;
    public Sprite spt_re_record;

    public Image img_record_rec;
    public Sprite spt_record_rec_1;
    public Sprite spt_record_rec_2;

    private enum RecordState { Idle, Recording, Finished }
    private RecordState currentState = RecordState.Idle;

    private const int MAX_RECORD_TIME = 30;
    private const float SILENCE_THRESHOLD = 0.01f;
    private const float SILENCE_DURATION = 4f;
    private const int SAMPLE_SIZE = 256;

    private string filedir;
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

    // 재녹음 시 덮어쓰기 위해 최근 저장 파일 경로 기억
    private string lastRecordedFilePath = null;

    // --- 실시간 볼륨 감지 ---
    private float voiceDetectTimer = 0f;
    private float exceptionCooldownTimer = 0f;
    private const float DETECT_DURATION = 2f;
    private const float EXCEPTION_COOLDOWN = 2f;

    private void Awake()
    {
        filedir = Application.persistentDataPath;
        if (!Directory.Exists(filedir)) Directory.CreateDirectory(filedir);
    }

    private void RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            Debug.Log("Requested microphone permission. Please allow it in the popup.");
        }
#endif

        // 권한이 이미 있거나, 팝업 호출 후 마이크 오픈 시점에서 다시 체크
        InitializeRecorder();
    }
    public void RequestMic()
    {
        RequestMicPermission();
    }

    public void InitializeRecorder()
    {
        if (isMicOpen)
            CloseMicrophone();

        isRecording = false;
        micDetected = false;
        exception1Enabled = true;

        silenceTimer = 0f;
        maxTimer = 0f;
        voiceDetectTimer = 0f;
        exceptionCooldownTimer = 0f;

        recordStartPos = 0;
        recordEndPos = 0;

        currentVolume = 0f;

        lastRecordedFilePath = null;

        currentState = RecordState.Idle;
        img_record.sprite = spt_record;
        img_record_rec.enabled = false;
    }

    private string GenerateFileName()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        return $"voice_record_{timestamp}.wav";
    }

    public void OpenMicrophone()
    {
        if (isMicOpen) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        micDevice = Microphone.devices[0];

        micClip = Microphone.Start(micDevice, true, 30, 44100);

        StartCoroutine(WaitMicStart());
    }

    private IEnumerator WaitMicStart()
    {
        float timeout = 2f;

        while (Microphone.GetPosition(micDevice) <= 0 && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (timeout <= 0)
        {
            Debug.LogError("Microphone failed to start!");
            yield break;
        }

        isMicOpen = true;
        micMonitorCoroutine = StartCoroutine(MonitorMicLevel());
        Debug.Log("Microphone opened successfully");
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

        Debug.Log("Microphone closed.");
    }

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

            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];

            float rms = Mathf.Sqrt(sum / samples.Length);
            currentVolume = Mathf.Clamp01(rms * 10f);
            micDetected = rms > SILENCE_THRESHOLD;

            if (!isRecording && exception1Enabled)
            {
                if (micDetected)
                {
                    voiceDetectTimer += 0.4f;
                    if (voiceDetectTimer >= DETECT_DURATION)
                    {
                        Debug.Log("Exception1 Triggered");
                        onTimelineHandler?.Invoke("Exception1");

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
                StartRecording(); // 재녹음
                break;
        }
    }

    public void StartRecording()
    {
        if (!isMicOpen || micClip == null || Microphone.GetPosition(micDevice) <= 0)
        {
            Debug.LogWarning("Microphone not ready.");
            return;
        }

        isRecording = true;
        silenceTimer = 0f;
        maxTimer = 0f;

        recordStartPos = Microphone.GetPosition(micDevice);
        currentState = RecordState.Recording;

        img_record.sprite = spt_stop;
        img_record_rec.enabled = true;

        StartCoroutine(CheckSilenceAndStop());
    }

    private IEnumerator CheckSilenceAndStop()
    {
        float[] samples = new float[SAMPLE_SIZE];
        int stack = 0;
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
                Debug.Log("Auto stop by silence/max");
                StopRecording();
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
            if (++stack > 10)
            {
                if ((stack / 10) % 2 == 1) img_record_rec.sprite = spt_record_rec_2;
                else img_record_rec.sprite = spt_record_rec_1;
            }
        }
    }

    /*
    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;

        img_record_rec.enabled = false;

        recordEndPos = Microphone.GetPosition(micDevice);
        if (recordEndPos < recordStartPos)
            recordEndPos += micClip.samples;

        int length = recordEndPos - recordStartPos;
        float[] samples = new float[length * micClip.channels];
        int startPosMod = recordStartPos % micClip.samples;
        micClip.GetData(samples, startPosMod);

        AudioClip clip = AudioClip.Create("RecordedClip", length, micClip.channels, micClip.frequency, false);
        clip.SetData(samples, 0);

        AudioClip trimmedClip = TrimSilence(clip, SILENCE_THRESHOLD);
        byte[] wavData = AudioClipToWav(trimmedClip);

        if (lastRecordedFilePath == null)
        {
            string filename = GenerateFileName();
            filepath = Path.Combine(filedir, filename);
            SaveMetadata(filepath, "변비 탐정 실룩\n사라진 고등어 인형", "너는 꿈이 있니?\n나중에 어른이 되면 뭐 하고 싶어?");
        }
        else
        {
            filepath = lastRecordedFilePath;
        }

        File.WriteAllBytes(filepath, wavData);
        lastRecordedFilePath = filepath;

        Debug.Log("Saved to: " + filepath);

        currentState = RecordState.Finished;
        img_record.sprite = spt_re_record;

        onTimelineHandler?.Invoke("Re-Record");
    }
    */

    public async void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;

        img_record_rec.enabled = false;

        // 1. Raw 데이터 추출
        recordEndPos = Microphone.GetPosition(micDevice);
        if (recordEndPos < recordStartPos)
            recordEndPos += micClip.samples;

        int length = recordEndPos - recordStartPos;
        float[] samples = new float[length * micClip.channels];
        int startPosMod = recordStartPos % micClip.samples;
        micClip.GetData(samples, startPosMod);

        AudioClip clip = AudioClip.Create(
            "RecordedClip",
            length,
            micClip.channels,
            micClip.frequency,
            false
        );
        clip.SetData(samples, 0);

        // 2. 무음 Trim
        AudioClip trimmedClip = TrimSilence(clip, SILENCE_THRESHOLD);

        // 3. WAV 변환
        byte[] wavData = AudioClipToWav(trimmedClip);

        // 4. 파일 경로 생성/유지
        if (lastRecordedFilePath == null)
        {
            string filename = GenerateFileName();   // ex: 20250101_140022.wav
            filepath = Path.Combine(filedir, filename);
        }
        else
        {
            filepath = lastRecordedFilePath;
        }

        // 5. WAV 저장 (동기파일 저장)
        File.WriteAllBytes(filepath, wavData);
        Debug.Log("Saved WAV to: " + filepath);

        lastRecordedFilePath = filepath;

        // 6. Whisper STT 호출
        Debug.Log("Sending to Whisper: " + filepath);

        string whisperJson;

        try
        {
            whisperJson = await WhisperSTT.TranscribeAsync(filepath, "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");
            Debug.Log("Whisper STT Success: " + whisperJson);
        }
        catch (Exception e)
        {
            Debug.LogError("Whisper Error: " + e);
            whisperJson = "{\"error\":\"Whisper failed\"}";
        }

        // -------------------------
        // 7. Meta JSON 저장
        // -------------------------
        await SaveMetaAsync(filepath, "변비 탐정 실룩\n사라진 고등어 인형","너는 꿈이 있니?\n나중에 어른이 되면 뭐 하고 싶어?", whisperJson);
        Debug.Log("Saved STT Meta JSON");

        // -------------------------
        // 8. UI 및 상태 업데이트
        // -------------------------
        currentState = RecordState.Finished;
        img_record.sprite = spt_re_record;
        onTimelineHandler?.Invoke("Re-Record");
    }


    public void ForceStopWithoutSave()
    {
        if (isRecording)
        {
            Debug.LogWarning("Recording interrupted.");
            isRecording = false;

            currentState = RecordState.Idle;
            img_record.sprite = spt_record;
        }
    }

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

        AudioClip trimmedClip = AudioClip.Create(
            "Trimmed",
            length / clip.channels,
            clip.channels,
            clip.frequency,
            false
        );

        trimmedClip.SetData(trimmedSamples, 0);

        return trimmedClip;
    }

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

    async Task SaveMetaAsync(string wavPath, string title, string content, string reply)
    {
        VoiceMetadata data = new VoiceMetadata()
        {
            title = title,
            content = content,
            reply = reply
        };

        string json = JsonUtility.ToJson(data, true);
        string metaPath = Path.ChangeExtension(wavPath, ".json");
        await File.WriteAllTextAsync(metaPath, json);
    }

    private void SaveMetadata(string filepath, string title, string content, string reply)
    {
        VoiceMetadata data = new VoiceMetadata()
        {
            title = title,
            content = content,
            reply = reply
        };

        string json = JsonUtility.ToJson(data, true);

        string jsonPath = Path.ChangeExtension(filepath, ".json");
        File.WriteAllText(jsonPath, json);
    }
}
