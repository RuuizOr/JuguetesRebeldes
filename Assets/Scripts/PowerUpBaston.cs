using UnityEngine;

// Bastón de dulce: da velocidad extra al duende durante unos segundos.
public class PowerUpBaston : MonoBehaviour
{
    public float duracion = 5f;
    public GameObject efecto;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        DuendeController d = other.GetComponent<DuendeController>();
        if (d == null || d.Muerto)
            return;

        d.ActivarBoost(duracion);
        AudioManager.SFX("powerup");
        if (efecto != null)
            Instantiate(efecto, transform.position, Quaternion.identity);
        if (NivelManager.Instancia != null)
            NivelManager.Instancia.MostrarAviso("¡Bastón de dulce! Súper velocidad", new Color(1f, 0.6f, 0.7f));
        Destroy(gameObject);
    }
}
