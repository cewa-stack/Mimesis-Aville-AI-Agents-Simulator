using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AgentController : MonoBehaviour
{
    [Header("Identity & Emotions")]
    public string agentName = "Alan";
    public List<float> currentPAD = new List<float> { 0.0f, 0.0f, 0.0f };
    public string lastInternalThought = "Just arrived...";

    [Header("Survival Stats")]
    public float stamina = 100f;
    public float staminaDrainRate = 0.05f;
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
        // Inicjalizacja komponentów
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        agentCanvas = GetComponentInChildren<Canvas>();
        brainClient = gameObject.AddComponent<AIBrainClient>();
        agentUI = GetComponent<AgentUI>();
        wanderScript = GetComponent<AgentWander>();

        // Upewnij się, że lista emocji nie jest pusta na starcie
        if (currentPAD == null || currentPAD.Count < 3)
        {
            currentPAD = new List<float> { 0.0f, 0.0f, 0.0f };
        }
    }

    void Start()
    {
        LoadState(); // Wczytaj poprzedni stan z pamięci
        InvokeRepeating("SaveState", 5f, 5f); // Auto-zapis co 5 sekund
        
        Debug.Log($"<color=cyan>[System] {agentName} is ready.</color>");
        MakeDecision("I am continuing my life on this island.");
    }

    void OnApplicationQuit()
    {
        SaveState(); // Zapisz przy wyjściu
    }

    // --- SYSTEM ZAPISU (PERSISTENCE) ---
    public void SaveState()
    {
        if (string.IsNullOrEmpty(agentName)) return;

        PlayerPrefs.SetFloat(agentName + "_PosX", transform.position.x);
        PlayerPrefs.SetFloat(agentName + "_PosY", transform.position.y);
        PlayerPrefs.SetFloat(agentName + "_Stamina", stamina);

        if (currentPAD != null && currentPAD.Count >= 3)
        {
            PlayerPrefs.SetFloat(agentName + "_P", currentPAD[0]);
            PlayerPrefs.SetFloat(agentName + "_A", currentPAD[1]);
            PlayerPrefs.SetFloat(agentName + "_D", currentPAD[2]);
        }
        PlayerPrefs.Save();
    }

    public void LoadState()
    {
        if (PlayerPrefs.HasKey(agentName + "_PosX"))
        {
            float x = PlayerPrefs.GetFloat(agentName + "_PosX");
            float y = PlayerPrefs.GetFloat(agentName + "_PosY");
            transform.position = new Vector3(x, y, 0);
            stamina = PlayerPrefs.GetFloat(agentName + "_Stamina");
            
            currentPAD[0] = PlayerPrefs.GetFloat(agentName + "_P");
            currentPAD[1] = PlayerPrefs.GetFloat(agentName + "_A");
            currentPAD[2] = PlayerPrefs.GetFloat(agentName + "_D");
            
            Debug.Log($"<color=yellow>[Load] {agentName} restored at {transform.position}</color>");
        }
    }

    void Update()
    {
        if (isSleeping)
        {
            SetAnim(0, 0);
            stamina += staminaDrainRate * 15 * Time.deltaTime; // Szybka regeneracja
            if (stamina >= 100f) WakeUp();
            return;
        }

        if (isHeadingToHouse) return;

        // Zużycie energii
        stamina -= staminaDrainRate * Time.deltaTime;
        if (stamina <= 0) 
        {
            stamina = 0;
            StartHeadingToHouse();
        }
    }

    // --- CYKL ŻYCIA ---
    public void StartHeadingToHouse()
    {
        if (isHeadingToHouse || isSleeping) return;

        isHeadingToHouse = true;
        isWorking = false;
        
        if (wanderScript != null) 
        {
            wanderScript.StopAllCoroutines();
            wanderScript.enabled = false; 
        }
        
        if (currentWorkstation != null) currentWorkstation.Release(agentName);
        if (agentUI != null) agentUI.ShowSpeech("I'm exhausted. Heading home.");

        StartCoroutine(MoveToHouseSequentially());
    }

    IEnumerator MoveToHouseSequentially()
    {
        if (myHouse == null) { EnterSleepState(); yield break; }
        Vector3 targetPos = myHouse.position;

        // Najpierw X
        Vector3 xTarget = new Vector3(targetPos.x, transform.position.y, 0);
        while (Vector3.Distance(transform.position, xTarget) > 0.1f)
        {
            float d = xTarget.x > transform.position.x ? 1 : -1;
            SetAnim(d, 0); spriteRenderer.flipX = (d < 0);
            transform.position = Vector3.MoveTowards(transform.position, xTarget, 2.5f * Time.deltaTime);
            yield return null;
        }
        // Potem Y
        Vector3 yTarget = new Vector3(transform.position.x, targetPos.y, 0);
        while (Vector3.Distance(transform.position, yTarget) > 0.1f)
        {
            float d = yTarget.y > transform.position.y ? 1 : -1;
            SetAnim(0, d);
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
        isWorking = false;
        stamina = 100f;

        if (spriteRenderer) spriteRenderer.enabled = true;
        if (agentCanvas) agentCanvas.enabled = true;
        if (agentUI != null) agentUI.HideBubble(); // Schowaj dymki po wyjściu

        transform.position += new Vector3(0, -1.2f, 0); // Wyjdź przed dom

        if (wanderScript != null) { wanderScript.enabled = true; wanderScript.StopAllMovement(); }
        
        Debug.Log($"<color=green>[{agentName}] woke up refreshed!</color>");
        MakeDecision("I just woke up.");
    }

    // --- KOMUNIKACJA Z AI ---
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
        if (response == null) return;
        
        currentPAD = response.emotion_pad;
        lastInternalThought = response.internal_thought;

        if (agentUI != null)
        {
            if (!string.IsNullOrEmpty(response.dialogue))
            {
                agentUI.ShowSpeech(response.dialogue);
                BroadcastSpeech(response.dialogue);
                if (wanderScript != null) wanderScript.StopAllMovement();
                StartCoroutine(StopToTalk(5f));
            }
        }

        // Obsługa akcji EXPLORE z koordynatami
        if (response.action == "EXPLORE" && response.target_location != null && response.target_location.Count >= 2)
        {
            Vector3 target = new Vector3(response.target_location[0], response.target_location[1], 0);
            if (wanderScript != null) wanderScript.StopAllMovement();
            StartCoroutine(MoveSequentially(target, false));
        }
        else
        {
            ExecuteAction(response.action);
        }
    }

    IEnumerator MoveSequentially(Vector3 target, bool isHome)
    {
        Vector3 xTarget = new Vector3(target.x, transform.position.y, 0);
        while (Vector3.Distance(transform.position, xTarget) > 0.1f)
        {
            float d = xTarget.x > transform.position.x ? 1 : -1;
            SetAnim(d, 0); spriteRenderer.flipX = d < 0;
            transform.position = Vector3.MoveTowards(transform.position, xTarget, 2.5f * Time.deltaTime);
            yield return null;
        }
        Vector3 yTarget = new Vector3(transform.position.x, target.y, 0);
        while (Vector3.Distance(transform.position, yTarget) > 0.1f)
        {
            float d = yTarget.y > transform.position.y ? 1 : -1;
            SetAnim(0, d);
            transform.position = Vector3.MoveTowards(transform.position, yTarget, 2.5f * Time.deltaTime);
            yield return null;
        }
        if (isHome) EnterSleepState();
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
        if (action == "WORK" && stamina > 15f)
        {
            if (currentWorkstation != null && currentWorkstation.TryOccupy(agentName))
            {
                isWorking = true;
                if (wanderScript != null) wanderScript.enabled = false;
                SetAnim(0, 0);
            }
        }
        else if (isWorking) StopWorking();
    }

    void StopWorking()
    {
        isWorking = false;
        if (currentWorkstation != null) currentWorkstation.Release(agentName);
        if (wanderScript != null) wanderScript.enabled = true;
    }

    void SetAnim(float x, float y)
    {
        if (animator != null)
        {
            animator.SetFloat("MoveX", x);
            animator.SetFloat("MoveY", y);
        }
    }

    void BroadcastSpeech(string text)
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 6f);
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
            MakeDecision("I am near the workstation.");
        }
    }
}