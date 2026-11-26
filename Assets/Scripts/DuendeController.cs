using System.Collections;
using UnityEngine;

// Personaje del jugador: movimiento, sprint, salto, vidas, power-ups y animaciones.
// Funciona con teclado/mouse y con gamepad (ver Entrada.cs).
[RequireComponent(typeof(CharacterController))]
public class DuendeController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6f;
    public float velocidadSprint = 9.5f;
    public float fuerzaSalto = 1.6f;
    public float gravedad = -22f;
    public float aceleracion = 50f;
    public float aceleracionHielo = 5f;
    public float velocidadGiro = 720f;

    [Header("Sprint")]
    public float energiaMaxima = 100f;
    public float consumoSprint = 35f;
    public float recargaEnergia = 22f;

    [Header("Vidas")]
    public int vidasMaximas = 3;
    public float tiempoInvulnerable = 2f;

    [Header("Power-ups")]
    public GameObject prefabBolaNieve;
    public float fuerzaLanzamiento = 17f;
    public float radioIman = 5f;
    public GameObject burbujaEscudo;
    public ParticleSystem efectoIman;
    public GameObject efectoEscudoRoto;

    [Header("Referencias")]
    public Animator animator;
    [Tooltip("Un modelo por personaje (mismo orden que Personajes.Lista)")]
    public GameObject[] modelos;
    public Animator[] animadores;
    public ParticleSystem efectoBoost;
    public Transform costal;

    private CharacterController controller;
    private Renderer[] renderers;
    private Vector3 velocidadPlano;
    private Vector3 empuje;
    private float velocidadY;
    private float energia;
    private bool agotado;
    private int vidas;
    private float finInvulnerable;
    private float finBoost;
    private float finIman;
    private bool muerto;
    private int zonasHielo;
    private float ultimoSuelo;
    private Vector3 puntoInicio;
    private int bolas;
    private bool escudo;
    private Vector3 escalaCostal = Vector3.one;

    public bool Bloqueado { get; set; }
    public bool Muerto => muerto;
    public int Vidas => vidas;
    public float EnergiaNormalizada => energia / energiaMaxima;
    public bool Agotado => agotado;
    public bool EnHielo => zonasHielo > 0;
    public bool BoostActivo => Time.time < finBoost;
    public float BoostRestante => Mathf.Max(0f, finBoost - Time.time);
    public bool ImanActivo => Time.time < finIman;
    public float ImanRestante => Mathf.Max(0f, finIman - Time.time);
    public int Bolas => bolas;
    public bool TieneEscudo => escudo;
    public Vector3 PosicionCostal => costal != null ? costal.position : transform.position + Vector3.up;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        AplicarPersonaje();
        renderers = GetComponentsInChildren<Renderer>();
        vidas = vidasMaximas;
        energia = energiaMaxima;
        if (burbujaEscudo != null) burbujaEscudo.SetActive(false);
        if (costal != null) escalaCostal = costal.localScale;
    }

    void AplicarPersonaje()
    {
        if (modelos == null || modelos.Length == 0)
            return;

        int indice = Mathf.Clamp(GameData.Personaje, 0, modelos.Length - 1);
        for (int i = 0; i < modelos.Length; i++)
            if (modelos[i] != null) modelos[i].SetActive(i == indice);
        if (animadores != null && indice < animadores.Length)
            animator = animadores[indice];

        Personajes.Info info = Personajes.Lista[Mathf.Min(indice, Personajes.Lista.Length - 1)];
        velocidad = info.velocidad;
        velocidadSprint = info.velocidadSprint;
        energiaMaxima = info.energia;
        recargaEnergia = info.recarga;
        vidasMaximas = info.vidas;
    }

    void Start()
    {
        puntoInicio = transform.position;
        if (NivelManager.Instancia != null)
            NivelManager.Instancia.ActualizarVidas(vidas, vidasMaximas);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 direccion = Vector3.zero;
        bool quiereSprint = false;
        bool salto = false;
        bool lanzar = false;

        if (!Bloqueado && !muerto)
        {
            Vector2 m = Entrada.Mover();
            direccion = new Vector3(m.x, 0f, m.y);
            quiereSprint = Entrada.Correr() && direccion.sqrMagnitude > 0.01f;
            salto = Entrada.Saltar();
            lanzar = Entrada.Lanzar();
        }

        // Energía del sprint
        bool sprintando = quiereSprint && !agotado;
        if (sprintando)
        {
            energia -= consumoSprint * dt;
            if (energia <= 0f)
            {
                energia = 0f;
                agotado = true;
            }
        }
        else
        {
            energia = Mathf.Min(energiaMaxima, energia + recargaEnergia * dt);
            if (agotado && energia >= energiaMaxima * 0.35f)
                agotado = false;
        }

        float velocidadMax = sprintando ? velocidadSprint : velocidad;
        if (BoostActivo)
            velocidadMax *= 1.4f;

        Vector3 objetivo = direccion * velocidadMax;
        float acc = EnHielo ? aceleracionHielo : aceleracion;
        velocidadPlano = Vector3.MoveTowards(velocidadPlano, objetivo, acc * dt);

        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, velocidadGiro * dt);
        }

        if (controller.isGrounded)
        {
            ultimoSuelo = Time.time;
            if (velocidadY < 0f)
                velocidadY = -2f;
        }

        bool enSuelo = Time.time - ultimoSuelo < 0.12f;
        if (salto && enSuelo)
        {
            velocidadY = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
            ultimoSuelo = -10f;
            AudioManager.SFX("salto", 0.7f);
        }

        if (lanzar)
            LanzarBola();

        velocidadY += gravedad * dt;
        empuje = Vector3.MoveTowards(empuje, Vector3.zero, 14f * dt);

        controller.Move((velocidadPlano + empuje + Vector3.up * velocidadY) * dt);

        if (animator != null)
        {
            Vector3 plano = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            animator.SetFloat("Velocidad", plano.magnitude);
            animator.SetBool("EnSuelo", enSuelo);
        }

        ActualizarEfectos();

        // Parpadeo de invulnerabilidad
        bool invulnerable = Time.time < finInvulnerable && !muerto;
        bool visible = !invulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
        foreach (Renderer r in renderers)
            if (r != null && !(r is ParticleSystemRenderer) && (burbujaEscudo == null || !r.transform.IsChildOf(burbujaEscudo.transform)))
                r.enabled = visible;

        // Caída fuera del mapa
        if (transform.position.y < -15f && !muerto)
        {
            Teletransportar(puntoInicio);
            finInvulnerable = 0f;
            RecibirDanio(1, transform.position);
        }
    }

    void ActualizarEfectos()
    {
        if (efectoBoost != null)
        {
            if (BoostActivo && !efectoBoost.isPlaying) efectoBoost.Play();
            else if (!BoostActivo && efectoBoost.isPlaying) efectoBoost.Stop();
        }

        if (efectoIman != null)
        {
            if (ImanActivo && !efectoIman.isPlaying) efectoIman.Play();
            else if (!ImanActivo && efectoIman.isPlaying) efectoIman.Stop();
        }

        if (burbujaEscudo != null)
        {
            burbujaEscudo.SetActive(escudo);
            if (escudo)
            {
                float s = 1f + Mathf.Sin(Time.time * 4f) * 0.04f;
                burbujaEscudo.transform.localScale = Vector3.one * 2.4f * s;
            }
        }
    }

    // ---------------- Power-ups ----------------

    void LanzarBola()
    {
        if (bolas <= 0 || prefabBolaNieve == null)
            return;

        bolas--;
        Vector3 origen = transform.position + Vector3.up * 1.2f + transform.forward * 0.7f;
        GameObject b = Instantiate(prefabBolaNieve, origen, Quaternion.identity);
        Rigidbody rb = b.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * fuerzaLanzamiento + Vector3.up * 3f + new Vector3(velocidadPlano.x, 0f, velocidadPlano.z) * 0.5f;
        AudioManager.SFX("salto", 0.6f, 0.2f);
    }

    public void AgregarBolas(int cantidad) => bolas = Mathf.Min(bolas + cantidad, 9);
    public void ActivarEscudo() => escudo = true;
    public void ActivarIman(float segundos) => finIman = Time.time + segundos;

    public void ActivarBoost(float segundos)
    {
        finBoost = Time.time + segundos;
        energia = energiaMaxima;
        agotado = false;
    }

    public void SacudirCostal()
    {
        if (costal != null)
            StartCoroutine(PopCostal());
    }

    IEnumerator PopCostal()
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / 0.25f)
        {
            costal.localScale = escalaCostal * (1f + Mathf.Sin(x * Mathf.PI) * 0.35f);
            yield return null;
        }
        costal.localScale = escalaCostal;
    }

    // ---------------- Daño ----------------

    public void Teletransportar(Vector3 posicion)
    {
        controller.enabled = false;
        transform.position = posicion;
        controller.enabled = true;
        velocidadPlano = Vector3.zero;
        velocidadY = 0f;
    }

    public void RecibirDanio(int cantidad, Vector3 origen)
    {
        if (muerto || Time.time < finInvulnerable)
            return;

        Vector3 dir = transform.position - origen;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
        empuje = dir.normalized * 13f;
        velocidadY = 5f;

        if (escudo)
        {
            escudo = false;
            finInvulnerable = Time.time + 1.2f;
            AudioManager.SFX("pop");
            if (efectoEscudoRoto != null)
                Instantiate(efectoEscudoRoto, transform.position + Vector3.up, Quaternion.identity);
            if (NivelManager.Instancia != null)
                NivelManager.Instancia.MostrarAviso("¡El escudo te protegió!", new Color(0.6f, 0.85f, 1f));
            return;
        }

        vidas = Mathf.Max(0, vidas - cantidad);
        finInvulnerable = Time.time + tiempoInvulnerable;

        AudioManager.SFX("dano");
        CamaraSeguimiento.Sacudir(0.45f);

        if (NivelManager.Instancia != null)
            NivelManager.Instancia.ActualizarVidas(vidas, vidasMaximas);

        if (vidas <= 0)
            Morir();
    }

    void Morir()
    {
        muerto = true;
        velocidadPlano = Vector3.zero;
        if (animator != null)
            animator.SetTrigger("Morir");

        if (NivelManager.Instancia != null)
            NivelManager.Instancia.JugadorMurio();
    }

    public void Celebrar()
    {
        Bloqueado = true;
        velocidadPlano = Vector3.zero;
        if (animator != null)
            animator.SetTrigger("Celebrar");
    }

    public void EntrarHielo() => zonasHielo++;
    public void SalirHielo() => zonasHielo = Mathf.Max(0, zonasHielo - 1);
}
