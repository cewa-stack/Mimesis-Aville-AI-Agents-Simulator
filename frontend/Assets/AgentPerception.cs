using UnityEngine;

public class AgentPerception : MonoBehaviour
{
    private AgentController controller;
    private float lastInteractionTime;
    public float cooldown = 10f; 

    void Start()
    {
        controller = GetComponent<AgentController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (controller.isSleeping) return;

        AgentController otherAgent = other.GetComponent<AgentController>();
        if (otherAgent != null && !otherAgent.isSleeping)
        {
            if (Time.time - lastInteractionTime < cooldown) return;

            Debug.Log($"{controller.agentName} spotted {otherAgent.agentName}");
            lastInteractionTime = Time.time;
            
            controller.MakeDecision($"I just encountered {otherAgent.agentName}. I should talk to them.");
        }
    }

    public void ReceiveSpeech(string speakerName, string message)
    {
        if (controller.isSleeping) return;
        if (Time.time - lastInteractionTime < 3f) return;

        lastInteractionTime = Time.time;
        controller.MakeDecision($"{speakerName} told me: '{message}'");
    }
}