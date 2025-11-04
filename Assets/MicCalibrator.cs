using UnityEngine;
using System.Collections;

public class MicCalibrator : MonoBehaviour
{
    public int sampleRate = 44100;
    public int calibrationDuration = 3; // seconds
    private AudioClip micClip;
    private string micDevice;

    public float NoiseLevel { get; private set; }
    public float Threshold { get; private set; }

    void Start()
    {

    }

    [ContextMenu("start calibrate")]
    public void CallCalibrateMic()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            StartCoroutine(CalibrateMicrophone());
        }
    }

    IEnumerator CalibrateMicrophone()
    {
        Debug.Log("Calibrating mic...");

        micClip = Microphone.Start(micDevice, true, calibrationDuration, sampleRate);

        float sum = 0f;
        int count = 0;

        float[] samples = new float[1024];

        float startTime = Time.time;
        while (Time.time - startTime < calibrationDuration)
        {
            int micPos = Microphone.GetPosition(micDevice) - samples.Length;
            if (micPos < 0) continue;

            micClip.GetData(samples, micPos);

            // RMS 계산
            float rms = 0f;
            for (int i = 0; i < samples.Length; i++)
                rms += samples[i] * samples[i];

            rms = Mathf.Sqrt(rms / samples.Length);

            sum += rms;
            count++;

            yield return null;
        }

        NoiseLevel = sum / count;
        Threshold = NoiseLevel * 2f; // 노이즈보다 약간 크게 설정
        Debug.Log($"Calibration done. NoiseLevel={NoiseLevel}, Threshold={Threshold}");

        Microphone.End(micDevice);
    }
}
