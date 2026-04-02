using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 10f;
    public float minZoom = 3f;   // Maksymalne zbliżenie
    public float maxZoom = 20f;  // Maksymalne oddalenie (zobaczysz całą wyspę)

    [Header("Panning Settings")]
    public float panSpeed = 1f;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Ustawienie początkowe kamery na środek wyspy
        transform.position = new Vector3(0, 0, -10f);
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float newSize = cam.orthographicSize - (scroll * zoomSpeed);
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }

    void HandlePan()
    {
        // Kliknięcie prawym przyciskiem myszy (1) zapisuje punkt startowy
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // Trzymanie prawego przycisku myszy przesuwa kamerę
        if (Input.GetMouseButton(1))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            
            // Przesuwamy kamerę o różnicę pozycji
            transform.position += difference;
        }
    }
}