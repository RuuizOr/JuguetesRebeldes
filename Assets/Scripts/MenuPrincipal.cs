using UnityEngine;
using UnityEngine.UI;

// Menú principal: Jugar, Niveles, Cómo jugar, Opciones, Ver intro y Salir.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelPrincipal;
    public GameObject panelNiveles;
    public GameObject panelComoJugar;
    public GameObject panelOpciones;

    [Header("Botones principales")]
    public Button botonJugar;
    public Text textoJugar;
    public Button botonNiveles;
    public Button botonComoJugar;
    public Button botonOpciones;
    public Button botonIntro;
    public Button botonSalir;

    [Header("Volver")]
    public Button botonVolverNiveles;
    public Button botonVolverComoJugar;
    public Button botonVolverOpciones;

    [Header("Niveles")]
    public Button[] botonesNivel;
    public GameObject[] candados;
    public Image[] estrellasNivel; // 3 por nivel
    public Sprite spriteEstrella;
    public Sprite spriteEstrellaVacia;

    [Header("Título")]
    public RectTransform titulo;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioManager.MusicaMenu();

        botonJugar.onClick.AddListener(Jugar);
        botonNiveles.onClick.AddListener(() => Mostrar(panelNiveles));
        botonComoJugar.onClick.AddListener(() => Mostrar(panelComoJugar));
        botonOpciones.onClick.AddListener(() => Mostrar(panelOpciones));
        botonIntro.onClick.AddListener(() => SceneLoader.Cargar("Intro", "Había una vez..."));
        botonSalir.onClick.AddListener(Salir);

        botonVolverNiveles.onClick.AddListener(() => Mostrar(panelPrincipal));
        botonVolverComoJugar.onClick.AddListener(() => Mostrar(panelPrincipal));
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
            textoJugar.text = desbloqueado > 1 ? "Continuar (Nivel " + desbloqueado + ")" : "Jugar";

        Mostrar(panelPrincipal);
    }

    void Update()
    {
        if (titulo != null)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * 2f) * 0.03f;
            titulo.localScale = new Vector3(s, s, 1f);
            titulo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.3f) * 2f);
        }
    }

    void Mostrar(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelNiveles.SetActive(panel == panelNiveles);
        panelComoJugar.SetActive(panel == panelComoJugar);
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
