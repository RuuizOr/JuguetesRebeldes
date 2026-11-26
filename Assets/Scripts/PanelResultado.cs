using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Panel de resultados (nivel completado, derrota o victoria final) con animaciones:
// las secciones aparecen en orden, las estrellas "saltan", los puntos cuentan hacia arriba
// y cae confeti. Usa tiempo sin escala porque el juego se detiene al terminar.
public class PanelResultado : MonoBehaviour
{
    [Header("Cabecera")]
    public RectTransform ajuste;
    public float altoTarjeta = 1080f;
    public float anchoTarjeta = 1120f;
    public RectTransform caja;
    public RectTransform rayos;
    public RectTransform insignia;
    public Text subtitulo;

    [Header("Contenido")]
    public CanvasGroup[] secciones;
    public Image[] estrellas;
    public Sprite estrellaLlena;
    public Sprite estrellaVacia;
    public Text[] valoresStats;
    public Text[] valoresBonus;
    public Text textoTotal;
    public Text textoMejor;
    public RectTransform sello;
    public Text textoSello;
    public Text textoMotivo;
    public Text textoConsejo;

    [Header("Confeti")]
    public RectTransform contenedorConfeti;
    public Sprite spriteConfeti;
    public int cantidadConfeti = 0;

    // Datos a mostrar
    private int numEstrellas;
    private string[] stats;
    private int[] bonus;
    private int puntosBase;
    private int puntosTotal;
    private int mejor;
    private string record;
    private bool listo;

    private readonly List<RectTransform> confeti = new List<RectTransform>();
    private readonly List<Vector3> velConfeti = new List<Vector3>();

    static readonly string[] Consejos =
    {
        "Lanza bolas de nieve (E / clic / X) para congelar a los juguetes que huyen.",
        "Corre con Shift o RT, pero vigila tu barra de energía.",
        "El escudo aguanta un golpe completo del Cascanueces.",
        "Las flechas en el borde de la pantalla señalan a los juguetes que escapan.",
        "Un juguete cansado se mueve lento: ¡es tu oportunidad!",
        "Revisa el minimapa: los puntos rojos son juguetes que van hacia la puerta.",
        "El imán atrae a los juguetes cercanos: úsalo cuando haya varios juntos."
    };

    public void ConfigurarResultado(int estrellasGanadas, string[] valoresStat, int[] valoresBono, int total, int mejorPuntaje, string textoRecord, string sub = null)
    {
        numEstrellas = estrellasGanadas;
        stats = valoresStat;
        bonus = valoresBono;
        puntosTotal = total;
        puntosBase = total;
        if (bonus != null) foreach (int b in bonus) puntosBase -= b;
        mejor = mejorPuntaje;
        record = textoRecord;
        if (sub != null && subtitulo != null) subtitulo.text = sub;
        listo = true;
        Reiniciar();
    }

    public void ConfigurarDerrota(string motivo, string[] valoresStat, int total, int mejorPuntaje)
    {
        stats = valoresStat;
        bonus = null;
        puntosBase = 0;
        puntosTotal = total;
        mejor = mejorPuntaje;
        record = null;
        numEstrellas = 0;
        if (textoMotivo != null) textoMotivo.text = motivo;
        if (textoConsejo != null) textoConsejo.text = Consejos[Random.Range(0, Consejos.Length)];
        listo = true;
        Reiniciar();
    }

    void Reiniciar()
    {
        if (secciones != null) foreach (CanvasGroup g in secciones) if (g != null) g.alpha = 0f;
        if (estrellas != null) foreach (Image e in estrellas) if (e != null) { e.sprite = estrellaVacia; e.transform.localScale = Vector3.one; }
        if (sello != null) sello.gameObject.SetActive(false);
        if (textoTotal != null) textoTotal.text = "0";
        if (valoresStats != null && stats != null)
            for (int i = 0; i < valoresStats.Length && i < stats.Length; i++) valoresStats[i].text = stats[i];
        if (valoresBonus != null)
            for (int i = 0; i < valoresBonus.Length; i++) { valoresBonus[i].text = ""; }
        if (textoMejor != null) textoMejor.text = "";
    }

    void OnEnable()
    {
        if (!listo) return;
        StopAllCoroutines();
        StartCoroutine(Animar());
        CrearConfeti();
    }

    IEnumerator Esperar(float s)
    {
        yield return new WaitForSecondsRealtime(s);
    }

    IEnumerator Aparecer(CanvasGroup g, float dur = 0.25f)
    {
        if (g == null) yield break;
        RectTransform rt = g.transform as RectTransform;
        Vector2 baseP = rt.anchoredPosition;
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / dur)
        {
            float k = 1f - Mathf.Pow(1f - x, 3f);
            g.alpha = k;
            rt.anchoredPosition = baseP + new Vector2(0f, -25f * (1f - k));
            yield return null;
        }
        g.alpha = 1f;
        rt.anchoredPosition = baseP;
    }

    IEnumerator Pop(Transform t, float fuerza = 0.35f, float dur = 0.3f)
    {
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / dur)
        {
            t.localScale = Vector3.one * (1f + Mathf.Sin(x * Mathf.PI) * fuerza);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator Contar(Text t, int desde, int hasta, float dur, string prefijo = "", string sufijo = "")
    {
        if (t == null) yield break;
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / dur)
        {
            int v = Mathf.RoundToInt(Mathf.Lerp(desde, hasta, 1f - Mathf.Pow(1f - x, 2f)));
            t.text = prefijo + v.ToString("N0") + sufijo;
            yield return null;
        }
        t.text = prefijo + hasta.ToString("N0") + sufijo;
    }

    IEnumerator Animar()
    {
        yield return Esperar(0.25f);
        if (insignia != null) StartCoroutine(Pop(insignia, 0.25f, 0.4f));

        int s = 0;
        if (secciones == null) yield break;
        for (; s < secciones.Length; s++)
        {
            CanvasGroup g = secciones[s];
            if (g == null) continue;
            yield return Aparecer(g);

            switch (g.name)
            {
                case "Estrellas":
                    for (int i = 0; i < numEstrellas && i < estrellas.Length; i++)
                    {
                        estrellas[i].sprite = estrellaLlena;
                        AudioManager.SFX("pop", 1f, 0f);
                        StartCoroutine(Pop(estrellas[i].transform, 0.45f, 0.3f));
                        yield return Esperar(0.28f);
                    }
                    break;
                case "Bonos":
                    if (bonus != null)
                        for (int i = 0; i < valoresBonus.Length && i < bonus.Length; i++)
                        {
                            if (bonus[i] > 0)
                            {
                                yield return Contar(valoresBonus[i], 0, bonus[i], 0.35f, "+");
                                AudioManager.SFX("click", 0.8f, 0.1f);
                            }
                            else
                                valoresBonus[i].text = "<color=#6F7892>—</color>";
                            yield return Esperar(0.08f);
                        }
                    break;
                case "Total":
                    yield return Contar(textoTotal, puntosBase > 0 ? puntosBase : 0, puntosTotal, 0.8f);
                    StartCoroutine(Pop(textoTotal.transform, 0.15f, 0.25f));
                    if (textoMejor != null)
                        textoMejor.text = "Mejor del nivel: " + Mathf.Max(mejor, puntosTotal).ToString("N0");
                    if (!string.IsNullOrEmpty(record) && sello != null)
                    {
                        yield return Esperar(0.15f);
                        sello.gameObject.SetActive(true);
                        if (textoSello != null) textoSello.text = record;
                        AudioManager.SFX("powerup", 1f, 0f);
                        StartCoroutine(Sellar(sello));
                    }
                    break;
            }
            yield return Esperar(0.05f);
        }
    }

    IEnumerator Sellar(RectTransform t)
    {
        for (float x = 0f; x < 1f; x += Time.unscaledDeltaTime / 0.25f)
        {
            t.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - Mathf.Pow(1f - x, 3f));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void CrearConfeti()
    {
        if (contenedorConfeti == null || cantidadConfeti <= 0 || spriteConfeti == null) return;
        foreach (RectTransform c in confeti) if (c != null) Destroy(c.gameObject);
        confeti.Clear();
        velConfeti.Clear();
        Color[] colores = { new Color(1f, 0.82f, 0.25f), new Color(0.95f, 0.3f, 0.3f), new Color(0.35f, 0.85f, 0.45f), new Color(0.4f, 0.75f, 1f), Color.white };
        Rect r = contenedorConfeti.rect;
        for (int i = 0; i < cantidadConfeti; i++)
        {
            GameObject g = new GameObject("Confeti", typeof(RectTransform), typeof(Image));
            RectTransform rt = g.GetComponent<RectTransform>();
            rt.SetParent(contenedorConfeti, false);
            rt.sizeDelta = new Vector2(Random.Range(10f, 16f), Random.Range(16f, 26f));
            rt.anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + Random.Range(20f, 700f));
            rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            Image im = g.GetComponent<Image>();
            im.sprite = spriteConfeti;
            im.color = colores[Random.Range(0, colores.Length)];
            im.raycastTarget = false;
            confeti.Add(rt);
            velConfeti.Add(new Vector3(Random.Range(-40f, 40f), Random.Range(-260f, -160f), Random.Range(-260f, 260f)));
        }
    }

    void Update()
    {
        // Escala la tarjeta para que siempre quepa en pantalla
        if (ajuste != null)
        {
            Rect pr = ((RectTransform)transform).rect;
            float k = Mathf.Min(1f, pr.height / altoTarjeta, pr.width / anchoTarjeta);
            ajuste.localScale = new Vector3(k, k, 1f);
        }
        float dt = Time.unscaledDeltaTime;
        if (rayos != null) rayos.Rotate(0f, 0f, -12f * dt);
        if (sello != null && sello.gameObject.activeSelf)
        {
            float k = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.04f;
            sello.localScale = new Vector3(k, k, 1f);
        }
        if (contenedorConfeti == null || confeti.Count == 0) return;
        Rect r = contenedorConfeti.rect;
        for (int i = 0; i < confeti.Count; i++)
        {
            RectTransform c = confeti[i];
            if (c == null) continue;
            Vector3 v = velConfeti[i];
            Vector2 p = c.anchoredPosition + new Vector2(v.x + Mathf.Sin(Time.unscaledTime * 2f + i) * 40f, v.y) * dt;
            if (p.y < r.yMin - 40f) p = new Vector2(Random.Range(r.xMin, r.xMax), r.yMax + 30f);
            c.anchoredPosition = p;
            c.Rotate(0f, 0f, v.z * dt);
            float flip = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f + i));
            c.localScale = new Vector3(0.3f + 0.7f * flip, 1f, 1f);
        }
    }
}
