using System;
using System.IO;
using UnityEngine;

public static class AudioUtility
{
    private const float SILENCE_THRESHOLD = 0.02f; // 음성 인식 임계값, 필요한 만큼 조정하세요.

    // 무음 제거
    public static AudioClip TrimSilence(AudioClip audioClip, float silenceThreshold)
    {
        int sampleCount = audioClip.samples * audioClip.channels;
        float[] audioData = new float[sampleCount];
        audioClip.GetData(audioData, 0);

        // 무음 제거를 위한 샘플 인덱스 계산
        int startSample = 0;
        int endSample = sampleCount;

        // 시작 지점 찾기
        for (int i = 0; i < sampleCount; i++)
        {
            if (Mathf.Abs(audioData[i]) > silenceThreshold)
            {
                startSample = i;
                break;
            }
        }

        // 끝 지점 찾기
        for (int i = sampleCount - 1; i >= startSample; i--)
        {
            if (Mathf.Abs(audioData[i]) > silenceThreshold)
            {
                endSample = i;
                break;
            }
        }

        // 새로운 AudioClip 생성 (잘라낸 부분만 포함)
        int trimmedLength = endSample - startSample;
        float[] trimmedData = new float[trimmedLength];
        Array.Copy(audioData, startSample, trimmedData, 0, trimmedLength);

        AudioClip trimmedClip = AudioClip.Create("TrimmedAudio", trimmedLength / audioClip.channels, audioClip.channels, audioClip.frequency, false);
        trimmedClip.SetData(trimmedData, 0);

        return trimmedClip;
    }

    // WAV 파일로 저장
    public static byte[] SaveWav(string fileName, AudioClip audioClip)
    {
        // 오디오 데이터를 바이트 배열로 변환
        float[] audioData = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(audioData, 0);

        // WAV 헤더 작성
        using (MemoryStream ms = new MemoryStream())
        {
            WriteWavHeader(ms, audioClip);

            // 오디오 데이터를 WAV 포맷에 맞게 변환하여 저장
            byte[] byteData = ConvertAudioDataToByteArray(audioData);
            ms.Write(byteData, 0, byteData.Length);

            return ms.ToArray();
        }
    }

    private static void WriteWavHeader(MemoryStream ms, AudioClip audioClip)
    {
        int sampleCount = audioClip.samples;
        int byteRate = audioClip.frequency * audioClip.channels * 2;
        int blockAlign = audioClip.channels * 2;

        // WAV 파일의 기본 헤더 (44바이트)
        byte[] header = new byte[44];

        // RIFF 헤더
        Array.Copy(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, header, 0, 4);
        BitConverter.GetBytes(36 + sampleCount * 2).CopyTo(header, 4); // 파일 크기
        Array.Copy(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, header, 8, 4);

        // fmt 서브 청크
        Array.Copy(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, header, 12, 4);
        BitConverter.GetBytes(16).CopyTo(header, 16); // 서브청크 크기
        BitConverter.GetBytes((short)1).CopyTo(header, 20); // PCM 형식
        BitConverter.GetBytes((short)audioClip.channels).CopyTo(header, 22);
        BitConverter.GetBytes(audioClip.frequency).CopyTo(header, 24);
        BitConverter.GetBytes(byteRate).CopyTo(header, 28);
        BitConverter.GetBytes((short)blockAlign).CopyTo(header, 32);
        BitConverter.GetBytes((short)16).CopyTo(header, 34); // 샘플 크기

        // data 서브 청크
        Array.Copy(System.Text.Encoding.UTF8.GetBytes("data"), 0, header, 36, 4);
        BitConverter.GetBytes(sampleCount * 2).CopyTo(header, 40); // 데이터 크기

        ms.Write(header, 0, 44);
    }

    private static byte[] ConvertAudioDataToByteArray(float[] audioData)
    {
        byte[] byteArray = new byte[audioData.Length * 2]; // 16비트 PCM 포맷
        for (int i = 0; i < audioData.Length; i++)
        {
            short sample = (short)(audioData[i] * short.MaxValue);
            byteArray[i * 2] = (byte)(sample & 0xFF);
            byteArray[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        return byteArray;
    }
}
