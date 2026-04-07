using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    [Header("Panning Settings")]
    public float panSpeed = 1.2f;
    private Vector3 lastMousePosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Zabezpieczenie: jeśli zapomnisz ustawić w Inspektorze, 
        // ustawiamy domyślne wartości, żeby nie było 0.
        if (maxZoom <= 0) maxZoom = 20f;
        if (minZoom <= 0) minZoom = 2f;
    }

    // LateUpdate jest lepsze dla kamer, zapobiega drganiom
    void LateUpdate()
    {
        HandleZoom();
        HandlePan();
    }

    void HandleZoom()
    {
        // Nowy sposób odczytu scrolla w Unity 6
        float scrollDelta = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            // Obliczamy nowy rozmiar
            float newSize = cam.orthographicSize - (scrollDelta * zoomSpeed);
            
            // Ograniczamy zoom
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
            
            Debug.Log($"[Camera] Zooming: {cam.orthographicSize}");
        }
    }

    void HandlePan()
    {
        // PPM (Prawy Przycisk Myszy) do przesuwania
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            
            // Prędkość przesuwania zależna od poziomu zoomu 
            // (im dalej jesteś, tym szybciej przesuwasz)
            float screenFactor = cam.orthographicSize / 10f;
            
            Vector3 move = new Vector3(
                -delta.x * panSpeed * 0.01f * screenFactor, 
                -delta.y * panSpeed * 0.01f * screenFactor, 
                0
            );

            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }
}