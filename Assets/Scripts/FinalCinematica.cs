using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Escena final: Santa sale en su trineo y entrega los regalos casa por casa.
public class FinalCinematica : MonoBehaviour
{
    public Camera camara;
    public Transform trineo;
    public Transform[] ruta;
    public Saltarin[] celebrantes;
    public GameObject[] personajes;
    public Animator[] animadoresPersonajes;
    public GameObject[] modelosRegalo;
    public ParticleSystem estela;

    [Header("UI")]
    public Text subtitulo;
    public CanvasGroup grupoSubtitulo;
    public CanvasGroup grupoTitulo;
    public CanvasGroup negro;
    public Text textoSaltar;
    public Text textoPuntos;

    private bool saliendo;
    private bool puedeSalir;
    private bool saltando;
    private float progresoRuta;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.MusicaMenu();

        int pj = Mathf.Clamp(GameData.Personaje, 0, personajes.Length - 1);
        for (int i = 0; i < personajes.Length; i++)
            personajes[i].SetActive(i == pj);

        int total = 0;
        for (int n = 1; n <= GameData.TotalNiveles; n++) total += GameData.RecordPuntos(n);
        if (textoPuntos != null)
            textoPuntos.text = "Puntuación total (récords): " + total.ToString("N0");

        grupoTitulo.alpha = 0f;
        grupoSubtitulo.alpha = 0f;
        negro.alpha = 1f;
        if (estela != null) estela.Stop();
        ColocarTrineo(0f);
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (textoSaltar != null)
            textoSaltar.color = new Color(1f, 1f, 1f, 0.45f + Mathf.Sin(Time.time * 3f) * 0.25f);

        if (Entrada.Saltear())
        {
            if (puedeSalir) Salir();
            else if (!saltando)
            {
                saltando = true;
                StopAllCoroutines();
                negro.alpha = 0f;
                grupoSubtitulo.alpha = 0f;
                ColocarTrineo(1f);
                camara.transform.position = new Vector3(4f, 3f, 120f);
                camara.transform.LookAt(trineo.position);
                StartCoroutine(MostrarTitulo());
            }
        }
    }

    IEnumerator Secuencia()
    {
        StartCoroutine(Fundido(negro, 1f, 0f, 1.2f));

        // 1. Celebración
        foreach (Saltarin s in celebrantes) if (s != null) s.velocidadForzada = 1.6f;
        int pj = Mathf.Clamp(GameData.Personaje, 0, animadoresPersonajes.Length - 1);
        if (animadoresPersonajes[pj] != null) animadoresPersonajes[pj].SetTrigger("Celebrar");
        AudioManager.SFX("victoria");
        yield return Toma(new Vector3(-4f, 2f, -9f), new Vector3(3f, 2.2f, -8.5f), new Vector3(0f, 1f, -4f), new Vector3(0f, 1f, -4f), 5.5f,
            "¡Lo lograste! Todos los juguetes volvieron al taller.");

        // 2. Santa en el trineo
        yield return Toma(new Vector3(-5f, 2f, 1f), new Vector3(-4f, 2.4f, 3.5f), trineo.position + Vector3.up, trineo.position + Vector3.up * 1.2f + Vector3.forward, 4.5f,
            "Santa subió a su trineo con todos los regalos...");

        // 3. Despegue
        if (estela != null) estela.Play();
        AudioManager.SFX("magia");
        StartCoroutine(MostrarSubtitulo("¡Y despegó hacia el cielo de Nochebuena!", 5.5f));
        yield return Seguir(0f, 0.35f, 5.5f, new Vector3(-6f, 2.5f, -7f));

        // 4. Entrega de regalos
        StartCoroutine(MostrarSubtitulo("Casa por casa, cada regalo llegó a su destino.", 7f));
        StartCoroutine(SoltarRegalos(6.5f));
        yield return Seguir(0.35f, 0.75f, 7f, new Vector3(9f, 6f, -6f));

        // 5. La luna
        StartCoroutine(MostrarSubtitulo("¡Feliz Navidad!", 4f));
        Vector3 camFija = new Vector3(4f, 3f, 120f);
        float inicio = progresoRuta;
        for (float x = 0f; x < 1f; x += Time.deltaTime / 6f)
        {
            ColocarTrineo(Mathf.Lerp(inicio, 1f, x));
            camara.transform.position = camFija;
            camara.transform.LookAt(trineo.position);
            yield return null;
        }

        yield return MostrarTitulo();
    }

    IEnumerator MostrarTitulo()
    {
        grupoSubtitulo.alpha = 0f;
        AudioManager.SFX("nivel_completo");
        yield return Fundido(grupoTitulo, 0f, 1f, 1f);
        puedeSalir = true;
    }

    IEnumerator Toma(Vector3 desde, Vector3 hasta, Vector3 mirarDesde, Vector3 mirarHasta, float duracion, string texto)
    {
        StartCoroutine(MostrarSubtitulo(texto, duracion));
        for (float x = 0f; x < 1f; x += Time.deltaTime / duracion)
        {
            float e = Mathf.SmoothStep(0f, 1f, x);
            camara.transform.position = Vector3.Lerp(desde, hasta, e);
            camara.transform.LookAt(Vector3.Lerp(mirarDesde, mirarHasta, e));
            yield return null;
        }
    }

    IEnumerator Seguir(float desde, float hasta, float duracion, Vector3 offset)
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / duracion)
        {
            ColocarTrineo(Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, x)));
            Vector3 deseada = trineo.position + offset;
            camara.transform.position = Vector3.Lerp(camara.transform.position, deseada, 1f - Mathf.Exp(-3f * Time.deltaTime));
            camara.transform.LookAt(trineo.position + Vector3.up);
            yield return null;
        }
    }

    void ColocarTrineo(float t)
    {
        progresoRuta = t;
        if (ruta == null || ruta.Length < 2) return;
        float f = Mathf.Clamp01(t) * (ruta.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(f), ruta.Length - 2);
        float k = f - i;
        Vector3 a = ruta[i].position, b = ruta[i + 1].position;
        Vector3 p = Vector3.Lerp(a, b, k);
        trineo.position = p;
        Vector3 dir = b - a;
        if (dir.sqrMagnitude > 0.001f)
            trineo.rotation = Quaternion.Slerp(trineo.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 4f + 0.02f);
        trineo.position += Vector3.up * Mathf.Sin(Time.time * 3f) * 0.15f * Mathf.Clamp01(t * 10f);
    }

    IEnumerator SoltarRegalos(float duracion)
    {
        float fin = Time.time + duracion;
        while (Time.time < fin)
        {
            if (modelosRegalo != null && modelosRegalo.Length > 0)
            {
                GameObject r = Instantiate(modelosRegalo[Random.Range(0, modelosRegalo.Length)], trineo.position - trineo.forward * 1.5f, Random.rotation);
                r.transform.localScale *= 0.8f;
                Rigidbody rb = r.AddComponent<Rigidbody>();
                rb.linearVelocity = -trineo.forward * 2f + Random.insideUnitSphere;
                rb.angularVelocity = Random.insideUnitSphere * 4f;
                Destroy(r, 4f);
                AudioManager.SFX("pop", 0.4f, 0.3f);
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    IEnumerator MostrarSubtitulo(string texto, float duracion)
    {
        subtitulo.text = "";
        yield return Fundido(grupoSubtitulo, grupoSubtitulo.alpha, 1f, 0.3f);
        float porLetra = Mathf.Min(0.04f, (duracion * 0.6f) / Mathf.Max(1, texto.Length));
        for (int i = 1; i <= texto.Length; i++)
        {
            subtitulo.text = texto.Substring(0, i);
            yield return new WaitForSeconds(porLetra);
        }
        yield return new WaitForSeconds(Mathf.Max(0.2f, duracion - texto.Length * porLetra - 0.7f));
        yield return Fundido(grupoSubtitulo, 1f, 0f, 0.35f);
    }

    IEnumerator Fundido(CanvasGroup g, float desde, float hasta, float duracion)
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / duracion)
        {
            g.alpha = Mathf.Lerp(desde, hasta, x);
            yield return null;
        }
        g.alpha = hasta;
    }

    void Salir()
    {
        if (saliendo) return;
        saliendo = true;
        SceneLoader.Cargar("MenuPrincipal", "Juguetes Rebeldes");
    }
}
