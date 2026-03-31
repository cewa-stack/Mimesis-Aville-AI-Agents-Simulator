using UnityEngine;
using UnityEngine.InputSystem; // Upewnij się, że masz to, jeśli używasz New Input System

public class SimulationManager : MonoBehaviour
{
    public AgentController[] agents;

    void Update()
    {
        // Sprawdzamy oba systemy na wszelki wypadek
        bool rPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) rPressed = true;
#endif

        if (Input.GetKeyDown(KeyCode.R)) rPressed = true;

        if (rPressed)
        {
            Debug.Log("R key detected!");
            TriggerNightPhase();
        }
    }

    public void TriggerNightPhase()
    {
        if (agents == null || agents.Length == 0)
        {
            Debug.LogError("No agents assigned to SimulationManager!");
            return;
        }

        Debug.Log($"--- NIGHT PHASE START for {agents.Length} agents ---");
        foreach (var agent in agents)
        {
            if (agent != null)
            {
                Debug.Log($"Triggering reflection for: {agent.agentName}");
                agent.StartReflection();
            }
        }
    }
}