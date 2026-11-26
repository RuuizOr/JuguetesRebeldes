using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EstadoJuguete
{
    Deambular,
    Huir,
    Cansado,
    Escapar,
    Atrapado,
    Escapado
}

// Juguete rebelde con máquina de estados "invertida": en vez de perseguir, huye del jugador.
[RequireComponent(typeof(NavMeshAgent))]
public class Juguete : MonoBehaviour
{
    public string nombreJuguete = "Juguete";

    [Header("Velocidades")]
    public float velocidadDeambular = 2.2f;
    public float velocidadHuir = 6.2f;
    public float velocidadEscapar = 4.2f;

    [Header("Detección")]
    public float rangoMiedo = 6f;
    public float rangoCalma = 11f;
    public float rangoPanicoEscapando = 3.5f;

    [Header("Cansancio")]
    public float tiempoMaxHuyendo = 3f;
    public float tiempoCansado = 2.8f;

    [Header("Intento de escape (segundos de juego)")]
    public Vector2 tiempoPrimerIntento = new Vector2(30f, 60f);

    [Header("Referencias")]
    public Animator animator;
    public TextMesh etiqueta;
    public GameObject efectoAtrapar;
    public GameObject efectoEscape;

    [Header("Estado actual")]
    [SerializeField] private EstadoJuguete estadoActual;

    private NavMeshAgent agente;
    private Transform jugador;
    private PuertaSalida puerta;
    private float tiempoEnEstado;
    private float huyendoAcumulado;
    private float momentoIntento;
    private float siguienteRecalculo;
    private bool avisado;

    public EstadoJuguete Estado => estadoActual;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) jugador = p.transform;

        momentoIntento = Random.Range(tiempoPrimerIntento.x, tiempoPrimerIntento.y);

        if (NivelManager.Instancia != null)
            NivelManager.Instancia.RegistrarJuguete(this);

        CambiarEstado(EstadoJuguete.Deambular);
    }

    void Update()
    {
        if (estadoActual == EstadoJuguete.Atrapado || estadoActual == EstadoJuguete.Escapado)
            return;

        if (animator != null)
            animator.SetFloat("Velocidad", agente.velocity.magnitude);

        bool enJuego = NivelManager.Instancia == null || NivelManager.Instancia.EnJuego;
        if (!enJuego || !agente.isOnNavMesh || jugador == null)
        {
            if (agente.isOnNavMesh) agente.isStopped = true;
            return;
        }

        agente.isStopped = estadoActual == EstadoJuguete.Cansado;
        tiempoEnEstado += Time.deltaTime;
        float distancia = DistanciaJugador();

        switch (estadoActual)
        {
            case EstadoJuguete.Deambular:
                huyendoAcumulado = Mathf.Max(0f, huyendoAcumulado - Time.deltaTime);
                if (distancia <= rangoMiedo) { CambiarEstado(EstadoJuguete.Huir); break; }
                if (DebeEscapar()) { CambiarEstado(EstadoJuguete.Escapar); break; }
                if (!agente.pathPending && (agente.remainingDistance < 0.6f || tiempoEnEstado > 7f))
                    NuevoDestinoAleatorio();
                break;

            case EstadoJuguete.Huir:
                huyendoAcumulado += Time.deltaTime;
                if (huyendoAcumulado >= tiempoMaxHuyendo) { CambiarEstado(EstadoJuguete.Cansado); break; }
                if (distancia > rangoCalma) { CambiarEstado(DebeEscapar() ? EstadoJuguete.Escapar : EstadoJuguete.Deambular); break; }
                if (Time.time >= siguienteRecalculo) HuirDelJugador();
                break;

            case EstadoJuguete.Cansado:
                if (tiempoEnEstado >= tiempoCansado)
                {
                    huyendoAcumulado = 0f;
                    if (distancia <= rangoMiedo) CambiarEstado(EstadoJuguete.Huir);
                    else CambiarEstado(DebeEscapar() ? EstadoJuguete.Escapar : EstadoJuguete.Deambular);
                }
                break;

            case EstadoJuguete.Escapar:
                if (distancia <= rangoPanicoEscapando) { CambiarEstado(EstadoJuguete.Huir); break; }
                if (puerta == null) puerta = PuertaSalida.MasCercana(transform.position);
                if (puerta == null) break;
                if (Time.time >= siguienteRecalculo)
                {
                    agente.SetDestination(puerta.PuntoEscape);
                    siguienteRecalculo = Time.time + 0.5f;
                }
                Vector3 a = transform.position; a.y = 0f;
                Vector3 b = puerta.PuntoEscape; b.y = 0f;
                if (Vector3.Distance(a, b) <= puerta.radioEscape)
                    Escaparse();
                break;
        }
    }

    bool DebeEscapar()
    {
        NivelManager nm = NivelManager.Instancia;
        if (nm == null) return false;
        return nm.TiempoJuego >= momentoIntento || nm.TiempoRestante <= nm.tiempoAvisoFinal;
    }

    void CambiarEstado(EstadoJuguete nuevo)
    {
        estadoActual = nuevo;
        tiempoEnEstado = 0f;
        siguienteRecalculo = 0f;

        if (agente == null || !agente.isOnNavMesh)
            return;

        agente.isStopped = false;

        switch (nuevo)
        {
            case EstadoJuguete.Deambular:
                agente.speed = velocidadDeambular;
                NuevoDestinoAleatorio();
                Etiqueta("", Color.white);
                break;

            case EstadoJuguete.Huir:
                agente.speed = velocidadHuir;
                HuirDelJugador();
                Etiqueta("!", new Color(1f, 0.9f, 0.3f));
                break;

            case EstadoJuguete.Cansado:
                agente.isStopped = true;
                agente.ResetPath();
                Etiqueta("Zzz", new Color(0.6f, 0.85f, 1f));
                break;

            case EstadoJuguete.Escapar:
                agente.speed = velocidadEscapar;
                puerta = PuertaSalida.MasCercana(transform.position);
                if (puerta != null) agente.SetDestination(puerta.PuntoEscape);
                Etiqueta("¡!", new Color(1f, 0.25f, 0.25f));
                if (!avisado)
                {
                    avisado = true;
                    if (NivelManager.Instancia != null)
                        NivelManager.Instancia.AvisoEscape(this);
                }
                break;
        }
    }

    void Etiqueta(string texto, Color color)
    {
        if (etiqueta == null) return;
        etiqueta.text = texto;
        etiqueta.color = color;
    }

    void NuevoDestinoAleatorio()
    {
        tiempoEnEstado = 0f;
        Vector3 centro = transform.position;
        float radio = 8f;
        if (NivelManager.Instancia != null)
        {
            centro = NivelManager.Instancia.centroNivel;
            radio = NivelManager.Instancia.radioNivel;
        }

        for (int i = 0; i < 12; i++)
        {
            Vector2 r = Random.insideUnitCircle * radio;
            Vector3 p = new Vector3(centro.x + r.x, transform.position.y, centro.z + r.y);
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agente.SetDestination(hit.position);
                return;
            }
        }
    }

    void HuirDelJugador()
    {
        siguienteRecalculo = Time.time + 0.3f;

        Vector3 lejos = transform.position - jugador.position;
        lejos.y = 0f;
        if (lejos.sqrMagnitude < 0.01f) lejos = transform.forward;
        lejos.Normalize();

        float mejorPuntaje = float.MinValue;
        Vector3 mejor = transform.position;
        bool encontrado = false;
        float[] angulos = { 0f, 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f };

        foreach (float ang in angulos)
        {
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * lejos;
            Vector3 candidato = transform.position + dir * 6f;
            if (!NavMesh.SamplePosition(candidato, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                continue;

            float puntaje = Vector3.Distance(hit.position, jugador.position) - Mathf.Abs(ang) * 0.01f;
            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = hit.position;
                encontrado = true;
            }
        }

        if (encontrado)
            agente.SetDestination(mejor);
    }

    float DistanciaJugador()
    {
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = jugador.position; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (estadoActual == EstadoJuguete.Atrapado || estadoActual == EstadoJuguete.Escapado)
            return;
        if (NivelManager.Instancia != null && !NivelManager.Instancia.EnJuego)
            return;

        DuendeController d = other.GetComponent<DuendeController>();
        if (d != null && d.Muerto)
            return;

        Atrapar();
    }

    void Atrapar()
    {
        estadoActual = EstadoJuguete.Atrapado;
        Finalizar(efectoAtrapar);
        AudioManager.SFX("atrapar");
        if (NivelManager.Instancia != null)
            NivelManager.Instancia.JugueteAtrapado(this);
    }

    void Escaparse()
    {
        estadoActual = EstadoJuguete.Escapado;
        Finalizar(efectoEscape);
        AudioManager.SFX("escape");
        if (NivelManager.Instancia != null)
            NivelManager.Instancia.JugueteEscapado(this);
    }

    void Finalizar(GameObject efecto)
    {
        Etiqueta("", Color.white);
        if (agente.isOnNavMesh) { agente.isStopped = true; agente.ResetPath(); }
        agente.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (efecto != null)
            Instantiate(efecto, transform.position + Vector3.up * 0.8f, Quaternion.identity);

        StartCoroutine(Encoger());
    }

    IEnumerator Encoger()
    {
        Vector3 inicial = transform.localScale;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            float s = t < 0.3f ? 1f + t : (1.3f - (t - 0.3f) / 0.7f * 1.3f);
            transform.localScale = inicial * Mathf.Max(0f, s);
            yield return null;
        }
        Destroy(gameObject);
    }
}
