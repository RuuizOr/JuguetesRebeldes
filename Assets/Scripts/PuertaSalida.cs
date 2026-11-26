using System.Collections.Generic;
using UnityEngine;

// Puerta por la que los juguetes intentan escapar. Se ilumina en rojo cuando alguno va hacia ella.
public class PuertaSalida : MonoBehaviour
{
    public Transform puntoEscape;
    public float radioEscape = 1.3f;
    public Light luz;
    public Renderer[] brillo;

    private static readonly List<PuertaSalida> todas = new List<PuertaSalida>();
    private Color colorNormal = new Color(1f, 0.8f, 0.45f);

    public Vector3 PuntoEscape => puntoEscape != null ? puntoEscape.position : transform.position;

    void OnEnable() => todas.Add(this);
    void OnDisable() => todas.Remove(this);

    public static PuertaSalida MasCercana(Vector3 desde)
    {
        PuertaSalida mejor = null;
        float min = float.MaxValue;
        foreach (PuertaSalida p in todas)
        {
            float d = (p.PuntoEscape - desde).sqrMagnitude;
            if (d < min) { min = d; mejor = p; }
        }
        return mejor;
    }

    void Update()
    {
        bool alerta = NivelManager.Instancia != null && NivelManager.Instancia.HayJuguetesEscapando;
        if (luz != null)
        {
            luz.color = alerta ? Color.Lerp(Color.red, new Color(1f, 0.4f, 0.2f), Mathf.PingPong(Time.time * 3f, 1f)) : colorNormal;
            luz.intensity = alerta ? 6f + Mathf.Sin(Time.time * 10f) * 2f : 3f;
        }
    }
}
