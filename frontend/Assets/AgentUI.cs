using UnityEngine;
using TMPro;
using System.Collections;

public class AgentUI : MonoBehaviour
{
    public TextMeshProUGUI speechText;
    public TextMeshProUGUI statusText; // New TextMeshPro for Mood/PAD
    public float displayDuration = 5f;

    public void UpdateStatus(float p, float a, float d)
    {
        if (statusText == null) return;
        
        string mood = "NEUTRAL";
        if (p > 0.3f) mood = "HAPPY";
        if (p < -0.3f) mood = "UPSET";
        if (p < -0.6f) mood = "ANGRY";

        statusText.text = $"[{mood}] P:{p:F1} A:{a:F1}";
        statusText.color = p >= 0 ? Color.green : Color.red;
    }

    public void ShowSpeech(string text)
    {
        if (speechText != null)
        {
            StopAllCoroutines();
            StartCoroutine(DisplayRoutine(text));
        }
    }

    private IEnumerator DisplayRoutine(string text)
    {
        speechText.text = text;
        yield return new WaitForSeconds(displayDuration);
        speechText.text = "";
    }
}