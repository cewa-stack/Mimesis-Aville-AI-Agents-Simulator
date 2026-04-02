using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class AgentWander : MonoBehaviour
{
    public float moveSpeed = 2f;
    public Tilemap navigationMap;
    
    [Header("Safety Filter")]
    public float maxDistanceAllowed = 15f; // Maksymalny dystans od środka (0,0)

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isMoving = false;
    private List<Vector3Int> walkableTiles = new List<Vector3Int>();
    private Vector3 currentTarget;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeNavigation();
        StartCoroutine(WanderRoutine());
    }

    void InitializeNavigation()
    {
        if (navigationMap != null)
        {
            walkableTiles.Clear();
            navigationMap.CompressBounds();
            BoundsInt bounds = navigationMap.cellBounds;

            foreach (var pos in bounds.allPositionsWithin)
            {
                if (navigationMap.HasTile(pos))
                {
                    // FILTR: Dodaj tylko kafelki, które są blisko środka wyspy
                    // Zapobiega to ucieczkom do "duchów" na krawędziach
                    Vector3 worldPos = navigationMap.GetCellCenterWorld(pos);
                    if (Vector3.Distance(worldPos, Vector3.zero) < maxDistanceAllowed)
                    {
                        walkableTiles.Add(pos);
                    }
                }
            }
            Debug.Log($"<color=orange>[Wander] {gameObject.name} wczytał {walkableTiles.Count} kafelków wyspy.</color>");
        }
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            if (!isMoving && walkableTiles.Count > 0)
            {
                yield return new WaitForSeconds(Random.Range(3f, 7f));
                
                Vector3Int targetTile = walkableTiles[Random.Range(0, walkableTiles.Count)];
                currentTarget = navigationMap.GetCellCenterWorld(targetTile);
                
                yield return StartCoroutine(MoveSequentially(currentTarget));
            }
            yield return null;
        }
    }

    IEnumerator MoveSequentially(Vector3 target)
    {
        isMoving = true;
        // 1. Ruch X
        Vector3 xTarget = new Vector3(target.x, transform.position.y, transform.position.z);
        if (Vector3.Distance(transform.position, xTarget) > 0.1f)
            yield return StartCoroutine(Step(xTarget, true));

        // 2. Ruch Y
        Vector3 yTarget = new Vector3(transform.position.x, target.y, transform.position.z);
        if (Vector3.Distance(transform.position, yTarget) > 0.1f)
            yield return StartCoroutine(Step(yTarget, false));

        SetAnim(0, 0); 
        isMoving = false;
    }

    IEnumerator Step(Vector3 destination, bool isHorizontal)
    {
        while (Vector3.Distance(transform.position, destination) > 0.05f)
        {
            Vector3 dir = (destination - transform.position).normalized;
            if (isHorizontal) {
                SetAnim(dir.x > 0 ? 1 : -1, 0);
                spriteRenderer.flipX = (dir.x < 0);
            } else {
                SetAnim(0, dir.y > 0 ? 1 : -1);
            }
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
    }

    void SetAnim(float x, float y) {
        if (animator) { animator.SetFloat("MoveX", x); animator.SetFloat("MoveY", y); }
    }

    public void StopAllMovement() {
        StopAllCoroutines();
        SetAnim(0, 0);
        isMoving = false;
        StartCoroutine(WanderRoutine());
    }

    // Narysuje zielone kółko w edytorze pokazujące bezpieczną strefę
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, maxDistanceAllowed);
    }
}