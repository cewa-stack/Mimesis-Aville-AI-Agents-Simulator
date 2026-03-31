using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIBrainClient : MonoBehaviour
{
    private string baseUrl = "http://localhost:8000";

    public delegate void OnDecisionReceived(AgentResponse response);

    public void SendObservation(EnvironmentObservation observation, OnDecisionReceived callback)
    {
        StartCoroutine(PostObservation(observation, callback));
    }

    private IEnumerator PostObservation(EnvironmentObservation observation, OnDecisionReceived callback)
    {
        string json = JsonUtility.ToJson(observation);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/agent/decide", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        // Linia chunkedTransfer została usunięta, aby pozbyć się ostrzeżeń

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AgentResponse response = JsonUtility.FromJson<AgentResponse>(request.downloadHandler.text);
            callback?.Invoke(response);
        }
        else
        {
            Debug.LogError($"Network Error: {request.error}");
        }
    }

    public void SendReflection(string agentName, List<float> currentEmotion)
    {
        StartCoroutine(PostReflection(agentName, currentEmotion));
    }

    private IEnumerator PostReflection(string agentName, List<float> emotion)
    {
        string emotionJson = $"[{emotion[0].ToString().Replace(',', '.')}, {emotion[1].ToString().Replace(',', '.')}, {emotion[2].ToString().Replace(',', '.')}]";
        string json = $"{{\"agent_name\":\"{agentName}\", \"current_emotion\":{emotionJson}}}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/agent/reflect", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Reflection completed for {agentName}");
        }
    }
}