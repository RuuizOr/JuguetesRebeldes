using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Minimapa: foto del nivel vista desde arriba + puntos para jugador, juguetes, Cascanueces, puertas y power-ups.
public class MiniMapa : MonoBehaviour
{
    public RawImage fondo;
    public RectTransform contenedor;
    public Camera camaraMapa;
    public Vector3 centro;
    public float tamano = 30f;
    public Sprite spritePunto;
    public Sprite spriteJugador;

    private RenderTexture rt;
    private DuendeController jugador;
    private RectTransform marcaJugador;
    private readonly Dictionary<Object, Image> puntos = new Dictionary<Object, Image>();
    private readonly List<Object> borrar = new List<Object>();

    IEnumerator Start()
    {
        jugador = FindAnyObjectByType<DuendeController>();
        marcaJugador = CrearPunto(spriteJugador, new Color(0.4f, 1f, 0.5f), 30f).rectTransform;
        marcaJugador.SetAsLastSibling();

        yield return null;
        RenderizarFondo();
    }

    void RenderizarFondo()
    {
        if (camaraMapa == null || fondo == null) return;

        rt = new RenderTexture(384, 384, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.Create();
        camaraMapa.orthographic = true;
        camaraMapa.orthographicSize = tamano / 2f;
        camaraMapa.aspect = 1f;
        camaraMapa.transform.position = centro + Vector3.up * 40f;
        camaraMapa.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Ocultar actores para que no queden "pintados" en la foto
        List<Renderer> ocultos = new List<Renderer>();
        foreach (Juguete j in JuguetesNivel())
            if (j != null) Ocultar(j.gameObject, ocultos);
        foreach (Cascanueces c in Cascanueces.Todos) Ocultar(c.gameObject, ocultos);
        foreach (PowerUp p in PowerUp.Todos) Ocultar(p.gameObject, ocultos);
        if (jugador != null) Ocultar(jugador.gameObject, ocultos);

        bool niebla = RenderSettings.fog;
        RenderSettings.fog = false;

        RenderPipeline.StandardRequest req = new RenderPipeline.StandardRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(camaraMapa, req))
            RenderPipeline.SubmitRenderRequest(camaraMapa, req);
        else
        {
            camaraMapa.targetTexture = rt;
            camaraMapa.Render();
            camaraMapa.targetTexture = null;
        }

        RenderSettings.fog = niebla;
        foreach (Renderer r in ocultos) if (r != null) r.enabled = true;
        camaraMapa.enabled = false;
        fondo.texture = rt;
    }

    static void Ocultar(GameObject g, List<Renderer> lista)
    {
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            if (r.enabled) { r.enabled = false; lista.Add(r); }
    }

    Image CrearPunto(Sprite s, Color c, float tam)
    {
        GameObject g = new GameObject("Punto", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(contenedor, false);
        Image img = g.GetComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        RectTransform r = img.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(tam, tam);
        return img;
    }

    Vector2 AMapa(Vector3 mundo)
    {
        Vector2 tam = contenedor.rect.size;
        return new Vector2((mundo.x - centro.x) / tamano * tam.x, (mundo.z - centro.z) / tamano * tam.y);
    }

    void Marcar(Object clave, Vector3 pos, Color color, float tam, bool visible = true)
    {
        if (!puntos.TryGetValue(clave, out Image img))
        {
            img = CrearPunto(spritePunto, color, tam);
            puntos[clave] = img;
        }
        img.gameObject.SetActive(visible);
        img.color = color;
        img.rectTransform.sizeDelta = new Vector2(tam, tam);
        img.rectTransform.anchoredPosition = AMapa(pos);
    }

    void LateUpdate()
    {
        borrar.Clear();
        foreach (var kv in puntos)
            if (kv.Key == null) borrar.Add(kv.Key);
        foreach (Object k in borrar)
        {
            if (puntos[k] != null) Destroy(puntos[k].gameObject);
            puntos.Remove(k);
        }

        foreach (PuertaSalida p in PuertaSalida.Todas)
            Marcar(p, p.PuntoEscape, new Color(1f, 1f, 1f, 0.9f), 20f);

        foreach (PowerUp p in PowerUp.Todos)
            Marcar(p, p.transform.position, new Color(1f, 0.55f, 0.85f), 11f, p.Disponible);

        foreach (Juguete j in JuguetesNivel())
        {
            if (j == null) continue;
            if (!j.Activo)
            {
                if (puntos.TryGetValue(j, out Image oculto) && oculto != null) oculto.gameObject.SetActive(false);
                continue;
            }
            Color c = j.Estado == EstadoJuguete.Escapar ? new Color(1f, 0.25f, 0.25f)
                    : j.EstaCongelado ? new Color(0.5f, 0.9f, 1f)
                    : new Color(1f, 0.85f, 0.25f);
            float t = j.Estado == EstadoJuguete.Escapar ? 16f + Mathf.PingPong(Time.time * 20f, 6f) : 14f;
            Marcar(j, j.transform.position, c, t);
        }

        foreach (Cascanueces c in Cascanueces.Todos)
            Marcar(c, c.transform.position, new Color(0.9f, 0.1f, 0.15f), 22f);

        if (jugador != null && marcaJugador != null)
        {
            marcaJugador.anchoredPosition = AMapa(jugador.transform.position);
            marcaJugador.localRotation = Quaternion.Euler(0f, 0f, 90f - jugador.transform.eulerAngles.y);
            marcaJugador.SetAsLastSibling();
        }
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }

    static IReadOnlyList<Juguete> JuguetesNivel()
    {
        if (NivelManager.Instancia != null) return NivelManager.Instancia.Juguetes;
        return new List<Juguete>();
    }
}
