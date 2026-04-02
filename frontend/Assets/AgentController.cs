using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AgentController : MonoBehaviour
{
    [Header("Identity & Emotions")]
    public string agentName = "Alan";
    public List<float> currentPAD = new List<float> { 0.0f, 0.0f, 0.0f };

    [Header("Survival Stats")]
    public float stamina = 100f;
    public float staminaDrainRate = 0.5f;
    public bool isSleeping = false;
    public bool isWorking = false;
    public bool isHeadingToHouse = false;

    [Header("Visuals")]
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Canvas agentCanvas;

    [Header("World References")]
    public Transform myHouse;
    public ResearchStation currentWorkstation;

    private AIBrainClient brainClient;
    private AgentUI agentUI;
    private AgentWander wanderScript;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        agentCanvas = GetComponentInChildren<Canvas>();
        brainClient = gameObject.AddComponent<AIBrainClient>();
        agentUI = GetComponent<AgentUI>();
        wanderScript = GetComponent<AgentWander>();
    }

    void Start()
    {
        Debug.Log($"<color=cyan>[System] {agentName} joined the island.</color>");
        MakeDecision("I am starting my day on this island.");
    }

    void Update()
    {
        if (isSleeping)
        {
            SetAnim(0, 0); // Stój w miejscu podczas snu
            stamina += staminaDrainRate * 15 * Time.deltaTime;
            if (stamina >= 100f) WakeUp();
            return;
        }

        if (isHeadingToHouse)
        {
            // Kontrola ruchu do domu jest wewnątrz tej funkcji (poniżej)
            return;
        }

        stamina -= staminaDrainRate * Time.deltaTime;
        if (stamina <= 0) StartHeadingToHouse();
    }

    public void StartHeadingToHouse()
    {
        if (isHeadingToHouse || isSleeping) return;

        isHeadingToHouse = true;
        isWorking = false;
        
        if (wanderScript != null) wanderScript.StopAllCoroutines();
        if (wanderScript != null) wanderScript.enabled = false; 
        
        if (currentWorkstation != null) currentWorkstation.Release(agentName);
        if (agentUI != null) agentUI.ShowSpeech("I'm exhausted. Heading home.");

        StartCoroutine(MoveToHouseSequentially());
    }

    // Specjalna rutyna idąca do domu bez skosów (naprawia plecy!)
    IEnumerator MoveToHouseSequentially()
    {
        if (myHouse == null) { EnterSleepState(); yield break; }

        Vector3 targetPos = myHouse.position;

        // 1. Najpierw wyrównaj w poziomie (X)
        Vector3 xTarget = new Vector3(targetPos.x, transform.position.y, transform.position.z);
        while (Vector3.Distance(transform.position, xTarget) > 0.1f)
        {
            float dirX = xTarget.x > transform.position.x ? 1 : -1;
            SetAnim(dirX, 0); // Y musi być 0 podczas ruchu w bok!
            spriteRenderer.flipX = (dirX < 0);
            transform.position = Vector3.MoveTowards(transform.position, xTarget, 2.5f * Time.deltaTime);
            yield return null;
        }

        // 2. Potem wyrównaj w pionie (Y)
        Vector3 yTarget = new Vector3(transform.position.x, targetPos.y, transform.position.z);
        while (Vector3.Distance(transform.position, yTarget) > 0.1f)
        {
            float dirY = yTarget.y > transform.position.y ? 1 : -1;
            SetAnim(0, dirY); // X musi być 0 podczas ruchu góra/dół!
            transform.position = Vector3.MoveTowards(transform.position, yTarget, 2.5f * Time.deltaTime);
            yield return null;
        }

        EnterSleepState();
    }

    private void EnterSleepState()
    {
        isHeadingToHouse = false;
        isSleeping = true;
        SetAnim(0, 0);

        if (spriteRenderer) spriteRenderer.enabled = false;
        if (agentCanvas) agentCanvas.enabled = false;

        Debug.Log($"<color=blue>[{agentName}] is now sleeping.</color>");
        brainClient.SendReflection(agentName, currentPAD);
    }

    public void WakeUp()
    {
        isSleeping = false;
        isHeadingToHouse = false;
        stamina = 100f;

        if (spriteRenderer) spriteRenderer.enabled = true;
        if (agentCanvas) agentCanvas.enabled = true;

        transform.position += new Vector3(0, -0.8f, 0); // Wyjście przed dom

        if (wanderScript != null) wanderScript.enabled = true; 
        
        Debug.Log($"<color=green>[{agentName}] woke up!</color>");
        MakeDecision("I just woke up. Time to explore.");
    }

    public void MakeDecision(string observation)
    {
        if (isSleeping || isHeadingToHouse) return;

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

        if (agentUI != null)
        {
            agentUI.UpdateStatus(currentPAD[0], currentPAD[1], currentPAD[2]);
            if (!string.IsNullOrEmpty(response.dialogue))
            {
                agentUI.ShowSpeech(response.dialogue);
                BroadcastSpeech(response.dialogue);
                
                // Zatrzymaj wander na czas czytania dymka
                if (wanderScript != null) wanderScript.StopAllMovement();
                StartCoroutine(StopToTalk(5f));
            }
        }
        ExecuteAction(response.action);
    }

    IEnumerator StopToTalk(float duration)
    {
        if (wanderScript != null) wanderScript.enabled = false;
        SetAnim(0, 0);
        yield return new WaitForSeconds(duration);
        if (!isSleeping && !isHeadingToHouse && !isWorking)
        {
            if (wanderScript != null) wanderScript.enabled = true;
        }
    }

    void ExecuteAction(string action)
    {
        if (isSleeping || isHeadingToHouse) return;

        if (action == "WORK" && stamina > 15f)
        {
            if (currentWorkstation != null && currentWorkstation.TryOccupy(agentName))
            {
                isWorking = true;
                if (wanderScript != null) wanderScript.enabled = false;
                SetAnim(0, 0);
            }
        }
        else
        {
            if (isWorking) StopWorking();
        }
    }

    void StopWorking()
    {
        isWorking = false;
        if (currentWorkstation != null) currentWorkstation.Release(agentName);
        if (wanderScript != null) wanderScript.enabled = true;
    }

    void SetAnim(float x, float y)
    {
        if (animator == null) return;
        animator.SetFloat("MoveX", x);
        animator.SetFloat("MoveY", y);
    }

    void BroadcastSpeech(string text)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            AgentPerception other = col.GetComponent<AgentPerception>();
            if (other != null) other.ReceiveSpeech(agentName, text);
        }
    }

    public void StartReflection()
    {
        if (isSleeping) return;
        brainClient.SendReflection(agentName, currentPAD);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSleeping || isHeadingToHouse) return;
        ResearchStation station = other.GetComponent<ResearchStation>();
        if (station != null)
        {
            currentWorkstation = station;
            MakeDecision("I am near the island community workstation.");
        }
    }
}