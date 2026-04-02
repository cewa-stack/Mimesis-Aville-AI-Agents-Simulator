using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIBrainClient : MonoBehaviour
{
    // Używamy 127.0.0.1 zamiast localhost dla lepszej stabilności na Windows
    private string baseUrl = "http://127.0.0.1:8000";

    public delegate void OnDecisionReceived(AgentResponse response);

    // --- GŁÓWNA DECYZJA AGENTA ---
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

        // KLUCZOWE POPRAWKI:
        request.timeout = 60; // Dajemy lokalnemu AI do 60 sekund na odpowiedź
        
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                AgentResponse response = JsonUtility.FromJson<AgentResponse>(request.downloadHandler.text);
                callback?.Invoke(response);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BrainClient] Błąd parsowania JSON: {e.Message}");
            }
        }
        else
        {
            // Szczegółowy log błędu sieciowego
            Debug.LogError($"[BrainClient] Network Error: {request.error} | URL: {request.url} | Code: {request.responseCode}");
        }
    }

    // --- CYKL REFLEKSJI (SEN) ---
    public void SendReflection(string agentName, List<float> currentEmotion)
    {
        StartCoroutine(PostReflection(agentName, currentEmotion));
    }

    private IEnumerator PostReflection(string agentName, List<float> emotion)
    {
        // Ręczne budowanie JSONa dla pewności formatowania liczb float (kropka zamiast przecinka)
        string emotionJson = $"[{emotion[0].ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                             $"{emotion[1].ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
                             $"{emotion[2].ToString(System.Globalization.CultureInfo.InvariantCulture)}]";
        
        string json = $"{{\"agent_name\":\"{agentName}\", \"current_emotion\":{emotionJson}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/agent/reflect", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        
        request.timeout = 90; // Refleksja (sen) trwa zazwyczaj dłużej, dajemy 90 sekund

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"<color=blue>[BrainClient] Reflection for {agentName} completed successfully.</color>");
        }
        else
        {
            Debug.LogError($"[BrainClient] Reflection Error: {request.error} | Code: {request.responseCode}");
        }
    }
}