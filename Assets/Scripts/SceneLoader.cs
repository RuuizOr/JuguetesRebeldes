using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla de carga entre escenas (se carga desde Resources/PantallaCarga).
public class SceneLoader : MonoBehaviour
{
    public CanvasGroup grupo;
    public Image barra;
    public Text textoPorcentaje;
    public Text textoTip;
    public Text textoTitulo;
    public RectTransform icono;
    public Image fondoEscena;
    public string[] escenasConFondo;
    public Sprite[] fondos;
    [TextArea] public string[] tips;

    private static SceneLoader instancia;
    private bool cargando;

    static SceneLoader Inst
    {
        get
        {
            if (instancia == null)
            {
                SceneLoader prefab = Resources.Load<SceneLoader>("PantallaCarga");
                Instantiate(prefab).name = "PantallaCarga";
            }
            return instancia;
        }
    }

    public static bool Cargando => instancia != null && instancia.cargando;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
    }

    public static void Cargar(string escena, string titulo = "Cargando...")
    {
        SceneLoader s = Inst;
        if (s.cargando)
            return;
        s.StartCoroutine(s.Rutina(escena, titulo));
    }

    void Update()
    {
        if (icono != null && grupo.alpha > 0f)
            icono.Rotate(0f, 0f, -80f * Time.unscaledDeltaTime);
    }

    IEnumerator Rutina(string escena, string titulo)
    {
        cargando = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        grupo.blocksRaycasts = true;
        barra.fillAmount = 0f;
        textoPorcentaje.text = "0%";
        if (textoTitulo != null) textoTitulo.text = titulo;
        if (fondoEscena != null)
        {
            Sprite s = null;
            for (int i = 0; escenasConFondo != null && i < escenasConFondo.Length; i++)
                if (escenasConFondo[i] == escena && i < fondos.Length) s = fondos[i];
            fondoEscena.sprite = s;
            fondoEscena.enabled = s != null;
        }

        if (tips != null && tips.Length > 0)
            textoTip.text = tips[Random.Range(0, tips.Length)];

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
        {
            grupo.alpha = t;
            yield return null;
        }
        grupo.alpha = 1f;

        AsyncOperation op = SceneManager.LoadSceneAsync(escena);
        op.allowSceneActivation = false;

        const float tiempoMinimo = 2.2f;
        float tiempo = 0f;
        float mostrado = 0f;

        while (true)
        {
            tiempo += Time.unscaledDeltaTime;
            float real = Mathf.Clamp01(op.progress / 0.9f);
            float objetivo = Mathf.Min(real, tiempo / tiempoMinimo);
            mostrado = Mathf.MoveTowards(mostrado, objetivo, Time.unscaledDeltaTime * 1.2f);
            barra.fillAmount = mostrado;
            textoPorcentaje.text = Mathf.RoundToInt(mostrado * 100f) + "%";

            if (mostrado >= 0.999f && real >= 1f)
                break;

            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
            yield return null;

        yield return null;

        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.45f)
        {
            grupo.alpha = t;
            yield return null;
        }

        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        cargando = false;
    }
}
