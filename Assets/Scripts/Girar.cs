using UnityEngine;

// Gira y hace flotar un objeto (power-ups y decoración).
public class Girar : MonoBehaviour
{
    public float velocidad = 90f;
    public float amplitud = 0.2f;
    public float frecuencia = 2f;
    public bool tiempoSinEscala;

    private Vector3 inicio;
    private float desfase;

    void Start()
    {
        inicio = transform.localPosition;
        desfase = Random.value * 10f;
    }

    void Update()
    {
        float dt = tiempoSinEscala ? Time.unscaledDeltaTime : Time.deltaTime;
        float t = (tiempoSinEscala ? Time.unscaledTime : Time.time) + desfase;
        transform.Rotate(0f, velocidad * dt, 0f, Space.World);
        transform.localPosition = inicio + Vector3.up * Mathf.Sin(t * frecuencia) * amplitud;
    }
}
