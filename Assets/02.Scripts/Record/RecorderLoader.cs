using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RecorderLoader : MonoBehaviour
{
    public GameObject voiceItemPrefab;
    public Transform parentTransform;
    public AudioSource audioSource;

    private string folderPath;

    private void Awake()
    {
        folderPath = Application.persistentDataPath;
    }

    private void Start()
    {
        StartCoroutine(LoadRecordedVoices());
    }

    private IEnumerator LoadRecordedVoices()
    {
        string[] wavFiles = Directory.GetFiles(folderPath, "*.wav");

        foreach (Transform child in parentTransform)
            Destroy(child.gameObject);

        foreach (string wav in wavFiles)
        {
            yield return StartCoroutine(CreateVoiceItem(wav));
        }
    }

    private IEnumerator CreateVoiceItem(string wavPath)
    {
        // 프리팹 생성
        GameObject item = Instantiate(voiceItemPrefab, parentTransform);
        RecordVoiceContent ui = item.GetComponent<RecordVoiceContent>();

        if (ui == null)
        {
            Debug.LogError("RecorderVoiceContent 없음");
            yield break;
        }

        // 날짜 파싱
        string dateString = "(날짜 없음)";
        if (TryParseVoiceFile(wavPath, out DateTime dt))
            dateString = dt.ToString("yyyy.MM.dd HH:mm");

        // JSON 메타 읽기
        string title = "";
        string content = "";
        string jsonPath = Path.ChangeExtension(wavPath, ".json");

        if (File.Exists(jsonPath))
        {
            var meta = JsonUtility.FromJson<VoiceMetadata>(File.ReadAllText(jsonPath));
            title = meta.title;
            content = meta.content;
        }

        string url = "file://" + wavPath;
        AudioClip clip = null;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("오디오 로딩 실패 : " + req.error);
                yield break;
            }

            clip = DownloadHandlerAudioClip.GetContent(req);
        }

        // UI 초기화 (모든 데이터 입력)
        ui.Init(dateString, title, content, clip, audioSource);
    }

    public static bool TryParseVoiceFile(string filePath, out DateTime dateTime)
    {
        dateTime = default;

        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string[] parts = fileName.Split('_');

        if (parts.Length < 4) return false;

        string date = parts[2];
        string time = parts[3];

        return DateTime.TryParseExact(
            date + time,
            "yyyyMMddHHmmss",
            null,
            System.Globalization.DateTimeStyles.None,
            out dateTime
        );
    }
}

[Serializable]
public class VoiceMetadata
{
    public string title;
    public string content;
}
