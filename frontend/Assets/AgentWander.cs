using UnityEngine;
using System.Collections;

public class AgentWander : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 1.0f;
    public float idleTime = 3.0f;

    [Header("Boundaries (Laboratory Walls)")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minY = -4f;
    public float maxY = 4f;

    private Vector2 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        SetNewTarget();
    }

    void Update()
    {
        if (isMoving)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Jeśli dotarliśmy do celu, czekamy i wybieramy nowy
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
            {
                StartCoroutine(WaitAndMove());
            }
        }
    }

    void SetNewTarget()
    {
        // Losujemy punkt Wewnątrz zdefiniowanych granic
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);
        
        targetPosition = new Vector2(randomX, randomY);
        isMoving = true;
    }

    IEnumerator WaitAndMove()
    {
        isMoving = false;
        yield return new WaitForSeconds(Random.Range(idleTime * 0.5f, idleTime * 1.5f));
        SetNewTarget();
    }

    public void StopMoving(float duration)
    {
        StopAllCoroutines();
        isMoving = false;
        StartCoroutine(ResumeAfterDelay(duration));
    }

    IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetNewTarget();
    }

    // Rysuje ramkę zasięgu w oknie Scene (nie będzie jej widać w grze)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 topLeft = new Vector3(minX, maxY, 0);
        Vector3 topRight = new Vector3(maxX, maxY, 0);
        Vector3 bottomLeft = new Vector3(minX, minY, 0);
        Vector3 bottomRight = new Vector3(maxX, minY, 0);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}