using UnityEngine;
using UnityEngine.InputSystem;

// Pino, el duende: movimiento, sprint, salto, vidas y animaciones.
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

    [Header("Referencias")]
    public Animator animator;
    [Tooltip("Un modelo por personaje (mismo orden que Personajes.Lista)")]
    public GameObject[] modelos;
    public Animator[] animadores;
    public ParticleSystem efectoBoost;

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
    private bool muerto;
    private int zonasHielo;
    private float ultimoSuelo;
    private Vector3 puntoInicio;

    public bool Bloqueado { get; set; }
    public bool Muerto => muerto;
    public int Vidas => vidas;
    public float EnergiaNormalizada => energia / energiaMaxima;
    public bool Agotado => agotado;
    public bool EnHielo => zonasHielo > 0;
    public bool BoostActivo => Time.time < finBoost;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        AplicarPersonaje();
        renderers = GetComponentsInChildren<Renderer>();
        vidas = vidasMaximas;
        energia = energiaMaxima;
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

        Keyboard k = Keyboard.current;
        if (k != null && !Bloqueado && !muerto)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) direccion.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) direccion.x += 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) direccion.z += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) direccion.z -= 1f;
            direccion = Vector3.ClampMagnitude(direccion, 1f);
            quiereSprint = (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) && direccion.sqrMagnitude > 0.01f;
            salto = k.spaceKey.wasPressedThisFrame;
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

        velocidadY += gravedad * dt;
        empuje = Vector3.MoveTowards(empuje, Vector3.zero, 14f * dt);

        controller.Move((velocidadPlano + empuje + Vector3.up * velocidadY) * dt);

        if (animator != null)
        {
            Vector3 plano = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
            animator.SetFloat("Velocidad", plano.magnitude);
            animator.SetBool("EnSuelo", enSuelo);
        }

        if (efectoBoost != null)
        {
            if (BoostActivo && !efectoBoost.isPlaying) efectoBoost.Play();
            else if (!BoostActivo && efectoBoost.isPlaying) efectoBoost.Stop();
        }

        // Parpadeo de invulnerabilidad
        bool invulnerable = Time.time < finInvulnerable && !muerto;
        bool visible = !invulnerable || Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
        foreach (Renderer r in renderers)
            if (r != null && !(r is ParticleSystemRenderer))
                r.enabled = visible;

        // Caída fuera del mapa
        if (transform.position.y < -15f && !muerto)
        {
            Teletransportar(puntoInicio);
            finInvulnerable = 0f;
            RecibirDanio(1, transform.position);
        }
    }

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

        vidas = Mathf.Max(0, vidas - cantidad);
        finInvulnerable = Time.time + tiempoInvulnerable;

        Vector3 dir = transform.position - origen;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -transform.forward;
        empuje = dir.normalized * 13f;
        velocidadY = 5f;

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

    public void ActivarBoost(float segundos)
    {
        finBoost = Time.time + segundos;
        energia = energiaMaxima;
        agotado = false;
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
