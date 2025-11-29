using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

public static class WhisperSTT
{
    static readonly HttpClient client = new HttpClient();

    public static async Task<string> TranscribeAsync(string wavPath, string apiKey)
    {
        using (var form = new MultipartFormDataContent())
        {
            form.Add(new StringContent("whisper-1"), "model");

            byte[] fileBytes = File.ReadAllBytes(wavPath);
            form.Add(new ByteArrayContent(fileBytes), "file", Path.GetFileName(wavPath));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage res = await client.PostAsync(
                "https://api.openai.com/v1/audio/transcriptions", form);

            string json = await res.Content.ReadAsStringAsync();
            return json; // 여기에 STT text 포함됨
        }
    }

    public static string ExtractReplyText(string reply)
    {
        // 2) reply 내부 JSON 문자열을 다시 파싱할 수 있도록 언에스케이프 처리
        string replyJson = reply
            .Replace("\\\"", "\"")   // \" → "
            .Replace("\\n", "\n");   // \n → 줄바꿈

        // 맨 앞과 뒤의 따옴표 제거 필요할 수 있음
        if (replyJson.StartsWith("\"") && replyJson.EndsWith("\""))
            replyJson = replyJson.Substring(1, replyJson.Length - 2);

        // 3) reply JSON 파싱
        ReplyData replyData = JsonUtility.FromJson<ReplyData>(replyJson);
        if (replyData == null)
            return null;

        return replyData.text;
    }

    [System.Serializable]
    public class ReplyData
    {
        public string text;
    }
}
