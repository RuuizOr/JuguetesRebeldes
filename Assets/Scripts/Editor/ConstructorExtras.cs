using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public static partial class ConstructorJuego
{
    // Modelos de los personajes elegibles (mismo orden que Personajes.Lista)
    static readonly string[] ModelosPersonaje = { "character-soldier", "character-female-c", "character-female-b", "character-worker" };
    static readonly float[] AlturasPersonaje = { 0.7549f, 0.7755f, 0.7234f, 0.8241f };
    static RuntimeAnimatorController[] ctrlPersonajes;

    static readonly Dictionary<string, Sprite> fondosCapturados = new Dictionary<string, Sprite>();

    static void CrearControladoresPersonajes()
    {
        ctrlPersonajes = new RuntimeAnimatorController[ModelosPersonaje.Length];
        for (int i = 0; i < ModelosPersonaje.Length; i++)
            ctrlPersonajes[i] = i == 0 ? ctrlDuende
                : CrearControlador("Personaje_" + i, RutaPersonajes + "/" + ModelosPersonaje[i] + ".fbx", true);
    }

    static GameObject[] InstanciarTodosLosPersonajes(Transform padre, out Animator[] animadores)
    {
        GameObject[] modelos = new GameObject[ModelosPersonaje.Length];
        animadores = new Animator[ModelosPersonaje.Length];
        for (int i = 0; i < ModelosPersonaje.Length; i++)
        {
            GameObject m = InstanciarPersonaje(ModelosPersonaje[i], 1.85f, AlturasPersonaje[i], padre, ctrlPersonajes[i]);
            m.name = "Modelo_" + Personajes.Lista[i].nombre;
            m.SetActive(i == 0);
            modelos[i] = m;
            animadores[i] = m.GetComponent<Animator>();
        }
        return modelos;
    }

    // ---------------- Fondo borroso de cada escena (para la pantalla de carga) ----------------

    static void CapturarFondo(string escena, Vector3 pos, Vector3 mirar)
    {
        GameObject go = new GameObject("CamCaptura");
        Camera cam = go.AddComponent<Camera>();
        Camera principal = Camera.main;
        if (principal != null)
        {
            cam.clearFlags = principal.clearFlags;
            cam.backgroundColor = principal.backgroundColor;
        }
        cam.fieldOfView = 50f;
        cam.farClipPlane = 300f;
        go.transform.position = pos;
        go.transform.LookAt(mirar);
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;

        const int w = 960, h = 540;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.Create();

        RenderPipeline.StandardRequest req = new RenderPipeline.StandardRequest { destination = rt };
        if (RenderPipeline.SupportsRenderRequest(cam, req))
            RenderPipeline.SubmitRenderRequest(cam, req);
        else
        {
            cam.targetTexture = rt;
            cam.Render();
        }

        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = anterior;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);

        Texture2D borroso = Desenfocar(tex, 4, 3, 3, 0.85f);
        Object.DestroyImmediate(tex);

        string carpeta = RutaUI + "/Fondos";
        if (!AssetDatabase.IsValidFolder(carpeta))
            AssetDatabase.CreateFolder(RutaUI, "Fondos");
        string ruta = carpeta + "/Fondo_" + escena + ".png";
        File.WriteAllBytes(ruta, borroso.EncodeToPNG());
        Object.DestroyImmediate(borroso);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(ruta);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();

        fondosCapturados[escena] = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    static Texture2D Desenfocar(Texture2D src, int factor, int radio, int pasadas, float brillo)
    {
        int w = src.width / factor, h = src.height / factor;
        Color[] s = src.GetPixels();
        Color[] a = new Color[w * h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.clear;
                for (int dy = 0; dy < factor; dy++)
                    for (int dx = 0; dx < factor; dx++)
                        c += s[(y * factor + dy) * src.width + x * factor + dx];
                a[y * w + x] = c / (factor * factor);
            }

        Color[] b = new Color[w * h];
        for (int p = 0; p < pasadas; p++)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.clear; int n = 0;
                    for (int k = -radio; k <= radio; k++)
                    {
                        int xx = Mathf.Clamp(x + k, 0, w - 1);
                        c += a[y * w + xx]; n++;
                    }
                    b[y * w + x] = c / n;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = Color.clear; int n = 0;
                    for (int k = -radio; k <= radio; k++)
                    {
                        int yy = Mathf.Clamp(y + k, 0, h - 1);
                        c += b[yy * w + x]; n++;
                    }
                    a[y * w + x] = c / n;
                }
        }

        for (int i = 0; i < a.Length; i++)
        {
            Color c = a[i] * brillo;
            c.a = 1f;
            a[i] = c;
        }

        Texture2D r = new Texture2D(w, h, TextureFormat.RGB24, false);
        r.SetPixels(a);
        r.Apply();
        return r;
    }

    // ---------------- Botón con icono ----------------

    static Button BotonIcono(string nombre, Transform padre, string etiqueta, string icono, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, int tamTexto = 32)
    {
        Button b = Boton(nombre, padre, etiqueta, ancla, pos, tam, color, tamTexto);
        Image ic = Imagen("Icono", b.transform, SpriteUI(icono), Color.white);
        float lado = Mathf.Min(tam.y * 0.5f, 56f);
        Colocar(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(lado, lado));
        Text t = b.GetComponentInChildren<Text>();
        t.rectTransform.offsetMin = new Vector2(22f + lado, 0f);
        return b;
    }

    // ---------------- Post proceso con desenfoque (selección de personaje) ----------------

    static void PostProcesoDesenfoque()
    {
        string ruta = RutaMateriales + "/PostProcesoSeleccion.asset";
        AssetDatabase.DeleteAsset(ruta);
        VolumeProfile perfil = null;
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);

            Bloom bloom = perfil.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.85f);

            Vignette v = perfil.Add<Vignette>(true);
            v.intensity.Override(0.4f);

            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.saturation.Override(10f);
            ca.postExposure.Override(0.2f);

            Tonemapping tm = perfil.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);

            DepthOfField dof = perfil.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(8.5f);
            dof.gaussianEnd.Override(16f);
            dof.gaussianMaxRadius.Override(1.5f);
            dof.highQualitySampling.Override(true);

            foreach (VolumeComponent c in perfil.components)
                AssetDatabase.AddObjectToAsset(c, perfil);
            EditorUtility.SetDirty(perfil);
            AssetDatabase.SaveAssets();
        }

        GameObject g = new GameObject("PostProceso");
        Volume vol = g.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = perfil;
    }

    static void LimitesInvisibles(Transform padre, float minX, float maxX, float minZ, float maxZ)
    {
        GameObject lim = Grupo("LimitesInvisibles", padre);
        float anchoX = maxX - minX + 4f, anchoZ = maxZ - minZ + 4f;
        float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f;
        void Pared(Vector3 c, Vector3 t)
        {
            BoxCollider b = lim.AddComponent<BoxCollider>();
            b.center = c;
            b.size = t;
        }
        Pared(new Vector3(cx, 5f, maxZ + 0.2f), new Vector3(anchoX, 10f, 1.2f));
        Pared(new Vector3(cx, 5f, minZ - 0.2f), new Vector3(anchoX, 10f, 1.2f));
        Pared(new Vector3(maxX + 0.2f, 5f, cz), new Vector3(1.2f, 10f, anchoZ));
        Pared(new Vector3(minX - 0.2f, 5f, cz), new Vector3(1.2f, 10f, anchoZ));
    }

    // =====================================================================
    // Escena: selección de personaje
    // =====================================================================

    static void ConstruirSeleccion()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.Random r = new System.Random(11);
        Camera cam = PrepararCamara(new Color(0.06f, 0.08f, 0.16f));
        Iluminacion(true);
        PostProcesoDesenfoque();
        cam.fieldOfView = 40f;
        cam.transform.position = new Vector3(0f, 1.65f, -6.6f);
        cam.transform.LookAt(new Vector3(0f, 1.15f, 0f));

        // Mapa de fondo (se ve desenfocado)
        Transform deco = Grupo("MapaFondo").transform;
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloNieve";
        suelo.transform.localScale = new Vector3(8f, 1f, 8f);
        suelo.GetComponent<Renderer>().sharedMaterial = matNieve;
        Poner("tree-decorated-snow", new Vector3(-3f, 0f, 16f), 0f, 2.2f, deco);
        AnilloRegalos(new Vector3(-3f, 0f, 16f), 2.6f, 9, deco, r, 1.3f);
        Luz("LuzArbol", new Vector3(-3f, 4f, 13f), new Color(1f, 0.6f, 0.35f), 6f, 14f, deco, true);
        string[] pinos = { "tree-snow-a", "tree-snow-b", "tree-snow-c" };
        for (int i = 0; i < 22; i++)
        {
            Vector3 p = new Vector3((float)(r.NextDouble() - 0.5) * 50f, 0f, 12f + (float)r.NextDouble() * 22f);
            if (Mathf.Abs(p.x + 3f) < 4f && p.z < 20f) continue;
            Poner(pinos[r.Next(3)], p, r.Next(0, 360), 1.6f + (float)r.NextDouble() * 1f, deco);
        }
        foreach (Vector3 p in new[] { new Vector3(6f, 0f, 11f), new Vector3(-10f, 0f, 12f), new Vector3(10f, 0f, 18f) })
            Farol(p, deco, 4f);
        Poner("snowman-hat", new Vector3(5f, 0f, 14f), 200f, 1.6f, deco);
        Poner("sled-long", new Vector3(-8f, 0f, 9f), 40f, 1.4f, deco);
        Poner("reindeer", new Vector3(8f, 0f, 9f), 220f, 1.6f, deco);
        for (int i = 0; i < 4; i++)
            Poner(Tipos[i].modelo, new Vector3(-6f + i * 4f, 0f, 22f), 180f, 1.3f, deco);
        CrearNieve(null, new Vector3(0f, 9f, 8f), new Vector3(30f, 1f, 30f), 160f);

        // Plataforma y personajes
        Poner("snow-flat-large", new Vector3(0f, 0f, 0f), 0f, 0.9f, deco);
        GameObject giro = Grupo("Giratorio");
        giro.transform.position = new Vector3(0f, 0.2f, 0f);
        giro.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Girar g = giro.AddComponent<Girar>();
        g.velocidad = 18f;
        g.amplitud = 0f;
        GameObject[] modelos = InstanciarTodosLosPersonajes(giro.transform, out Animator[] animadores);

        GameObject foco = new GameObject("Foco");
        foco.transform.position = new Vector3(0f, 5f, -2.5f);
        foco.transform.LookAt(new Vector3(0f, 0.8f, 0f));
        Light spot = foco.AddComponent<Light>();
        spot.type = UnityEngine.LightType.Spot;
        spot.spotAngle = 45f;
        spot.range = 12f;
        spot.intensity = 12f;
        spot.color = new Color(1f, 0.92f, 0.8f);
        spot.shadows = LightShadows.Soft;
        Luz("Relleno", new Vector3(1.5f, 1.8f, -2.5f), new Color(0.7f, 0.8f, 1f), 1.5f, 6f, null);

        // UI
        Canvas c = CrearCanvas("UI_Seleccion", 10);
        Transform t = c.transform;
        SeleccionPersonaje sp = new GameObject("SeleccionPersonaje").AddComponent<SeleccionPersonaje>();
        sp.modelos = modelos;
        sp.animadores = animadores;
        sp.plataforma = giro.transform;

        Image sup = Imagen("DegradadoArriba", t, SpriteUI("degradado_arriba"), new Color(0f, 0f, 0f, 0.7f));
        sup.rectTransform.anchorMin = new Vector2(0f, 1f); sup.rectTransform.anchorMax = new Vector2(1f, 1f);
        sup.rectTransform.pivot = new Vector2(0.5f, 1f); sup.rectTransform.sizeDelta = new Vector2(0f, 260f);
        sup.rectTransform.anchoredPosition = Vector2.zero;
        Texto("Titulo", t, "Elige tu personaje", 76, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1200f, 110f));

        sp.botonAnterior = BotonFlecha("BotonAnterior", t, "flecha_izq", new Vector2(-300f, 40f));
        sp.botonSiguiente = BotonFlecha("BotonSiguiente", t, "flecha_der", new Vector2(300f, 40f));

        Image info = Panel("PanelInfo", t, new Vector2(1f, 0.5f), new Vector2(-30f, 40f), new Vector2(560f, 560f), new Color(0.05f, 0.07f, 0.14f, 0.85f));
        info.raycastTarget = false;
        Vector2 a = new Vector2(0f, 1f);
        sp.textoNombre = Texto("Nombre", info.transform, "Pino", 84, Color.white, TextAnchor.MiddleLeft, a, new Vector2(40f, -25f), new Vector2(520f, 100f));
        sp.textoTitulo = Texto("Subtitulo", info.transform, "", 36, new Color(1f, 0.82f, 0.3f), TextAnchor.MiddleLeft, a, new Vector2(40f, -120f), new Vector2(520f, 50f));
        sp.textoDescripcion = Texto("Descripcion", info.transform, "", 30, new Color(0.88f, 0.92f, 1f), TextAnchor.UpperLeft, a, new Vector2(40f, -185f), new Vector2(480f, 110f), false);
        sp.barraVelocidad = BarraStat("Velocidad", info.transform, new Vector2(40f, -320f), new Color(0.45f, 0.85f, 1f));
        sp.barraEnergia = BarraStat("Energía", info.transform, new Vector2(40f, -395f), new Color(1f, 0.75f, 0.3f));
        sp.barraVidas = BarraStat("Vidas", info.transform, new Vector2(40f, -470f), new Color(1f, 0.35f, 0.4f));
        sp.textoVidas = Texto("NumVidas", sp.barraVidas.transform.parent, "3", 26, Color.white, TextAnchor.MiddleRight,
            new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(60f, 36f));

        sp.puntos = new Image[ModelosPersonaje.Length];
        for (int i = 0; i < sp.puntos.Length; i++)
        {
            Image d = Imagen("Punto" + i, t, SpriteKnob, Color.white);
            Colocar(d.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - (sp.puntos.Length - 1) / 2f) * 44f, 200f), new Vector2(26f, 26f));
            d.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            sp.puntos[i] = d;
        }

        sp.textoElegido = Texto("TextoElegido", t, "", 34, new Color(0.6f, 1f, 0.6f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(800f, 50f));
        sp.botonElegir = BotonIcono("BotonElegir", t, "¡Elegir!", "estrella", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(420f, 100f), ColorBoton, 44);
        sp.botonVolver = BotonIcono("BotonVolver", t, "Volver", "flecha_izq", new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(280f, 90f), ColorBotonAzul, 36);
        Texto("Ayuda", t, "A / D o flechas para cambiar  •  Enter para elegir", 24, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(-30f, 25f), new Vector2(800f, 40f), false);

        sp.botonAnterior.transform.SetAsLastSibling();
        sp.botonSiguiente.transform.SetAsLastSibling();

        CrearEventSystem();
        CapturarFondo("SeleccionPersonaje", cam.transform.position + new Vector3(0f, 0.5f, -1f), new Vector3(0f, 1.5f, 6f));
        HornearYGuardar(escena, "SeleccionPersonaje", null);
    }

    static Button BotonFlecha(string nombre, Transform padre, string icono, Vector2 pos)
    {
        Button b = Boton(nombre, padre, "", new Vector2(0.5f, 0.5f), pos, new Vector2(120f, 120f), new Color(0.15f, 0.2f, 0.35f, 0.9f));
        b.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        Image ic = Imagen("Icono", b.transform, SpriteUI(icono), Color.white);
        Colocar(ic.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        return b;
    }

    static Image BarraStat(string etiqueta, Transform padre, Vector2 pos, Color color)
    {
        Texto("Label" + etiqueta, padre, etiqueta, 28, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 1f), pos, new Vector2(180f, 40f));
        Image fondo = Imagen("Barra" + etiqueta, padre, SpriteRedondeado, new Color(0f, 0f, 0f, 0.55f), true);
        Colocar(fondo.rectTransform, new Vector2(0f, 1f), pos + new Vector2(170f, -2f), new Vector2(310f, 36f));
        Image relleno = Imagen("Relleno", fondo.transform, SpriteFondo, color);
        Estirar(relleno.rectTransform);
        relleno.rectTransform.offsetMin = new Vector2(4f, 4f);
        relleno.rectTransform.offsetMax = new Vector2(-4f, -4f);
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        return relleno;
    }
}

public static partial class ConstructorJuego
{
    // Exterior nevado alrededor de una sala para que el mapa no "flote" en el vacío.
    static void CrearExteriorNevado(Transform padre, float minX, float maxX, float minZ, float maxZ, System.Random r)
    {
        Transform ext = Grupo("ExteriorNevado", padre).transform;

        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloNieveExterior";
        suelo.transform.SetParent(ext, false);
        suelo.transform.position = new Vector3((minX + maxX) / 2f, -0.02f, (minZ + maxZ) / 2f);
        suelo.transform.localScale = new Vector3(16f, 1f, 16f);
        suelo.GetComponent<Renderer>().sharedMaterial = matNieve;
        Object.DestroyImmediate(suelo.GetComponent<MeshCollider>());
        IgnorarNavMesh(suelo);

        string[] pinos = { "tree-snow-a", "tree-snow-b", "tree-snow-c", "tree" };

        // Bosque a los lados y al fondo (norte)
        for (int i = 0; i < 140; i++)
        {
            float x = minX - 35f + (float)r.NextDouble() * (maxX - minX + 70f);
            float z = minZ - 4f + (float)r.NextDouble() * (maxZ - minZ + 45f);
            bool dentro = x > minX - 3f && x < maxX + 3f && z > minZ - 3f && z < maxZ + 3f;
            if (dentro) continue;
            if (z < minZ + 2f && x > minX - 6f && x < maxX + 6f) continue; // frente libre para la cámara
            float esc = 1.3f + (float)r.NextDouble() * 1.1f;
            Poner(pinos[r.Next(pinos.Length)], new Vector3(x, 0f, z), r.Next(0, 360), esc, ext);
        }

        // Frente (lado de la cámara): solo cosas bajas
        for (float x = minX - 2f; x <= maxX + 2f; x += 3.2f)
        {
            float z = minZ - 4.5f - (float)r.NextDouble() * 6f;
            Poner(r.NextDouble() < 0.7 ? "snow-pile" : "rocks-small", new Vector3(x, 0f, z), r.Next(0, 360), 0.9f + (float)r.NextDouble() * 0.7f, ext);
        }
        for (int i = 0; i < 30; i++)
        {
            float x = minX - 20f + (float)r.NextDouble() * (maxX - minX + 40f);
            float z = minZ - 16f + (float)r.NextDouble() * (maxZ - minZ + 40f);
            bool cerca = x > minX - 2f && x < maxX + 2f && z > minZ - 2f && z < maxZ + 2f;
            if (!cerca) Poner(r.NextDouble() < 0.5 ? "snow-flat-large" : "snow-pile", new Vector3(x, 0f, z), r.Next(0, 360), 1.2f, ext);
        }

        // Detalles con luz
        Vector3[] faroles =
        {
            new Vector3(minX - 3f, 0f, minZ - 2.5f), new Vector3(maxX + 3f, 0f, minZ - 2.5f),
            new Vector3(minX - 3.5f, 0f, (minZ + maxZ) / 2f), new Vector3(maxX + 3.5f, 0f, (minZ + maxZ) / 2f),
            new Vector3(minX - 3f, 0f, maxZ + 3f), new Vector3(maxX + 3f, 0f, maxZ + 3f)
        };
        foreach (Vector3 p in faroles)
        {
            Poner("lantern", p, 0f, 1f, ext);
            Luz("LuzExterior", p + Vector3.up * 3.7f, LuzCalida, 3f, 10f, ext, true);
        }
        Poner("sled-long", new Vector3(minX - 5f, 0f, minZ + 3f), 70f, 1.4f, ext);
        Poner("snowman-hat", new Vector3(maxX + 5f, 0f, minZ + 4f), 230f, 1.5f, ext);
        Poner("snowman", new Vector3(minX - 6f, 0f, maxZ - 2f), 120f, 1.4f, ext);
        Poner("rocks-large", new Vector3(maxX + 8f, 0f, maxZ + 4f), 30f, 1f, ext);
        PilaRegalos(new Vector3(maxX + 5f, 0f, maxZ - 3f), 3, ext, r);

        // Nieve cayendo solo afuera (cuatro franjas alrededor del taller)
        float cx = (minX + maxX) / 2f, cz = (minZ + maxZ) / 2f;
        float ancho = maxX - minX, largo = maxZ - minZ;
        CrearNieve(ext, new Vector3(cx, 9f, maxZ + 9f), new Vector3(ancho + 40f, 1f, 16f), 90f);
        CrearNieve(ext, new Vector3(cx, 9f, minZ - 9f), new Vector3(ancho + 40f, 1f, 16f), 90f);
        CrearNieve(ext, new Vector3(minX - 10f, 9f, cz), new Vector3(18f, 1f, largo + 2f), 60f);
        CrearNieve(ext, new Vector3(maxX + 10f, 9f, cz), new Vector3(18f, 1f, largo + 2f), 60f);
    }
}
