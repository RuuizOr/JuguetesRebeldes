using UnityEngine;

// Hace titilar suavemente una luz (faroles y velas).
public class LuzTitilante : MonoBehaviour
{
    public float variacion = 0.15f;
    public float velocidad = 3f;

    private Light luz;
    private float baseIntensidad;
    private float semilla;

    void Start()
    {
        luz = GetComponent<Light>();
        baseIntensidad = luz.intensity;
        semilla = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(semilla, Time.time * velocidad);
        luz.intensity = baseIntensidad * (1f - variacion + n * variacion * 2f);
    }
}
