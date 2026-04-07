using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIBrainClient : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8000";

    public delegate void OnDecisionReceived(AgentResponse response);
    public delegate void OnMemoriesReceived(List<MemoryEntry> memories);

    public void SendObservation(EnvironmentObservation observation, OnDecisionReceived callback)
    {
        StartCoroutine(PostObservation(observation, callback));
    }

    private IEnumerator PostObservation(EnvironmentObservation obs, OnDecisionReceived callback)
    {
        string json = JsonUtility.ToJson(obs);
        UnityWebRequest req = new UnityWebRequest($"{baseUrl}/agent/decide", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 60;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            callback?.Invoke(JsonUtility.FromJson<AgentResponse>(req.downloadHandler.text));
        else
            callback?.Invoke(new AgentResponse { action = "IDLE", internal_thought = "Offline" });
    }

    public void GetMemories(string agentName, OnMemoriesReceived callback)
    {
        StartCoroutine(FetchMemories(agentName, callback));
    }

    private IEnumerator FetchMemories(string name, OnMemoriesReceived callback)
    {
        UnityWebRequest req = UnityWebRequest.Get($"{baseUrl}/agent/{name}/memories");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            callback?.Invoke(JsonUtility.FromJson<MemoryListResponse>(req.downloadHandler.text).memories);
    }

    public void SendReflection(string agentName, List<float> currentEmotion)
    {
        StartCoroutine(PostReflection(agentName, currentEmotion));
    }

    private IEnumerator PostReflection(string agentName, List<float> emotion)
    {
        string emotionJson = $"[{emotion[0].ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                             $"{emotion[1].ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                             $"{emotion[2].ToString(System.Globalization.CultureInfo.InvariantCulture)}]";
        string json = $"{{\"agent_name\":\"{agentName}\", \"current_emotion\":{emotionJson}}}";
        UnityWebRequest req = new UnityWebRequest($"{baseUrl}/agent/reflect", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }
}