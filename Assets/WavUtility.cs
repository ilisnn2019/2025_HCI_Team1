using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleCount = clip.samples * clip.channels;
        int headerSize = 44;
        int fileSize = sampleCount * 2 + headerSize;

        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(fileSize - 8);
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });

        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((ushort)(clip.channels * 2));
        writer.Write((ushort)16);

        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(sampleCount * 2);

        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        foreach (float sample in samples)
        {
            writer.Write((short)(sample * 32767));
        }

        return stream.ToArray();
    }
}
