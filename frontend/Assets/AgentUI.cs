using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AgentUI : MonoBehaviour
{
    public TextMeshProUGUI speechText;
    public TextMeshProUGUI statusText;
    public GameObject bubbleObject; // Przeciągnij tutaj "BubbleBackground"
    public float displayDuration = 5f;

    void Start()
    {
        if (bubbleObject != null) bubbleObject.SetActive(false);
    }

    public void UpdateStatus(float p, float a, float d)
    {
        if (statusText == null) return;
        
        string mood = "NEUTRAL";
        if (p > 0.3f) mood = "HAPPY";
        if (p < -0.3f) mood = "UPSET";
        if (p < -0.6f) mood = "ANGRY";

        statusText.text = $"[{mood}]";
        statusText.color = p >= 0 ? Color.green : Color.red;
    }

    public void ShowSpeech(string text)
    {
        if (speechText == null || bubbleObject == null) return;
        
        StopAllCoroutines();
        StartCoroutine(DisplayRoutine(text));
    }

    private IEnumerator DisplayRoutine(string text)
    {
        speechText.text = text;
        bubbleObject.SetActive(true); // Pokaż dymek

        yield return new WaitForSeconds(displayDuration);

        bubbleObject.SetActive(false); // Ukryj dymek
        speechText.text = "";
    }
}