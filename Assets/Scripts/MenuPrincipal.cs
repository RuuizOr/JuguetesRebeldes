using UnityEngine;
using UnityEngine.UI;

// Menú principal: Jugar, Personaje, Niveles, Opciones, Ver intro y Salir.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelNiveles;
    public GameObject panelOpciones;

    [Header("Botones principales")]
    public Button botonJugar;
    public Text textoJugar;
    public Button botonPersonaje;
    public Button botonNiveles;
    public Button botonOpciones;
    public Button botonIntro;
    public Button botonSalir;
    public Text textoPersonaje;

    [Header("Volver")]
    public Button botonVolverNiveles;
    public Button botonVolverOpciones;

    [Header("Niveles")]
    public Button[] botonesNivel;
    public GameObject[] candados;
    public Image[] estrellasNivel; // 3 por nivel
    public Sprite spriteEstrella;
    public Sprite spriteEstrellaVacia;

    [Header("Escena")]
    public RectTransform titulo;
    public GameObject[] personajesDiorama;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioManager.MusicaMenu();

        int pj = GameData.Personaje;
        if (personajesDiorama != null)
            for (int i = 0; i < personajesDiorama.Length; i++)
                if (personajesDiorama[i] != null) personajesDiorama[i].SetActive(i == pj);
        if (textoPersonaje != null)
            textoPersonaje.text = "Jugando como: <color=#FFD24A>" + Personajes.Lista[pj].nombre + "</color>";

        botonJugar.onClick.AddListener(Jugar);
        botonPersonaje.onClick.AddListener(() => SceneLoader.Cargar("SeleccionPersonaje", "Elige tu personaje"));
        botonNiveles.onClick.AddListener(() => Mostrar(panelNiveles));
        botonOpciones.onClick.AddListener(() => Mostrar(panelOpciones));
        botonIntro.onClick.AddListener(() => SceneLoader.Cargar("Intro", "Había una vez..."));
        botonSalir.onClick.AddListener(Salir);

        botonVolverNiveles.onClick.AddListener(() => Mostrar(panelPrincipal));
        botonVolverOpciones.onClick.AddListener(() => Mostrar(panelPrincipal));

        int desbloqueado = GameData.NivelDesbloqueado;
        for (int i = 0; i < botonesNivel.Length; i++)
        {
            int nivel = i + 1;
            bool abierto = nivel <= desbloqueado;
            botonesNivel[i].interactable = abierto;
            botonesNivel[i].onClick.AddListener(() => CargarNivel(nivel));
            if (candados != null && i < candados.Length && candados[i] != null)
                candados[i].SetActive(!abierto);

            int e = GameData.Estrellas(nivel);
            for (int s = 0; s < 3; s++)
            {
                int idx = i * 3 + s;
                if (estrellasNivel != null && idx < estrellasNivel.Length && estrellasNivel[idx] != null)
                    estrellasNivel[idx].sprite = s < e ? spriteEstrella : spriteEstrellaVacia;
            }
        }

        if (textoJugar != null)
            textoJugar.text = desbloqueado > 1 ? "CONTINUAR  <size=34>(Nivel " + desbloqueado + ")</size>" : "JUGAR";

        Mostrar(panelPrincipal);
    }

    void Update()
    {
        if (titulo != null)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.03f;
            titulo.localScale = new Vector3(s, s, 1f);
            titulo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.3f) * 1.5f);
        }
    }

    void Mostrar(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelNiveles.SetActive(panel == panelNiveles);
        panelOpciones.SetActive(panel == panelOpciones);
    }

    void Jugar()
    {
        CargarNivel(GameData.NivelDesbloqueado);
    }

    void CargarNivel(int nivel)
    {
        GameData.NivelActual = nivel;
        SceneLoader.Cargar(GameData.EscenaNivel(nivel), "Nivel " + nivel);
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
