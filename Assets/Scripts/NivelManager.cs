using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controla un nivel: cuenta regresiva, tiempo, juguetes atrapados/escapados, HUD, pausa y pantallas finales.
public class NivelManager : MonoBehaviour
{
    public static NivelManager Instancia { get; private set; }

    [Header("Nivel")]
    public int numeroNivel = 1;
    public string nombreNivel = "El Taller";
    public float tiempoLimite = 150f;
    public float tiempoAvisoFinal = 30f;
    public int maxEscapes = 3;
    public Vector3 centroNivel;
    public float radioNivel = 10f;
    public DuendeController jugador;

    [Header("HUD")]
    public Text textoAtrapados;
    public Text textoEscapados;
    public Text textoTiempo;
    public Text textoNivel;
    public Text textoCentro;
    public Text textoAviso;
    public Image[] corazones;
    public Sprite spriteCorazon;
    public Sprite spriteCorazonVacio;
    public Image barraEnergia;
    public Text textoPuntos;
    public Text textoCombo;
    [Tooltip("Velocidad, Bolas, Imán, Escudo")]
    public CanvasGroup[] slotsPowerUp;
    public Text[] textosPowerUp;

    [Header("Paneles")]
    public GameObject panelPausa;
    public GameObject panelOpciones;
    public GameObject panelCompletado;
    public GameObject panelGameOver;
    public GameObject panelVictoriaFinal;
    public Image[] estrellas;
    public Sprite spriteEstrella;
    public Sprite spriteEstrellaVacia;
    public Text textoResumen;
    public Text textoMotivo;
    public Text textoResumenFinal;
    public PanelResultado resultadoCompletado;
    public PanelResultado resultadoGameOver;
    public PanelResultado resultadoVictoria;

    [Header("Botones")]
    public Button botonContinuar;
    public Button botonReiniciarPausa;
    public Button botonOpcionesPausa;
    public Button botonMenuPausa;
    public Button botonVolverOpciones;
    public Button botonSiguiente;
    public Button botonMenuCompletado;
    public Button botonReintentar;
    public Button botonMenuGameOver;
    public Button botonMenuFinal;
    public Button botonVerFinal;

    private readonly List<Juguete> juguetes = new List<Juguete>();
    private int total;
    private int atrapados;
    private int escapados;
    private float tiempoJuego;
    private bool enJuego;
    private bool terminado;
    private bool pausado;
    private bool avisoFinalDado;
    private Coroutine rutinaAviso;
    private Coroutine rutinaCombo;
    private int puntos;
    private int combo;
    private float ultimoAtrapado = -100f;

    public int Puntos => puntos;
    public IReadOnlyList<Juguete> Juguetes => juguetes;

    public bool EnJuego => enJuego && !terminado && !pausado;
    public float TiempoJuego => tiempoJuego;
    public float TiempoRestante => tiempoLimite - tiempoJuego;

    public bool HayJuguetesEscapando
    {
        get
        {
            foreach (Juguete j in juguetes)
                if (j != null && j.Estado == EstadoJuguete.Escapar)
                    return true;
            return false;
        }
    }

    void Awake()
    {
        Instancia = this;
        Time.timeScale = 1f;
        GameData.NivelActual = numeroNivel;

        Ocultar(panelPausa, panelOpciones, panelCompletado, panelGameOver, panelVictoriaFinal);
        if (textoAviso != null) textoAviso.text = "";
        if (textoCentro != null) textoCentro.text = "";

        Conectar(botonContinuar, Reanudar);
        Conectar(botonReiniciarPausa, Reiniciar);
        Conectar(botonOpcionesPausa, AbrirOpciones);
        Conectar(botonMenuPausa, IrAlMenu);
        Conectar(botonVolverOpciones, CerrarOpciones);
        Conectar(botonSiguiente, SiguienteNivel);
        Conectar(botonMenuCompletado, IrAlMenu);
        Conectar(botonReintentar, Reiniciar);
        Conectar(botonMenuGameOver, IrAlMenu);
        Conectar(botonMenuFinal, IrAlMenu);
        Conectar(botonVerFinal, () => SceneLoader.Cargar("Final", "¡Feliz Navidad!"));
        if (textoCombo != null) textoCombo.text = "";
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    static void Conectar(Button b, UnityEngine.Events.UnityAction accion)
    {
        if (b != null) b.onClick.AddListener(accion);
    }

    static void Ocultar(params GameObject[] objetos)
    {
        foreach (GameObject o in objetos)
            if (o != null) o.SetActive(false);
    }

    void Start()
    {
        AudioManager.MusicaJuego();
        if (textoNivel != null) textoNivel.text = "Nivel " + numeroNivel + ": " + nombreNivel;
        ActualizarContadores();
        ActualizarTiempo();
        StartCoroutine(CuentaRegresiva());
    }

    IEnumerator CuentaRegresiva()
    {
        if (jugador != null) jugador.Bloqueado = true;

        yield return new WaitForSeconds(0.6f);
        textoCentro.fontSize = 70;
        textoCentro.text = "Nivel " + numeroNivel + "\n<size=48>" + nombreNivel + "</size>";
        yield return new WaitForSeconds(1.8f);

        textoCentro.fontSize = 140;
        for (int i = 3; i >= 1; i--)
        {
            textoCentro.text = i.ToString();
            AudioManager.SFX("cuenta", 1f, 0f);
            StartCoroutine(Pop(textoCentro.transform));
            yield return new WaitForSeconds(0.8f);
        }

        textoCentro.text = "¡A atrapar!";
        AudioManager.SFX("go", 1f, 0f);
        StartCoroutine(Pop(textoCentro.transform));
        enJuego = true;
        if (jugador != null) jugador.Bloqueado = false;

        yield return new WaitForSeconds(0.9f);
        textoCentro.text = "";
    }

    void Update()
    {
        if (Entrada.Pausa() && enJuego && !terminado && !SceneLoader.Cargando)
        {
            if (pausado)
            {
                if (panelOpciones != null && panelOpciones.activeSelf) CerrarOpciones();
                else Reanudar();
            }
            else Pausar();
        }

        if (barraEnergia != null && jugador != null)
        {
            barraEnergia.fillAmount = jugador.EnergiaNormalizada;
            barraEnergia.color = jugador.BoostActivo ? new Color(1f, 0.5f, 0.75f) :
                                 jugador.Agotado ? new Color(0.9f, 0.35f, 0.3f) : new Color(0.45f, 0.85f, 1f);
        }

        ActualizarPowerUps();

        if (!EnJuego)
            return;

        tiempoJuego += Time.deltaTime;
        ActualizarTiempo();

        if (!avisoFinalDado && TiempoRestante <= tiempoAvisoFinal)
        {
            avisoFinalDado = true;
            MostrarAviso("¡Últimos " + Mathf.RoundToInt(tiempoAvisoFinal) + " segundos! Todos corren a la puerta", new Color(1f, 0.4f, 0.35f));
            AudioManager.SFX("alerta");
        }

        if (TiempoRestante <= 0f)
            GameOver("¡Se acabó el tiempo!\nLos juguetes que quedaban se escaparon.");
    }

    // ---------------- Juguetes ----------------

    public void RegistrarJuguete(Juguete j)
    {
        if (juguetes.Contains(j)) return;
        juguetes.Add(j);
        total++;
        ActualizarContadores();
    }

    public void JugueteAtrapado(Juguete j, bool estabaCongelado)
    {
        atrapados++;

        combo = Time.time - ultimoAtrapado <= 5f ? combo + 1 : 1;
        ultimoAtrapado = Time.time;
        int ganados = 100 * combo + (estabaCongelado ? 50 : 0);
        puntos += ganados;

        ActualizarContadores();
        MostrarAviso("¡Atrapaste " + j.nombreJuguete + "!", new Color(0.6f, 1f, 0.6f));
        string extra = estabaCongelado ? "\n<size=34>¡Congelado! +50</size>" : "";
        MostrarCombo(combo >= 2 ? "¡Combo x" + combo + "!  +" + ganados + extra : "+" + ganados + extra, combo >= 2);
        RevisarFin();
    }

    void MostrarCombo(string texto, bool grande)
    {
        if (textoCombo == null) return;
        if (rutinaCombo != null) StopCoroutine(rutinaCombo);
        rutinaCombo = StartCoroutine(RutinaCombo(texto, grande));
    }

    IEnumerator RutinaCombo(string texto, bool grande)
    {
        textoCombo.text = texto;
        textoCombo.fontSize = grande ? 64 : 48;
        Color c = grande ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.9f, 0.4f);
        RectTransform rt = textoCombo.rectTransform;
        Vector2 basePos = new Vector2(0f, -120f);
        for (float t = 0f; t < 1.3f; t += Time.unscaledDeltaTime)
        {
            float a = t < 0.9f ? 1f : 1f - (t - 0.9f) / 0.4f;
            textoCombo.color = new Color(c.r, c.g, c.b, a);
            rt.anchoredPosition = basePos + new Vector2(0f, t * 40f);
            rt.localScale = Vector3.one * (1f + Mathf.Max(0f, 0.3f - t) * 1.5f);
            yield return null;
        }
        textoCombo.text = "";
    }

    void ActualizarPowerUps()
    {
        if (jugador == null || slotsPowerUp == null || slotsPowerUp.Length < 4) return;
        bool[] activo =
        {
            jugador.BoostActivo, jugador.Bolas > 0, jugador.ImanActivo, jugador.TieneEscudo
        };
        string[] texto =
        {
            jugador.BoostActivo ? Mathf.CeilToInt(jugador.BoostRestante) + "s" : "",
            "x" + jugador.Bolas,
            jugador.ImanActivo ? Mathf.CeilToInt(jugador.ImanRestante) + "s" : "",
            jugador.TieneEscudo ? "ON" : ""
        };
        for (int i = 0; i < 4; i++)
        {
            if (slotsPowerUp[i] != null) slotsPowerUp[i].alpha = activo[i] ? 1f : 0.3f;
            if (textosPowerUp != null && i < textosPowerUp.Length && textosPowerUp[i] != null) textosPowerUp[i].text = texto[i];
        }
    }

    public void JugueteEscapado(Juguete j)
    {
        escapados++;
        ActualizarContadores();
        CamaraSeguimiento.Sacudir(0.25f);
        MostrarAviso("¡Se escapó " + j.nombreJuguete + "! (" + escapados + "/" + maxEscapes + ")", new Color(1f, 0.4f, 0.35f));

        if (escapados >= maxEscapes)
            GameOver("¡Se escaparon demasiados juguetes!");
        else
            RevisarFin();
    }

    public void AvisoEscape(Juguete j)
    {
        if (!EnJuego) return;
        MostrarAviso("¡" + Capitalizar(j.nombreJuguete) + " va hacia la puerta!", new Color(1f, 0.75f, 0.3f));
        AudioManager.SFX("alerta", 0.7f);
    }

    static string Capitalizar(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    void RevisarFin()
    {
        if (!terminado && total > 0 && atrapados + escapados >= total)
            Completar();
    }

    // ---------------- HUD ----------------

    void ActualizarContadores()
    {
        if (textoAtrapados != null) textoAtrapados.text = atrapados + " / " + total;
        if (textoEscapados != null) textoEscapados.text = escapados + " / " + maxEscapes;
        if (textoPuntos != null) textoPuntos.text = puntos.ToString("N0") + " pts";
    }

    void ActualizarTiempo()
    {
        if (textoTiempo == null) return;
        float r = Mathf.Max(0f, TiempoRestante);
        int m = Mathf.FloorToInt(r / 60f);
        int s = Mathf.FloorToInt(r % 60f);
        textoTiempo.text = m + ":" + s.ToString("00");
        bool urgente = r <= tiempoAvisoFinal;
        textoTiempo.color = urgente ? Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), Mathf.PingPong(Time.time * 3f, 1f)) : Color.white;
    }

    public void ActualizarVidas(int vidas, int maximo)
    {
        if (corazones == null) return;
        for (int i = 0; i < corazones.Length; i++)
        {
            if (corazones[i] == null) continue;
            corazones[i].gameObject.SetActive(i < maximo);
            corazones[i].sprite = i < vidas ? spriteCorazon : spriteCorazonVacio;
        }
        if (vidas < maximo && vidas >= 0 && vidas < corazones.Length && corazones[vidas] != null)
            StartCoroutine(Pop(corazones[vidas].transform));
    }

    public void MostrarAviso(string mensaje, Color color)
    {
        if (textoAviso == null) return;
        if (rutinaAviso != null) StopCoroutine(rutinaAviso);
        rutinaAviso = StartCoroutine(RutinaAviso(mensaje, color));
    }

    IEnumerator RutinaAviso(string mensaje, Color color)
    {
        textoAviso.text = mensaje;
        StartCoroutine(Pop(textoAviso.transform));
        for (float t = 0f; t < 2.6f; t += Time.unscaledDeltaTime)
        {
            float a = t < 2f ? 1f : 1f - (t - 2f) / 0.6f;
            textoAviso.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        textoAviso.text = "";
    }

    IEnumerator Pop(Transform t)
    {
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / 0.25f)
        {
            float s = 1f + Mathf.Sin(x * Mathf.PI) * 0.3f;
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator MostrarPanel(GameObject panel, float espera)
    {
        yield return new WaitForSecondsRealtime(espera);
        if (panel == null) yield break;
        OcultarAvisosHUD();
        panel.SetActive(true);
        UIFoco.Seleccionar(panel);
        PanelResultado res = panel.GetComponent<PanelResultado>();
        Transform caja = res != null && res.caja != null ? res.caja : null;
        if (caja == null) caja = panel.transform.childCount > 0 ? panel.transform.GetChild(0) : panel.transform;
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / 0.3f)
        {
            float s = Mathf.LerpUnclamped(0.6f, 1f, 1f - Mathf.Pow(1f - x, 3f) + Mathf.Sin(x * Mathf.PI) * 0.1f);
            caja.localScale = Vector3.one * s;
            yield return null;
        }
        caja.localScale = Vector3.one;
    }

    // Quita del HUD los textos flotantes para que no se amontonen con el panel final.
    void OcultarAvisosHUD()
    {
        if (textoCombo != null) textoCombo.gameObject.SetActive(false);
        if (textoAviso != null) textoAviso.gameObject.SetActive(false);
        if (textoCentro != null) textoCentro.gameObject.SetActive(false);
        Indicadores ind = FindAnyObjectByType<Indicadores>();
        if (ind != null) ind.gameObject.SetActive(false);
    }

    // ---------------- Fin del nivel ----------------

    void Completar()
    {
        terminado = true;
        enJuego = false;
        if (jugador != null) jugador.Celebrar();

        int numEstrellas = 1;
        if (escapados == 0) numEstrellas++;
        if (TiempoRestante >= tiempoLimite * 0.4f) numEstrellas++;

        GameData.GuardarEstrellas(numeroNivel, numEstrellas);
        if (numeroNivel < GameData.TotalNiveles)
            GameData.NivelDesbloqueado = Mathf.Max(GameData.NivelDesbloqueado, numeroNivel + 1);

        int bonusTiempo = Mathf.CeilToInt(Mathf.Max(0f, TiempoRestante)) * 5;
        int bonusVidas = (jugador != null ? jugador.Vidas : 0) * 150;
        int bonusPerfecto = escapados == 0 ? 500 : 0;
        puntos += bonusTiempo + bonusVidas + bonusPerfecto;
        ActualizarContadores();

        float usado = Mathf.Min(tiempoJuego, tiempoLimite);
        int mejorPrevio = GameData.RecordPuntos(numeroNivel);
        bool recordPuntos = GameData.GuardarPuntos(numeroNivel, puntos);
        bool recordTiempo = GameData.GuardarTiempo(numeroNivel, usado);

        string resumen = "Atrapados: " + atrapados + " / " + total + "   •   Escapados: " + escapados +
                         "\nTiempo: " + GameData.FormatoTiempo(usado) +
                         "\nBonos: tiempo +" + bonusTiempo + ", vidas +" + bonusVidas + (bonusPerfecto > 0 ? ", perfecto +500" : "") +
                         "\n<size=46><color=#FFD24A>" + puntos.ToString("N0") + " pts</color></size>";
        if (recordPuntos || recordTiempo)
            resumen += "\n<color=#7CFF8A>¡Nuevo récord" + (recordPuntos && recordTiempo ? " de puntos y tiempo" : recordPuntos ? " de puntos" : " de tiempo") + "!</color>";

        string[] stats = { atrapados + "/" + total, escapados.ToString(), GameData.FormatoTiempo(usado) };
        int[] bonos = { bonusTiempo, bonusVidas, bonusPerfecto };
        string textoRecord = null;
        if (recordPuntos || recordTiempo)
            textoRecord = "¡NUEVO RÉCORD!\n<size=22>" + (recordPuntos && recordTiempo ? "puntos y tiempo" : recordPuntos ? "de puntos" : "de tiempo") + "</size>";
        PanelResultado pr = numeroNivel >= GameData.TotalNiveles ? resultadoVictoria : resultadoCompletado;
        if (pr != null)
            pr.ConfigurarResultado(numEstrellas, stats, bonos, puntos, mejorPrevio, textoRecord);

        if (numeroNivel >= GameData.TotalNiveles)
        {
            if (textoResumenFinal != null)
                textoResumenFinal.text = "Todos los juguetes volvieron al taller.\nSanta podrá repartir los regalos a tiempo.\n\n" + resumen;
            AudioManager.SFX("victoria");
            StartCoroutine(MostrarPanel(panelVictoriaFinal, 1.8f));
        }
        else
        {
            if (textoResumen != null) textoResumen.text = resumen;
            for (int i = 0; i < estrellas.Length; i++)
                if (estrellas[i] != null) estrellas[i].sprite = spriteEstrellaVacia;
            AudioManager.SFX("nivel_completo");
            StartCoroutine(MostrarPanel(panelCompletado, 1.6f));
            if (resultadoCompletado == null) StartCoroutine(AnimarEstrellas(numEstrellas, 2.1f));
        }
    }

    IEnumerator AnimarEstrellas(int cantidad, float espera)
    {
        yield return new WaitForSecondsRealtime(espera);
        for (int i = 0; i < cantidad && i < estrellas.Length; i++)
        {
            estrellas[i].sprite = spriteEstrella;
            AudioManager.SFX("pop", 1f, 0f);
            StartCoroutine(Pop(estrellas[i].transform));
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    public void GameOver(string motivo)
    {
        if (terminado) return;
        terminado = true;
        enJuego = false;
        if (textoMotivo != null) textoMotivo.text = motivo;
        if (resultadoGameOver != null)
        {
            string[] stats = { atrapados + "/" + total, escapados.ToString(), GameData.FormatoTiempo(Mathf.Min(tiempoJuego, tiempoLimite)) };
            resultadoGameOver.ConfigurarDerrota(motivo, stats, puntos, GameData.RecordPuntos(numeroNivel));
        }
        AudioManager.SFX("derrota");
        StartCoroutine(MostrarPanel(panelGameOver, 1.2f));
    }

    public void JugadorMurio()
    {
        GameOver("¡El Cascanueces te atrapó!");
    }

    // ---------------- Pausa y navegación ----------------

    public void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        AudioManager.SFX("click");
        StartCoroutine(MostrarPanel(panelPausa, 0f));
    }

    public void Reanudar()
    {
        pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        UIFoco.Limpiar();
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void AbrirOpciones()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        StartCoroutine(MostrarPanel(panelOpciones, 0f));
    }

    public void CerrarOpciones()
    {
        if (panelOpciones != null) panelOpciones.SetActive(false);
        StartCoroutine(MostrarPanel(panelPausa, 0f));
    }

    public void Reiniciar()
    {
        AudioListener.pause = false;
        SceneLoader.Cargar(SceneManager.GetActiveScene().name, "Reiniciando nivel...");
    }

    public void SiguienteNivel()
    {
        int n = Mathf.Min(numeroNivel + 1, GameData.TotalNiveles);
        GameData.NivelActual = n;
        SceneLoader.Cargar(GameData.EscenaNivel(n), "Nivel " + n);
    }

    public void IrAlMenu()
    {
        AudioListener.pause = false;
        SceneLoader.Cargar("MenuPrincipal", "Volviendo al taller...");
    }
}
