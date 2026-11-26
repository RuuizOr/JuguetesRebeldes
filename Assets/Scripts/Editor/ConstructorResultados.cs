using UnityEngine;
using UnityEngine.UI;

// Paneles de fin de nivel: Nivel completado, ¡Oh no! (derrota) y ¡Salvaste la Navidad! (victoria final).
public static partial class ConstructorJuego
{
    enum TipoResultado { Completado, Victoria, Derrota }

    static readonly Color FondoTarjeta = new Color(0.07f, 0.09f, 0.18f, 0.98f);
    static readonly Color TextoSuave = new Color(0.68f, 0.76f, 0.92f);
    static readonly Color Dorado = new Color(1f, 0.83f, 0.3f);

    // Coloca un elemento por su centro, medido desde el borde superior de la tarjeta.
    static RectTransform Centro(RectTransform rt, float x, float yDesdeArriba, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, -yDesdeArriba);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Text TextoC(string nombre, Transform padre, string contenido, int tam, Color color, TextAnchor al, float x, float y, float w, float h, bool negrita = true)
    {
        Text t = Texto(nombre, padre, contenido, tam, color, al, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(w, h), negrita);
        Centro(t.rectTransform, x, y, w, h);
        return t;
    }

    static Image ImagenC(string nombre, Transform padre, Sprite sp, Color color, float x, float y, float w, float h, bool sliced = false)
    {
        Image i = Imagen(nombre, padre, sp, color, sliced);
        Centro(i.rectTransform, x, y, w, h);
        return i;
    }

    static CanvasGroup Seccion(string nombre, Transform padre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rt = Rect(g, padre);
        Estirar(rt);
        return g.GetComponent<CanvasGroup>();
    }

    static PanelResultado CrearPanelResultado(Transform t, string nombre, TipoResultado tipo, string titulo, string subtitulo,
        out GameObject raiz, out Button boton1, out Button boton2, string txt1, string ico1, Color col1, string txt2, string ico2, Color col2)
    {
        bool derrota = tipo == TipoResultado.Derrota;
        bool victoria = tipo == TipoResultado.Victoria;
        Color acento = derrota ? new Color(1f, 0.38f, 0.38f) : Dorado;
        Color colorCinta = derrota ? new Color(0.62f, 0.14f, 0.18f) : victoria ? new Color(0.13f, 0.52f, 0.3f) : new Color(0.82f, 0.2f, 0.22f);
        float W = derrota ? 920f : 1040f;
        float H = derrota ? 900f : 940f;

        // Fondo oscuro + viñeta
        Image fondo = Imagen(nombre, t, null, derrota ? new Color(0.12f, 0f, 0.02f, 0.55f) : new Color(0f, 0.02f, 0.06f, 0.55f));
        fondo.raycastTarget = true;
        Estirar(fondo.rectTransform);
        raiz = fondo.gameObject;
        PanelResultado pr = raiz.AddComponent<PanelResultado>();

        // Caja (primer hijo: NivelManager la anima al aparecer)
        GameObject ajusteGo = new GameObject("Ajuste", typeof(RectTransform));
        RectTransform ajuste = Rect(ajusteGo, fondo.transform);
        Estirar(ajuste);
        pr.ajuste = ajuste;
        pr.altoTarjeta = H + 140f;
        pr.anchoTarjeta = W + 80f;
        GameObject cajaGo = new GameObject("Caja", typeof(RectTransform));
        RectTransform caja = Rect(cajaGo, ajuste);
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0.5f, 0.5f);
        caja.anchoredPosition = new Vector2(0f, -40f);
        caja.sizeDelta = new Vector2(W, H);
        pr.caja = caja;

        Image vin = Imagen("Vineta", fondo.transform, SpriteUI("vineta"), new Color(1f, 1f, 1f, 0.85f));
        Estirar(vin.rectTransform);
        vin.transform.SetAsFirstSibling();

        // Rayos y brillo detrás de la tarjeta
        if (!derrota)
        {
            Image rayos = ImagenC("Rayos", caja, SpriteUI("rayos"), new Color(acento.r, acento.g, acento.b, 0.32f), 0f, 0f, 1100f, 1100f);
            pr.rayos = rayos.rectTransform;
        }
        ImagenC("Brillo", caja, SpriteUI("brillo"), new Color(acento.r, acento.g, acento.b, derrota ? 0.25f : 0.45f), 0f, 0f, 700f, 520f);

        // Sombra, tarjeta, degradado, borde
        Image sombra = Imagen("Sombra", caja, SpriteRedondeado, new Color(0f, 0f, 0f, 0.55f), true);
        Estirar(sombra.rectTransform);
        sombra.rectTransform.offsetMin = new Vector2(-8f, -26f);
        sombra.rectTransform.offsetMax = new Vector2(8f, -6f);
        Image tarjeta = Imagen("Tarjeta", caja, SpriteRedondeado, FondoTarjeta, true);
        Estirar(tarjeta.rectTransform);
        Image grad = Imagen("Degradado", caja, SpriteUI("degradado_v"), new Color(acento.r, acento.g, acento.b, 0.2f));
        grad.rectTransform.anchorMin = new Vector2(0f, 1f);
        grad.rectTransform.anchorMax = new Vector2(1f, 1f);
        grad.rectTransform.pivot = new Vector2(0.5f, 1f);
        grad.rectTransform.offsetMin = new Vector2(8f, -340f);
        grad.rectTransform.offsetMax = new Vector2(-8f, -8f);
        Image borde = Imagen("Borde", caja, SpriteRedondeado, new Color(acento.r, acento.g, acento.b, 0.75f), true);
        Estirar(borde.rectTransform);
        borde.rectTransform.offsetMin = new Vector2(7f, 7f);
        borde.rectTransform.offsetMax = new Vector2(-7f, -7f);
        borde.fillCenter = false;

        // Copos decorativos en las esquinas
        Sprite copo = SpriteUI("copo");
        float[,] esquinas = { { -W / 2 + 70f, H - 80f, 20f }, { W / 2 - 70f, H - 80f, -15f }, { -W / 2 + 60f, 260f, 35f }, { W / 2 - 60f, 240f, 10f } };
        for (int i = 0; i < 4; i++)
        {
            Image c = ImagenC("Copo", caja, copo, new Color(1f, 1f, 1f, 0.07f), esquinas[i, 0], esquinas[i, 1], 110f - i * 15f, 110f - i * 15f);
            c.rectTransform.localRotation = Quaternion.Euler(0f, 0f, esquinas[i, 2]);
        }

        // Cinta con el título
        ImagenC("Cinta", caja, SpriteUI("cinta"), colorCinta, 0f, 125f, 860f, 168f);
        TextoC("Titulo", caja, titulo, derrota ? 66 : 56, Color.white, TextAnchor.MiddleCenter, 0f, 112f, 700f, 100f)
            .GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        pr.subtitulo = TextoC("Subtitulo", caja, subtitulo, 30, TextoSuave, TextAnchor.MiddleCenter, 0f, 205f, W - 120f, 44f, false);

        // Insignia (encima de la tarjeta)
        Image ins = ImagenC("Insignia", caja, SpriteUI("insignia"), acento, 0f, -8f, 164f, 164f);
        string icono = derrota ? "corazon_roto" : victoria ? "regalo" : "estrella";
        ImagenC("Icono", ins.transform, SpriteUI(icono), Color.white, 0f, 82f, 96f, 96f);
        pr.insignia = ins.rectTransform;

        System.Collections.Generic.List<CanvasGroup> secciones = new System.Collections.Generic.List<CanvasGroup>();
        float yStats;

        if (!derrota)
        {
            // Estrellas
            CanvasGroup se = Seccion("Estrellas", caja);
            secciones.Add(se);
            ImagenC("Brillo", se.transform, SpriteUI("brillo"), new Color(1f, 0.85f, 0.3f, 0.25f), 0f, 300f, 560f, 200f);
            pr.estrellas = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float lado = i == 1 ? 150f : 118f;
                Image e = ImagenC("Estrella" + (i + 1), se.transform, SpriteUI("estrella_vacia"), Color.white, (i - 1) * 175f, i == 1 ? 292f : 312f, lado, lado);
                e.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (1 - i) * 10f);
                pr.estrellas[i] = e;
            }
            pr.estrellaLlena = SpriteUI("estrella");
            pr.estrellaVacia = SpriteUI("estrella_vacia");
            yStats = 448f;
        }
        else
        {
            CanvasGroup sm = Seccion("Motivo", caja);
            secciones.Add(sm);
            pr.textoMotivo = TextoC("TextoMotivo", sm.transform, "", 40, Color.white, TextAnchor.MiddleCenter, 0f, 290f, W - 100f, 110f);
            yStats = 425f;
        }

        // Tarjetitas de estadísticas
        CanvasGroup ss = Seccion("Stats", caja);
        secciones.Add(ss);
        string[] etiquetas = { "Atrapados", "Escapados", "Tiempo" };
        string[] iconos = { "regalo", "puerta", "reloj" };
        float anchoT = derrota ? 270f : 300f;
        float sep = derrota ? 290f : 320f;
        pr.valoresStats = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * sep;
            ImagenC("Tile" + i, ss.transform, SpriteRedondeado, new Color(1f, 1f, 1f, 0.07f), x, yStats, anchoT, 124f, true);
            ImagenC("Icono" + i, ss.transform, SpriteUI(iconos[i]), Color.white, x - anchoT / 2f + 58f, yStats, 66f, 66f);
            pr.valoresStats[i] = TextoC("Valor" + i, ss.transform, "0", 46, Color.white, TextAnchor.MiddleLeft, x + 45f, yStats - 16f, anchoT - 120f, 56f);
            TextoC("Etiqueta" + i, ss.transform, etiquetas[i], 22, TextoSuave, TextAnchor.MiddleLeft, x + 45f, yStats + 30f, anchoT - 120f, 30f, false);
        }

        if (!derrota)
        {
            // Bonos
            CanvasGroup sb = Seccion("Bonos", caja);
            secciones.Add(sb);
            ImagenC("FondoBonos", sb.transform, SpriteRedondeado, new Color(1f, 1f, 1f, 0.05f), 0f, 600f, 900f, 186f, true);
            string[] nombres = { "Tiempo restante", "Vidas restantes", "Sin escapes" };
            string[] ic = { "reloj", "corazon", "estrella" };
            pr.valoresBonus = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 545f + i * 56f;
                ImagenC("IconoBono" + i, sb.transform, SpriteUI(ic[i]), Color.white, -395f, y, 38f, 38f);
                TextoC("Bono" + i, sb.transform, nombres[i], 30, new Color(0.88f, 0.92f, 1f), TextAnchor.MiddleLeft, -125f, y, 460f, 50f, false);
                pr.valoresBonus[i] = TextoC("ValorBono" + i, sb.transform, "", 34, new Color(0.5f, 1f, 0.6f), TextAnchor.MiddleRight, 280f, y, 300f, 50f);
                if (i < 2) ImagenC("Linea" + i, sb.transform, null, new Color(1f, 1f, 1f, 0.08f), 0f, y + 28f, 840f, 2f);
            }
        }

        // Total
        CanvasGroup st = Seccion("Total", caja);
        secciones.Add(st);
        float yT = derrota ? 530f : 700f;
        TextoC("Etiqueta", st.transform, derrota ? "PUNTOS" : "PUNTUACIÓN TOTAL", 24, TextoSuave, TextAnchor.MiddleCenter, 0f, yT, 500f, 34f);
        pr.textoTotal = TextoC("Valor", st.transform, "0", derrota ? 62 : 78, Dorado, TextAnchor.MiddleCenter, 0f, yT + 50f, 600f, 90f);
        pr.textoTotal.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        pr.textoMejor = TextoC("Mejor", st.transform, "", 22, new Color(0.6f, 0.66f, 0.8f), TextAnchor.MiddleCenter, 0f, yT + 100f, 500f, 30f, false);
        if (!derrota)
        {
            Image sello = ImagenC("Sello", st.transform, SpriteRedondeado, new Color(0.2f, 0.7f, 0.35f), 330f, yT + 40f, 250f, 92f, true);
            sello.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            Image selloBorde = Imagen("Borde", sello.transform, SpriteRedondeado, new Color(1f, 1f, 1f, 0.8f), true);
            Estirar(selloBorde.rectTransform);
            selloBorde.rectTransform.offsetMin = new Vector2(5f, 5f);
            selloBorde.rectTransform.offsetMax = new Vector2(-5f, -5f);
            selloBorde.fillCenter = false;
            Text ts = Texto("Texto", sello.transform, "¡NUEVO RÉCORD!", 28, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 90f));
            Estirar(ts.rectTransform);
            ts.lineSpacing = 0.85f;
            pr.sello = sello.rectTransform;
            pr.textoSello = ts;
            sello.gameObject.SetActive(false);
        }

        if (derrota)
        {
            CanvasGroup sc = Seccion("Consejo", caja);
            secciones.Add(sc);
            ImagenC("FondoConsejo", sc.transform, SpriteRedondeado, new Color(0.35f, 0.6f, 1f, 0.13f), 0f, 700f, 820f, 100f, true);
            ImagenC("Icono", sc.transform, SpriteUI("copo"), new Color(0.75f, 0.9f, 1f), -360f, 700f, 52f, 52f);
            TextoC("Titulo", sc.transform, "CONSEJO", 20, new Color(0.6f, 0.82f, 1f), TextAnchor.MiddleLeft, 40f, 672f, 680f, 26f);
            pr.textoConsejo = TextoC("Texto", sc.transform, "", 26, Color.white, TextAnchor.UpperLeft, 40f, 712f, 680f, 60f, false);
        }

        // Botones
        CanvasGroup sbt = Seccion("Botones", caja);
        secciones.Add(sbt);
        float yB = H - 70f;
        boton1 = BotonIcono("Boton1", sbt.transform, txt1, ico1, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(390f, 92f), col1, 34);
        Centro((RectTransform)boton1.transform, -215f, yB, 390f, 92f);
        boton2 = BotonIcono("Boton2", sbt.transform, txt2, ico2, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(390f, 92f), col2, 34);
        Centro((RectTransform)boton2.transform, 215f, yB, 390f, 92f);
        foreach (Button b in new[] { boton1, boton2 })
        {
            Shadow sh = b.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0f, -6f);
        }

        pr.secciones = secciones.ToArray();

        // Confeti por encima de todo (solo al ganar)
        if (!derrota)
        {
            GameObject conf = new GameObject("Confeti", typeof(RectTransform));
            RectTransform rc = Rect(conf, fondo.transform);
            Estirar(rc);
            pr.contenedorConfeti = rc;
            pr.spriteConfeti = SpriteUI("confeti");
            pr.cantidadConfeti = victoria ? 90 : 50;
        }

        raiz.SetActive(false);
        return pr;
    }
}
