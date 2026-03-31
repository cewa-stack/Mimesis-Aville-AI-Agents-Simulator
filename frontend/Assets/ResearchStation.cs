using UnityEngine;

public class ResearchStation : MonoBehaviour
{
    public string currentOccupant = "None";
    public bool isOccupied = false;

    public bool TryOccupy(string agentName)
    {
        if (!isOccupied)
        {
            isOccupied = true;
            currentOccupant = agentName;
            Debug.Log($"{agentName} started working at the Research Station.");
            return true;
        }
        return false;
    }

    public void Release(string agentName)
    {
        if (currentOccupant == agentName)
        {
            isOccupied = false;
            currentOccupant = "None";
            Debug.Log($"{agentName} left the Research Station.");
        }
    }
}