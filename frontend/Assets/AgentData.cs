using System;
using System.Collections.Generic;

[Serializable]
public class EnvironmentObservation
{
    public string agent_name;
    public List<float> current_emotion;
    public string observation;
    public float stamina;
}

[Serializable]
public class AgentResponse
{
    public string internal_thought;
    public List<float> emotion_pad;
    public string dialogue;
    public string action;
    public List<float> target_location;
}

[Serializable]
public class MemoryEntry
{
    public string text;
    public int distortion;
}

[Serializable]
public class MemoryListResponse
{
    public List<MemoryEntry> memories;
}