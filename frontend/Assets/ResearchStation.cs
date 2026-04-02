using UnityEngine;

public class ResearchStation : MonoBehaviour // Zostawiamy nazwę klasy, żeby nie psuć referencji, zmieniamy tylko LOGI
{
    public string currentOccupant = "None";
    public bool isOccupied = false;

    public bool TryOccupy(string agentName)
    {
        if (!isOccupied)
        {
            isOccupied = true;
            currentOccupant = agentName;
            Debug.Log($"<color=yellow>{agentName} started using the workstation.</color>");
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
            Debug.Log($"<color=yellow>{agentName} left the workstation.</color>");
        }
    }
}