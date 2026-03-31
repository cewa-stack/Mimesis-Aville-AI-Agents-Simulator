using UnityEngine;
using UnityEngine.Rendering.Universal; 

public class VisualDayNight : MonoBehaviour
{
    [Header("Settings")]
    public Light2D globalLight;
    public float dayIntensity = 1.0f;
    public float nightIntensity = 0.2f;
    public float transitionSpeed = 0.5f;

    private SimulationManager simManager;

    void Start()
    {
        simManager = GetComponent<SimulationManager>();
        
        if (globalLight == null)
        {
            // Zmieniono na FindFirstObjectByType, aby usunąć żółte ostrzeżenie
            globalLight = Object.FindFirstObjectByType<Light2D>();
        }
    }

    void Update()
    {
        if (simManager == null || globalLight == null) return;

        bool isNightTime = false;
        foreach (var agent in simManager.agents)
        {
            if (agent != null && agent.isSleeping)
            {
                isNightTime = true;
                break;
            }
        }

        float target = isNightTime ? nightIntensity : dayIntensity;
        globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, target, transitionSpeed * Time.deltaTime);
    }
}