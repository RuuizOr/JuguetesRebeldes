using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pantalla para elegir el personaje con el que se juega.
public class SeleccionPersonaje : MonoBehaviour
{
    public GameObject[] modelos;
    public Animator[] animadores;
    public Transform plataforma;

    [Header("UI")]
    public Text textoNombre;
    public Text textoTitulo;
    public Text textoDescripcion;
    public Image barraVelocidad;
    public Image barraEnergia;
    public Image barraVidas;
    public Text textoVidas;
    public Image[] puntos;
    public Button botonAnterior;
    public Button botonSiguiente;
    public Button botonElegir;
    public Button botonVolver;
    public Text textoElegido;

    private int indice;
    private Vector3[] escalas;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.MusicaMenu();
        indice = GameData.Personaje;
        escalas = new Vector3[modelos.Length];
        for (int i = 0; i < modelos.Length; i++)
            escalas[i] = modelos[i].transform.localScale;

        botonAnterior.onClick.AddListener(() => Cambiar(-1));
        botonSiguiente.onClick.AddListener(() => Cambiar(1));
        botonElegir.onClick.AddListener(Elegir);
        botonVolver.onClick.AddListener(() => SceneLoader.Cargar("MenuPrincipal", "Juguetes Rebeldes"));

        Mostrar(false);
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k == null || SceneLoader.Cargando) return;
        if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) Cambiar(-1);
        if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) Cambiar(1);
        if (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Elegir();
        if (k.escapeKey.wasPressedThisFrame) SceneLoader.Cargar("MenuPrincipal", "Juguetes Rebeldes");
    }

    void Cambiar(int dir)
    {
        indice = (indice + dir + modelos.Length) % modelos.Length;
        AudioManager.SFX("pop");
        Mostrar(true);
    }

    void Mostrar(bool animar)
    {
        for (int i = 0; i < modelos.Length; i++)
            modelos[i].SetActive(i == indice);

        Personajes.Info info = Personajes.Lista[indice];
        textoNombre.text = info.nombre;
        textoTitulo.text = info.titulo;
        textoDescripcion.text = info.descripcion;
        barraVelocidad.fillAmount = Mathf.InverseLerp(4.5f, 7.2f, info.velocidad);
        barraEnergia.fillAmount = Mathf.InverseLerp(40f, 160f, info.energia);
        barraVidas.fillAmount = info.vidas / 4f;
        textoVidas.text = info.vidas.ToString();

        for (int i = 0; i < puntos.Length; i++)
            puntos[i].color = i == indice ? new Color(1f, 0.82f, 0.3f) : new Color(1f, 1f, 1f, 0.35f);

        bool elegido = indice == GameData.Personaje;
        textoElegido.text = elegido ? "Personaje actual" : "";

        if (animar)
        {
            StopAllCoroutines();
            for (int i = 0; i < modelos.Length; i++)
                modelos[i].transform.localScale = escalas[i];
            StartCoroutine(Aparecer(modelos[indice].transform, escalas[indice]));
        }
    }

    IEnumerator Aparecer(Transform t, Vector3 baseEscala)
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / 0.25f)
        {
            t.localScale = baseEscala * (0.7f + 0.3f * x + Mathf.Sin(x * Mathf.PI) * 0.15f);
            yield return null;
        }
        t.localScale = baseEscala;
    }

    void Elegir()
    {
        GameData.Personaje = indice;
        AudioManager.SFX("nivel_completo");
        if (animadores[indice] != null) animadores[indice].SetTrigger("Celebrar");
        textoElegido.text = "¡" + Personajes.Lista[indice].nombre + " elegido!";
        StartCoroutine(VolverDespues());
    }

    IEnumerator VolverDespues()
    {
        botonElegir.interactable = false;
        yield return new WaitForSeconds(1.3f);
        SceneLoader.Cargar("MenuPrincipal", "Juguetes Rebeldes");
    }
}
