using UnityEngine;
using UnityEngine.Rendering.Universal; // To jest kluczowe dla Light2D

public class FireFlicker : MonoBehaviour
{
    private Light2D fireLight; // Zmienione na prywatne, bo znajdziemy je automatycznie
    
    [Header("Settings")]
    public float minIntensity = 1.0f;
    public float maxIntensity = 1.8f;
    public float flickerSpeed = 2f;

    void Start()
    {
        // Szukamy komponentu Light2D na tym obiekcie LUB w jego dzieciach
        fireLight = GetComponentInChildren<Light2D>();

        if (fireLight == null)
        {
            Debug.LogError($"[FireFlicker] Nie znaleziono komponentu Light2D na obiekcie {gameObject.name} ani w jego dzieciach! Sprawdź czy ognisko ma podpięte światło 2D.");
        }
    }

    void Update()
    {
        if (fireLight == null) return;

        // Płynne, naturalne migotanie używając szumu Perline'a
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
        fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}