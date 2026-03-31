using UnityEngine;
using System.Collections.Generic;

public class AgentController : MonoBehaviour
{
    [Header("Agent Identity")]
    public string agentName = "Dr Alan";
    public List<float> currentPAD = new List<float> { 0.0f, 0.0f, 0.0f };
    
    [Header("Stats")]
    public float stamina = 100f;
    public float staminaDrainRate = 1.0f; 
    public bool isSleeping = false;
    public bool isWorking = false;
    private bool isHeadingToBed = false;

    [Header("World References")]
    public Transform myBed; 
    private ResearchStation targetStation;
    private AIBrainClient brainClient;
    private AgentUI agentUI;
    private AgentWander wander;

    void Awake()
    {
        brainClient = gameObject.AddComponent<AIBrainClient>();
        agentUI = GetComponent<AgentUI>();
        wander = GetComponent<AgentWander>();
    }

    void Start()
    {
        Debug.Log($"<color=cyan>[{agentName}] Initialized.</color>");
        MakeDecision("I just started my shift.");
    }

    void Update()
    {
        // LOGIKA SNU
        if (isSleeping)
        {
            stamina += staminaDrainRate * 15 * Time.deltaTime; // Szybka regeneracja
            if (stamina >= 100f) WakeUp();
            return;
        }

        // LOGIKA CHODZENIA DO ŁÓŻKA
        if (isHeadingToBed)
        {
            MoveTowardsBed();
            return;
        }

        // ZUŻYCIE STAMINY
        stamina -= staminaDrainRate * Time.deltaTime;
        if (stamina <= 0) 
        {
            stamina = 0;
            StartHeadingToBed();
        }
    }

    void StartHeadingToBed()
    {
        if (isHeadingToBed || isSleeping) return;
        
        Debug.Log($"<color=orange>[{agentName}] is exhausted. Heading to bed...</color>");
        isHeadingToBed = true;
        isWorking = false;
        if (wander != null) wander.enabled = false;
        if (targetStation != null) targetStation.Release(agentName);
        
        if (agentUI != null) agentUI.ShowSpeech("I'm so tired... going to bed.");
    }

    void MoveTowardsBed()
    {
        if (myBed == null) 
        {
            Debug.LogError($"[{agentName}] No Bed assigned in Inspector!");
            EnterSleepState(); // Teleport if no bed found to avoid stuck state
            return;
        }

        float step = 2.0f * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, myBed.position, step);

        if (Vector2.Distance(transform.position, myBed.position) < 0.3f)
        {
            EnterSleepState();
        }
    }

    void EnterSleepState()
    {
        isHeadingToBed = false;
        isSleeping = true;
        Debug.Log($"<color=blue>[{agentName}] is now sleeping and reflecting.</color>");
        brainClient.SendReflection(agentName, currentPAD);
    }

// W AgentController.cs
    public void WakeUp()
    {
        isSleeping = false;
        isHeadingToBed = false;
        stamina = 100f;
        if (wander != null) wander.enabled = true;
    
        Debug.Log($"<color=green>[{agentName}] woke up! Current Mood: P:{currentPAD[0]}</color>");
    
        // Agent po przebudzeniu od razu ocenia swój stan
        MakeDecision("I just woke up. I'm thinking about my dreams and how I feel today.");
    }

    public void MakeDecision(string observation)
    {
        // Blokada: śpiący lub idący do łóżka agent nie podejmuje nowych decyzji społecznych
        if (isSleeping || isHeadingToBed) return;

        EnvironmentObservation data = new EnvironmentObservation
        {
            agent_name = agentName,
            current_emotion = currentPAD,
            observation = observation,
            stamina = stamina 
        };

        brainClient.SendObservation(data, ProcessResponse);
    }

    void ProcessResponse(AgentResponse response)
    {
        currentPAD = response.emotion_pad;
        
        // DEBUG LOG: Zobaczysz w konsoli co AI kazało mu powiedzieć
        Debug.Log($"<color=white>[{agentName}] Thought: {response.internal_thought}</color>");
        Debug.Log($"<color=yellow>[{agentName}] Dialogue: {response.dialogue}</color>");

        if (agentUI != null)
        {
            agentUI.UpdateStatus(currentPAD[0], currentPAD[1], currentPAD[2]);
            if (!string.IsNullOrEmpty(response.dialogue))
            {
                agentUI.ShowSpeech(response.dialogue);
                BroadcastSpeech(response.dialogue);
            }
        }
        ExecuteAction(response.action);
    }

    void ExecuteAction(string action)
    {
        if (isSleeping || isHeadingToBed) return;

        if (action == "WORK" && stamina > 10f)
        {
            if (targetStation != null && targetStation.TryOccupy(agentName))
            {
                isWorking = true;
                if (wander != null) wander.enabled = false;
            }
        }
        else
        {
            if (isWorking) StopWorking();
            // Movement is handled by AgentWander.cs
        }
    }

    void StopWorking()
    {
        isWorking = false;
        if (targetStation != null) targetStation.Release(agentName);
        if (wander != null) wander.enabled = true;
    }

    public void StartReflection()
    {
        if (isSleeping) return;
        brainClient.SendReflection(agentName, currentPAD);
    }

    void BroadcastSpeech(string text)
    {
        // Szukamy innych agentów w promieniu 4 jednostek
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 4f);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            AgentPerception other = col.GetComponent<AgentPerception>();
            if (other != null)
            {
                other.ReceiveSpeech(agentName, text);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSleeping || isHeadingToBed) return;

        // Wykrywanie stacji badawczej
        ResearchStation station = other.GetComponent<ResearchStation>();
        if (station != null)
        {
            targetStation = station;
            MakeDecision("I am at the Research Station. I should work.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<ResearchStation>() != null)
        {
            StopWorking();
            targetStation = null;
        }
    }
}