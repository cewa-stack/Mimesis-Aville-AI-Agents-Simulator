using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class AgentUI : MonoBehaviour
{
    public TextMeshProUGUI speechText;
    public GameObject bubbleObject; // To jest Twoje tło (Image)
    public float displayDuration = 5f;

    void Awake()
    {
        // Ukryj dymek natychmiast przy starcie gry
        if (bubbleObject != null) bubbleObject.SetActive(false);
    }

    public void ShowSpeech(string text)
    {
        if (speechText == null || bubbleObject == null) return;
        
        StopAllCoroutines();
        StartCoroutine(DisplayRoutine(text));
    }

    public void HideBubble()
    {
        StopAllCoroutines();
        if (bubbleObject != null) bubbleObject.SetActive(false);
        if (speechText != null) speechText.text = "";
    }

    private IEnumerator DisplayRoutine(string text)
    {
        speechText.text = text;
        bubbleObject.SetActive(true);

        // Wymuszenie odświeżenia układu
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleObject.GetComponent<RectTransform>());

        yield return new WaitForSeconds(displayDuration);

        bubbleObject.SetActive(false);
        speechText.text = "";
    }

    // Usunięto UpdateStatus, zgodnie z Twoją prośbą (będzie w panelu agenta)
    public void UpdateStatus(float p, float a, float d) { }
}