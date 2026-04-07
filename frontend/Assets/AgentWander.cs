using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AgentWander : MonoBehaviour
{
    public float moveSpeed = 3f;
    public Tilemap navigationMap;
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public bool isMoving = false;
    private HashSet<Vector3Int> walkableTiles = new HashSet<Vector3Int>();

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeNavigation();
        StartCoroutine(WanderRoutine());
    }

    void InitializeNavigation()
    {
        if (navigationMap == null) return;
        walkableTiles.Clear();
        navigationMap.CompressBounds();
        foreach (var pos in navigationMap.cellBounds.allPositionsWithin)
        {
            if (navigationMap.HasTile(pos)) walkableTiles.Add(pos);
        }
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (!isMoving && walkableTiles.Count > 0)
            {
                yield return new WaitForSeconds(Random.Range(2f, 5f));

                Vector3Int currentTile = navigationMap.WorldToCell(transform.position);
                Vector3Int targetTile = currentTile;

                // LOGIKA DALEKIEJ EKSPLORACJI (szukamy celu min 10 kratek dalej)
                List<Vector3Int> allTiles = new List<Vector3Int>(walkableTiles);
                int attempts = 0;
                while (attempts < 100)
                {
                    Vector3Int candidate = allTiles[Random.Range(0, allTiles.Count)];
                    if (Vector3Int.Distance(currentTile, candidate) > 10)
                    {
                        targetTile = candidate;
                        break;
                    }
                    attempts++;
                }

                yield return StartCoroutine(SmartMove(targetTile));
            }
            yield return null;
        }
    }

    IEnumerator SmartMove(Vector3Int targetTile)
    {
        isMoving = true;
        while (true)
        {
            Vector3Int currentPos = navigationMap.WorldToCell(transform.position);
            if (currentPos == targetTile) break;

            Vector3Int nextStep = currentPos;
            if (currentPos.x != targetTile.x) nextStep.x += (targetTile.x > currentPos.x) ? 1 : -1;
            else if (currentPos.y != targetTile.y) nextStep.y += (targetTile.y > currentPos.y) ? 1 : -1;

            if (walkableTiles.Contains(nextStep))
            {
                Vector3 nextWorldPos = navigationMap.GetCellCenterWorld(nextStep);
                yield return StartCoroutine(MoveToTile(nextWorldPos));
            }
            else break; // Blokada (mur)
        }
        SetAnim(0, 0);
        isMoving = false;
    }

    IEnumerator MoveToTile(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > 0.02f)
        {
            Vector3 dir = (destination - transform.position).normalized;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) { SetAnim(dir.x > 0 ? 1 : -1, 0); spriteRenderer.flipX = (dir.x < 0); }
            else { SetAnim(0, dir.y > 0 ? 1 : -1); }
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
    }

    void SetAnim(float x, float y) { if (animator) { animator.SetFloat("MoveX", x); animator.SetFloat("MoveY", y); } }

    public void StopAllMovement()
    {
        StopAllCoroutines();
        SetAnim(0, 0);
        isMoving = false;
        StartCoroutine(WanderRoutine());
    }

    void OnDrawGizmos()
    {
        if (navigationMap == null) return;
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        foreach (var pos in walkableTiles) Gizmos.DrawCube(navigationMap.GetCellCenterWorld(pos), Vector3.one * 0.5f);
    }
}