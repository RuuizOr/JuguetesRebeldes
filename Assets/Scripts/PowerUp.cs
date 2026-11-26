using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TipoPowerUp
{
    Velocidad,
    BolaNieve,
    Iman,
    Escudo
}

// Power-up que se recoge al tocarlo y reaparece después de un tiempo.
public class PowerUp : MonoBehaviour
{
    public TipoPowerUp tipo;
    public float duracion = 5f;
    public int cantidad = 3;
    public float reaparecer = 25f;
    public GameObject visual;
    public GameObject efecto;

    private static readonly List<PowerUp> todos = new List<PowerUp>();
    public static IReadOnlyList<PowerUp> Todos => todos;

    private Collider col;
    public bool Disponible { get; private set; } = true;

    void Awake() => col = GetComponent<Collider>();
    void OnEnable() => todos.Add(this);
    void OnDisable() => todos.Remove(this);

    void OnTriggerEnter(Collider other)
    {
        if (!Disponible || !other.CompareTag("Player"))
            return;

        DuendeController d = other.GetComponent<DuendeController>();
        if (d == null || d.Muerto)
            return;

        string mensaje = "";
        Color color = Color.white;
        switch (tipo)
        {
            case TipoPowerUp.Velocidad:
                d.ActivarBoost(duracion);
                mensaje = "¡Bastón de dulce! Súper velocidad";
                color = new Color(1f, 0.6f, 0.7f);
                break;
            case TipoPowerUp.BolaNieve:
                d.AgregarBolas(cantidad);
                mensaje = "¡+" + cantidad + " bolas de nieve! (E / clic / X para lanzar)";
                color = new Color(0.7f, 0.9f, 1f);
                break;
            case TipoPowerUp.Iman:
                d.ActivarIman(duracion);
                mensaje = "¡Imán de regalos! Atrae a los juguetes cercanos";
                color = new Color(0.85f, 0.6f, 1f);
                break;
            case TipoPowerUp.Escudo:
                d.ActivarEscudo();
                mensaje = "¡Escudo! Aguanta un golpe del Cascanueces";
                color = new Color(0.55f, 0.85f, 1f);
                break;
        }

        AudioManager.SFX("powerup");
        if (efecto != null)
            Instantiate(efecto, transform.position + Vector3.up, Quaternion.identity);
        if (NivelManager.Instancia != null)
            NivelManager.Instancia.MostrarAviso(mensaje, color);

        StartCoroutine(Reaparecer());
    }

    IEnumerator Reaparecer()
    {
        Disponible = false;
        if (col != null) col.enabled = false;
        if (visual != null) visual.SetActive(false);
        yield return new WaitForSeconds(reaparecer);
        Disponible = true;
        if (col != null) col.enabled = true;
        if (visual != null) visual.SetActive(true);
    }
}
