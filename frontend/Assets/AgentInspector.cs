using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class AgentInspector : MonoBehaviour
{
    [Header("UI Panel Refs")] public GameObject panel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI thoughtText;
    public TextMeshProUGUI memoriesText;
    public Slider staminaSlider, pSlider, aSlider, dSlider;

    private AgentController selectedAgent;
    private AIBrainClient brainClient;
    private float updateTimer = 0f;

    void Start()
    {
        if (panel != null) panel.SetActive(false);
        brainClient = GetComponent<AIBrainClient>();
        if (brainClient == null) brainClient = gameObject.AddComponent<AIBrainClient>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                AgentController agent = hit.collider.GetComponent<AgentController>();
                if (agent != null) SelectAgent(agent);
            }
            else if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (panel != null) panel.SetActive(false);
                selectedAgent = null;
            }
        }

        if (selectedAgent != null)
        {
            UpdateLiveStats();
            updateTimer += Time.deltaTime;
            if (updateTimer >= 3f)
            {
                FetchMemories();
                updateTimer = 0;
            }
        }
    }

    void SelectAgent(AgentController agent)
    {
        selectedAgent = agent;
        if (panel != null) panel.SetActive(true);
        if (nameText != null) nameText.text = agent.agentName.ToUpper();
        FetchMemories();
    }

    void UpdateLiveStats()
    {
        if (selectedAgent == null) return;

        // Bezpieczne przypisywanie wartości (Null Checks)
        if (staminaSlider != null) staminaSlider.value = selectedAgent.stamina;
        if (pSlider != null) pSlider.value = selectedAgent.currentPAD[0];
        if (aSlider != null) aSlider.value = selectedAgent.currentPAD[1];
        if (dSlider != null) dSlider.value = selectedAgent.currentPAD[2];
        if (thoughtText != null) thoughtText.text = selectedAgent.lastInternalThought;
    }

    void FetchMemories()
    {
        brainClient.GetMemories(selectedAgent.agentName, (memories) =>
        {
            if (memoriesText == null) return;
            StringBuilder sb = new StringBuilder();

            // Dodajemy nagłówek i formatujemy listę
            sb.AppendLine("<size=120%><color=#FFA500>RECENT EVENTS:</color></size>");
            sb.AppendLine("");

            foreach (var m in memories)
            {
                // Jeśli distortion > 0, zaznaczamy wspomnienie jako "zniekształcone" (czerwone/pochylone)
                string color = m.distortion > 0 ? "<color=#FF6666><i>[Vague] " : "<color=#FFFFFF>• ";
                sb.AppendLine($"{color}{m.text}</color></i>");
                sb.AppendLine(""); // Odstęp między wspomnieniami
            }

            memoriesText.text = sb.ToString();
        });
    }
}