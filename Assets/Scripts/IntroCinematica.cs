using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cinemática de introducción: tomas de cámara con subtítulos que cuentan la historia.
public class IntroCinematica : MonoBehaviour
{
    [System.Serializable]
    public class Toma
    {
        public Vector3 camaraDesde;
        public Vector3 camaraHasta;
        public Vector3 mirarDesde;
        public Vector3 mirarHasta;
        public float duracion = 4f;
        [TextArea] public string texto;
    }

    public Camera camara;
    public Toma[] tomas;

    [Header("Escena")]
    public Saltarin[] juguetes;
    public Transform destinoJuguetes;
    public Transform cascanueces;
    public Light luzCascanueces;
    public Light[] lucesTaller;
    public ParticleSystem magia;
    public Animator duende;

    [Header("UI")]
    public Text subtitulo;
    public CanvasGroup grupoSubtitulo;
    public CanvasGroup grupoTitulo;
    public CanvasGroup negro;
    public Text textoSaltar;

    private bool saliendo;
    private bool juguetesVivos;
    private float[] intensidadesBase;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.MusicaMenu();

        intensidadesBase = new float[lucesTaller.Length];
        for (int i = 0; i < lucesTaller.Length; i++)
            intensidadesBase[i] = lucesTaller[i].intensity;

        foreach (Saltarin s in juguetes)
            if (s != null) s.velocidadForzada = 0f;

        if (luzCascanueces != null) luzCascanueces.intensity = 0f;
        grupoTitulo.alpha = 0f;
        grupoSubtitulo.alpha = 0f;
        negro.alpha = 1f;

        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (textoSaltar != null)
            textoSaltar.color = new Color(1f, 1f, 1f, 0.45f + Mathf.Sin(Time.time * 3f) * 0.25f);

        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        bool saltar = (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame)) ||
                      (m != null && m.leftButton.wasPressedThisFrame);
        if (saltar)
            Salir();

        if (juguetesVivos && destinoJuguetes != null)
        {
            foreach (Saltarin s in juguetes)
            {
                if (s == null) continue;
                Vector3 objetivo = destinoJuguetes.position;
                objetivo.y = s.transform.position.y;
                Vector3 dir = objetivo - s.transform.position;
                if (dir.magnitude > 0.5f)
                {
                    s.transform.position += dir.normalized * 1.6f * Time.deltaTime;
                    s.transform.rotation = Quaternion.Slerp(s.transform.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 4f);
                    s.velocidadForzada = 2f;
                }
                else s.velocidadForzada = 0f;
            }
        }
    }

    IEnumerator Secuencia()
    {
        yield return Fundido(negro, 1f, 0f, 1.2f);

        for (int i = 0; i < tomas.Length; i++)
        {
            Toma t = tomas[i];
            Evento(i);
            StartCoroutine(MostrarSubtitulo(t.texto, t.duracion));

            for (float x = 0f; x < 1f; x += Time.deltaTime / t.duracion)
            {
                float e = Mathf.SmoothStep(0f, 1f, x);
                camara.transform.position = Vector3.Lerp(t.camaraDesde, t.camaraHasta, e);
                camara.transform.LookAt(Vector3.Lerp(t.mirarDesde, t.mirarHasta, e));
                yield return null;
            }
        }

        // Título final
        grupoSubtitulo.alpha = 0f;
        AudioManager.SFX("victoria");
        yield return Fundido(grupoTitulo, 0f, 1f, 0.8f);
        yield return new WaitForSeconds(2.8f);
        Salir();
    }

    void Evento(int indice)
    {
        switch (indice)
        {
            case 2: // Magia extraña
                StartCoroutine(ParpadeoLuces());
                if (magia != null) magia.Play();
                AudioManager.SFX("magia");
                break;

            case 3: // Los juguetes cobran vida
                juguetesVivos = true;
                AudioManager.SFX("pop");
                break;

            case 4: // El Cascanueces
                StartCoroutine(DespertarCascanueces());
                break;

            case 5: // El duende
                if (duende != null) duende.SetTrigger("Celebrar");
                AudioManager.SFX("go");
                break;
        }
    }

    IEnumerator ParpadeoLuces()
    {
        for (int n = 0; n < 10; n++)
        {
            for (int i = 0; i < lucesTaller.Length; i++)
                lucesTaller[i].intensity = intensidadesBase[i] * (n % 2 == 0 ? 0.15f : 1.2f);
            yield return new WaitForSeconds(Random.Range(0.06f, 0.18f));
        }
        for (int i = 0; i < lucesTaller.Length; i++)
            lucesTaller[i].intensity = intensidadesBase[i] * 0.6f;
    }

    IEnumerator DespertarCascanueces()
    {
        AudioManager.SFX("pisada", 1f, 0f);
        CamaraLigeraSacudida();
        for (float x = 0f; x < 1f; x += Time.deltaTime / 1.2f)
        {
            if (luzCascanueces != null) luzCascanueces.intensity = Mathf.Lerp(0f, 2.2f, x);
            yield return null;
        }
        yield return new WaitForSeconds(0.6f);
        AudioManager.SFX("pisada", 1f, 0f);
        CamaraLigeraSacudida();
    }

    void CamaraLigeraSacudida()
    {
        StartCoroutine(Sacudir());
    }

    IEnumerator Sacudir()
    {
        for (float x = 0f; x < 0.3f; x += Time.deltaTime)
        {
            camara.transform.position += Random.insideUnitSphere * 0.08f;
            yield return null;
        }
    }

    IEnumerator MostrarSubtitulo(string texto, float duracion)
    {
        subtitulo.text = "";
        yield return Fundido(grupoSubtitulo, 0f, 1f, 0.3f);
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
