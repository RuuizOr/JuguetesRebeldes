using UnityEngine;
using UnityEngine.AI;

public enum EstadoCascanueces
{
    Patrullar,
    Perseguir,
    Atacar,
    Regresar
}

// Cascanueces gigante: máquina de estados finitos (Patrullar, Perseguir, Atacar, Regresar) con NavMesh.
[RequireComponent(typeof(NavMeshAgent))]
public class Cascanueces : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public Transform[] puntosPatrulla;
    public Light luzOjos;

    [Header("Detección")]
    public float rangoDeteccion = 9f;
    public float rangoPerdida = 14f;
    public float rangoAtaque = 2.2f;

    [Header("Movimiento")]
    public float velocidadPatrulla = 2.5f;
    public float velocidadPersecucion = 4.8f;

    [Header("Combate")]
    public int danio = 1;
    public float tiempoEntreAtaques = 2f;
    public float pausaTrasGolpe = 1.4f;

    [Header("Estado actual")]
    [SerializeField] private EstadoCascanueces estadoActual;

    private NavMeshAgent agente;
    private DuendeController duende;
    private int indicePunto;
    private float siguienteAtaque;
    private float pausaHasta;

    public EstadoCascanueces Estado => estadoActual;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        if (jugador == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) jugador = p.transform;
        }
        if (jugador != null)
            duende = jugador.GetComponent<DuendeController>();

        CambiarEstado(EstadoCascanueces.Patrullar);
    }

    void Update()
    {
        if (jugador == null || !agente.isOnNavMesh)
            return;

        bool enJuego = NivelManager.Instancia == null || NivelManager.Instancia.EnJuego;
        bool aturdido = Time.time < pausaHasta;
        agente.isStopped = !enJuego || aturdido || estadoActual == EstadoCascanueces.Atacar;
        if (!enJuego || aturdido)
            return;

        if (duende != null && duende.Muerto && estadoActual != EstadoCascanueces.Regresar && estadoActual != EstadoCascanueces.Patrullar)
        {
            CambiarEstado(EstadoCascanueces.Regresar);
            return;
        }

        switch (estadoActual)
        {
            case EstadoCascanueces.Patrullar: Patrullar(); break;
            case EstadoCascanueces.Perseguir: Perseguir(); break;
            case EstadoCascanueces.Atacar: Atacar(); break;
            case EstadoCascanueces.Regresar: Regresar(); break;
        }

        if (luzOjos != null)
        {
            bool alerta = estadoActual == EstadoCascanueces.Perseguir || estadoActual == EstadoCascanueces.Atacar;
            luzOjos.color = Color.Lerp(luzOjos.color, alerta ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.75f, 0.3f), Time.deltaTime * 4f);
            luzOjos.intensity = alerta ? 3f + Mathf.Sin(Time.time * 12f) : 1.5f;
        }
    }

    void CambiarEstado(EstadoCascanueces nuevo)
    {
        estadoActual = nuevo;
        if (agente == null || !agente.isOnNavMesh)
            return;

        agente.isStopped = false;

        switch (nuevo)
        {
            case EstadoCascanueces.Patrullar:
                agente.speed = velocidadPatrulla;
                IrAlPuntoActual();
                break;
            case EstadoCascanueces.Perseguir:
                agente.speed = velocidadPersecucion;
                AudioManager.SFX("alerta", 0.5f);
                break;
            case EstadoCascanueces.Atacar:
                agente.isStopped = true;
                agente.ResetPath();
                break;
            case EstadoCascanueces.Regresar:
                agente.speed = velocidadPatrulla;
                IrAlPuntoActual();
                break;
        }

        Debug.Log("Cascanueces " + name + ": " + estadoActual);
    }

    bool JugadorDisponible() => duende == null || !duende.Muerto;

    void Patrullar()
    {
        if (JugadorDisponible() && DistanciaJugador() <= rangoDeteccion)
        {
            CambiarEstado(EstadoCascanueces.Perseguir);
            return;
        }

        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
            return;

        if (!agente.pathPending && agente.remainingDistance <= 0.6f)
        {
            indicePunto = (indicePunto + 1) % puntosPatrulla.Length;
            IrAlPuntoActual();
        }
    }

    void Perseguir()
    {
        float d = DistanciaJugador();
        if (d > rangoPerdida) { CambiarEstado(EstadoCascanueces.Regresar); return; }
        if (d <= rangoAtaque) { CambiarEstado(EstadoCascanueces.Atacar); return; }
        agente.SetDestination(jugador.position);
    }

    void Atacar()
    {
        float d = DistanciaJugador();
        if (d > rangoPerdida) { CambiarEstado(EstadoCascanueces.Regresar); return; }
        if (d > rangoAtaque) { CambiarEstado(EstadoCascanueces.Perseguir); return; }

        Vector3 mirar = jugador.position - transform.position;
        mirar.y = 0f;
        if (mirar.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mirar), Time.deltaTime * 8f);

        if (Time.time >= siguienteAtaque)
        {
            siguienteAtaque = Time.time + tiempoEntreAtaques;
            if (duende != null)
            {
                duende.RecibirDanio(danio, transform.position);
                pausaHasta = Time.time + pausaTrasGolpe;
                CambiarEstado(EstadoCascanueces.Perseguir);
            }
        }
    }

    void Regresar()
    {
        if (JugadorDisponible() && DistanciaJugador() <= rangoDeteccion)
        {
            CambiarEstado(EstadoCascanueces.Perseguir);
            return;
        }

        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
            return;

        if (!agente.pathPending && agente.remainingDistance <= 0.6f)
            CambiarEstado(EstadoCascanueces.Patrullar);
    }

    void IrAlPuntoActual()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
            return;
        Transform p = puntosPatrulla[indicePunto];
        if (p != null && agente.isOnNavMesh)
            agente.SetDestination(p.position);
    }

    float DistanciaJugador()
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = jugador.position; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
