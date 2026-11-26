using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Construye TODO el juego "Juguetes Rebeldes": importación, prefabs, UI y escenas.
// Menú: Juguetes Rebeldes > Construir todo
public static partial class ConstructorJuego
{
    public const float S = 2.5f; // 1 módulo Kenney = 2.5 m
    const string RutaHoliday = "Assets/Modelos/Holiday";
    const string RutaPersonajes = "Assets/Modelos/Personajes";
    const string RutaUI = "Assets/UI";
    const string RutaPrefabs = "Assets/Prefabs";
    const string RutaMateriales = "Assets/Materiales";
    const string RutaEscenas = "Assets/Scenes";
    const string RutaAnimaciones = "Assets/Animaciones";

    static float factorUnidad = -1f;
    static Font fuente;

    [MenuItem("Juguetes Rebeldes/Construir todo", priority = 0)]
    public static void ConstruirTodo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        try
        {
            EditorUtility.DisplayProgressBar("Juguetes Rebeldes", "Configurando importación...", 0.05f);
            ConfigurarImportacion();
            Preparar();

            EditorUtility.DisplayProgressBar("Juguetes Rebeldes", "Creando prefabs...", 0.2f);
            CrearTodosLosPrefabs();

            EditorUtility.DisplayProgressBar("Juguetes Rebeldes", "Escena: Intro", 0.35f);
            ConstruirIntro();
            EditorUtility.DisplayProgressBar("Juguetes Rebeldes", "Escena: Menú", 0.5f);
            ConstruirMenu();
            for (int n = 1; n <= 3; n++)
            {
                EditorUtility.DisplayProgressBar("Juguetes Rebeldes", "Escena: Nivel " + n, 0.55f + n * 0.13f);
                ConstruirNivel(n);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(RutaEscenas + "/Intro.unity", true),
                new EditorBuildSettingsScene(RutaEscenas + "/MenuPrincipal.unity", true),
                new EditorBuildSettingsScene(RutaEscenas + "/Nivel1.unity", true),
                new EditorBuildSettingsScene(RutaEscenas + "/Nivel2.unity", true),
                new EditorBuildSettingsScene(RutaEscenas + "/Nivel3.unity", true),
            };

            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(RutaEscenas + "/Intro.unity");
            Debug.Log("<b>Juguetes Rebeldes construido.</b> Abre la escena Intro y presiona Play.");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Juguetes Rebeldes/Reiniciar progreso guardado", priority = 20)]
    public static void ReiniciarProgreso()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Progreso borrado (niveles, estrellas y volumen).");
    }

    [MenuItem("Juguetes Rebeldes/Desbloquear todos los niveles", priority = 21)]
    public static void DesbloquearTodo()
    {
        PlayerPrefs.SetInt("NivelDesbloqueado", 3);
        PlayerPrefs.Save();
        Debug.Log("Niveles desbloqueados.");
    }

    // =====================================================================
    // Importación
    // =====================================================================

    static void ConfigurarImportacion()
    {
        AssetDatabase.Refresh();

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { RutaUI }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(p);
            if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Musica" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter ai = (AudioImporter)AssetImporter.GetAtPath(p);
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.Streaming) continue;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            ai.defaultSampleSettings = s;
            ai.SaveAndReimport();
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { RutaHoliday }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(p);
            if (!mi.importAnimation) continue;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.SaveAndReimport();
        }

        string[] loops = { "idle", "walk", "sprint", "fall", "emote-yes" };
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { RutaPersonajes }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter mi = (ModelImporter)AssetImporter.GetAtPath(p);
            if (mi.userData == "configurado") continue;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
            foreach (ModelImporterClipAnimation c in clips)
                c.loopTime = loops.Any(l => c.name == l || c.name.EndsWith("|" + l));
            mi.clipAnimations = clips;
            mi.userData = "configurado";
            mi.SaveAndReimport();
        }

        AssetDatabase.Refresh();
    }

    static void Preparar()
    {
        foreach (string c in new[] { RutaPrefabs, RutaMateriales, RutaEscenas, RutaAnimaciones, "Assets/Resources" })
            if (!AssetDatabase.IsValidFolder(c))
            {
                string padre = Path.GetDirectoryName(c).Replace('\\', '/');
                AssetDatabase.CreateFolder(padre, Path.GetFileName(c));
            }

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        factorUnidad = -1f;
        CrearMateriales();
    }

    // =====================================================================
    // Modelos y utilidades 3D
    // =====================================================================

    static float Unidad
    {
        get
        {
            if (factorUnidad < 0f)
            {
                GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(RutaHoliday + "/cabin-wall.fbx");
                GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(src);
                tmp.transform.localScale = Vector3.one;
                Bounds b = BoundsMalla(tmp);
                Object.DestroyImmediate(tmp);
                factorUnidad = b.size.y > 0.0001f ? 1f / b.size.y : 1f;
                Debug.Log("Factor de unidad de modelos: " + factorUnidad);
            }
            return factorUnidad;
        }
    }

    static GameObject CargarModelo(string nombre)
    {
        GameObject m = AssetDatabase.LoadAssetAtPath<GameObject>(RutaHoliday + "/" + nombre + ".fbx");
        if (m == null) m = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPersonajes + "/" + nombre + ".fbx");
        if (m == null) Debug.LogError("No se encontró el modelo " + nombre);
        return m;
    }

    // Coloca un modelo. Si alinear=true, el centro (x,z) y la base del modelo quedan en "pos".
    static GameObject Poner(string modelo, Vector3 pos, float rotY = 0f, float escala = 1f, Transform padre = null,
        bool colision = false, bool alinear = true)
    {
        GameObject src = CargarModelo(modelo);
        if (src == null) return new GameObject("FALTA_" + modelo);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        if (padre != null) go.transform.SetParent(padre, true);
        go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale = Vector3.one * S * escala * Unidad;
        go.transform.position = pos;

        if (alinear)
        {
            Bounds b = BoundsMalla(go);
            go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
        }

        if (colision)
            AjustarCollider(go);

        return go;
    }

    static Bounds BoundsMalla(GameObject go)
    {
        bool hay = false;
        Bounds total = new Bounds(go.transform.position, Vector3.zero);

        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Encapsular(ref total, ref hay, mf.sharedMesh.bounds, mf.transform.localToWorldMatrix);
        }
        foreach (SkinnedMeshRenderer sr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (sr.sharedMesh == null) continue;
            Encapsular(ref total, ref hay, sr.sharedMesh.bounds, sr.transform.localToWorldMatrix);
        }
        return total;
    }

    static void Encapsular(ref Bounds total, ref bool hay, Bounds local, Matrix4x4 m)
    {
        Vector3 c = local.center, e = local.extents;
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            Vector3 w = m.MultiplyPoint3x4(p);
            if (!hay) { total = new Bounds(w, Vector3.zero); hay = true; }
            else total.Encapsulate(w);
        }
    }

    static BoxCollider AjustarCollider(GameObject go, float margen = 0f)
    {
        Bounds b = BoundsMalla(go);
        BoxCollider bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = go.AddComponent<BoxCollider>();
        bc.center = go.transform.InverseTransformPoint(b.center);
        Vector3 t = go.transform.InverseTransformVector(b.size + Vector3.one * margen);
        bc.size = new Vector3(Mathf.Abs(t.x), Mathf.Abs(t.y), Mathf.Abs(t.z));
        return bc;
    }

    static void EscalarAAltura(GameObject go, float altura)
    {
        Bounds b = BoundsMalla(go);
        if (b.size.y > 0.0001f)
            go.transform.localScale *= altura / b.size.y;
    }

    static GameObject Grupo(string nombre, Transform padre = null)
    {
        GameObject g = new GameObject(nombre);
        if (padre != null) g.transform.SetParent(padre, false);
        return g;
    }

    static void IgnorarNavMesh(GameObject go)
    {
        NavMeshModifier m = go.GetComponent<NavMeshModifier>();
        if (m == null) m = go.AddComponent<NavMeshModifier>();
        m.ignoreFromBuild = true;
    }

    static Light Luz(string nombre, Vector3 pos, Color color, float intensidad, float rango, Transform padre, bool titilar = false)
    {
        GameObject g = new GameObject(nombre);
        if (padre != null) g.transform.SetParent(padre, false);
        g.transform.position = pos;
        Light l = g.AddComponent<Light>();
        l.type = UnityEngine.LightType.Point;
        l.color = color;
        l.intensity = intensidad;
        l.range = rango;
        l.shadows = LightShadows.None;
        if (titilar) g.AddComponent<LuzTitilante>();
        return l;
    }

    static void Iluminacion(bool exterior)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        if (exterior)
        {
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.38f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.24f, 0.36f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.1f, 0.13f, 0.24f);
            RenderSettings.fogDensity = 0.012f;
        }
        else
        {
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.36f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.3f, 0.24f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.12f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.08f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.01f;
        }

        Light sol = Object.FindAnyObjectByType<Light>();
        if (sol != null && sol.type == UnityEngine.LightType.Directional)
        {
            sol.name = exterior ? "Luna" : "Luz Ambiente";
            sol.color = exterior ? new Color(0.6f, 0.7f, 1f) : new Color(1f, 0.85f, 0.7f);
            sol.intensity = exterior ? 0.7f : 0.45f;
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }
    }

    static Camera PrepararCamara(Color fondo)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject g = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            g.tag = "MainCamera";
            cam = g.GetComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = fondo;
        cam.fieldOfView = 50f;
        cam.farClipPlane = 300f;
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        return cam;
    }

    static void PostProceso()
    {
        string ruta = RutaMateriales + "/PostProceso.asset";
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, ruta);

            Bloom bloom = perfil.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);

            Vignette v = perfil.Add<Vignette>(true);
            v.intensity.Override(0.32f);
            v.smoothness.Override(0.45f);

            ColorAdjustments ca = perfil.Add<ColorAdjustments>(true);
            ca.saturation.Override(12f);
            ca.contrast.Override(8f);
            ca.postExposure.Override(0.25f);

            Tonemapping tm = perfil.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.Neutral);

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

    static NavMeshSurface CrearNavegacion()
    {
        GameObject nav = new GameObject("Navigation");
        NavMeshSurface s = nav.AddComponent<NavMeshSurface>();
        s.collectObjects = CollectObjects.All;
        s.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        s.agentTypeID = 0;
        return s;
    }

    static void HornearYGuardar(UnityEngine.SceneManagement.Scene escena, string nombre, NavMeshSurface surface)
    {
        string rutaEscena = RutaEscenas + "/" + nombre + ".unity";
        EditorSceneManager.SaveScene(escena, rutaEscena);

        if (surface != null)
        {
            string carpeta = RutaEscenas + "/" + nombre;
            if (!AssetDatabase.IsValidFolder(carpeta))
                AssetDatabase.CreateFolder(RutaEscenas, nombre);
            string rutaNav = carpeta + "/NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(rutaNav) != null)
                AssetDatabase.DeleteAsset(rutaNav);

            surface.BuildNavMesh();
            AssetDatabase.CreateAsset(surface.navMeshData, rutaNav);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(escena);
        }

        EditorSceneManager.SaveScene(escena, rutaEscena);
    }

    // =====================================================================
    // Materiales y partículas
    // =====================================================================

    static Material matParticula, matCopo, matNieve, matHielo;

    static Material CrearMaterialLit(string nombre, Color color, float suavidad, Color? emision = null, bool transparente = false)
    {
        string ruta = RutaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", suavidad);
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (transparente)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material CrearMaterialParticula(string nombre, string textura)
    {
        string ruta = RutaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaUI + "/" + textura + ".png");
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CrearMateriales()
    {
        matParticula = CrearMaterialParticula("MatParticula", "particula");
        matCopo = CrearMaterialParticula("MatCopo", "particula_copo");
        matNieve = CrearMaterialLit("MatNieve", new Color(0.88f, 0.92f, 1f), 0.25f);
        matHielo = CrearMaterialLit("MatHielo", new Color(0.55f, 0.85f, 1f, 0.6f), 0.95f, new Color(0.05f, 0.15f, 0.25f), true);
        AssetDatabase.SaveAssets();
    }

    static ParticleSystem CrearNieve(Transform padre, Vector3 centro, Vector3 tam, float tasa)
    {
        GameObject g = new GameObject("Nieve");
        if (padre != null) g.transform.SetParent(padre, false);
        g.transform.position = centro;
        g.transform.rotation = Quaternion.identity;

        ParticleSystem ps = g.AddComponent<ParticleSystem>();
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = matCopo;

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 5f;
        main.startLifetime = 12f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = tasa;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = tam;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y = new ParticleSystem.MinMaxCurve(-1.3f, -0.8f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.25f;

        return ps;
    }

    static GameObject CrearEfectoBurst(string nombre, Color a, Color b, int cantidad, float velMin, float velMax,
        float vida, float tam, float gravedad, bool guardarPrefab = true)
    {
        GameObject g = new GameObject(nombre);
        ParticleSystem ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        g.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;

        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.6f, vida);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
        main.startSize = new ParticleSystem.MinMaxCurve(tam * 0.5f, tam);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.gravityModifier = gravedad;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.3f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                   new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;

        g.AddComponent<AutoDestruir>().segundos = vida + 1f;

        if (!guardarPrefab) return g;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(g, RutaPrefabs + "/" + nombre + ".prefab");
        Object.DestroyImmediate(g);
        return prefab;
    }

    // =====================================================================
    // UI
    // =====================================================================

    static Sprite SpriteUI(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(RutaUI + "/" + nombre + ".png");
    static Sprite SpriteRedondeado => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    static Sprite SpriteFondo => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    static Sprite SpriteKnob => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    static Sprite SpriteCheck => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

    static readonly Color ColorBoton = new Color(0.18f, 0.55f, 0.32f);
    static readonly Color ColorBotonRojo = new Color(0.75f, 0.18f, 0.2f);
    static readonly Color ColorBotonAzul = new Color(0.2f, 0.4f, 0.7f);
    static readonly Color ColorPanel = new Color(0.08f, 0.1f, 0.18f, 0.92f);

    static Canvas CrearCanvas(string nombre, int orden)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas c = g.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        CanvasScaler sc = g.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;
        return c;
    }

    static void CrearEventSystem()
    {
        GameObject g = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        g.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    static RectTransform Rect(GameObject g, Transform padre)
    {
        RectTransform rt = g.GetComponent<RectTransform>();
        if (rt == null) rt = g.AddComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    // ancla: (0..1, 0..1). pos relativa al ancla. El pivote es igual al ancla.
    static void Colocar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Image Imagen(string nombre, Transform padre, Sprite sprite, Color color, bool sliced = false)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        Rect(g, padre);
        Image img = g.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        if (sliced) img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return img;
    }

    static Image Panel(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color)
    {
        Image img = Imagen(nombre, padre, SpriteRedondeado, color, true);
        img.raycastTarget = true;
        Colocar(img.rectTransform, ancla, pos, tam);
        return img;
    }

    static Text Texto(string nombre, Transform padre, string contenido, int tam, Color color, TextAnchor alineacion,
        Vector2 ancla, Vector2 pos, Vector2 caja, bool negrita = true, bool sombra = true)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        RectTransform rt = Rect(g, padre);
        Colocar(rt, ancla, pos, caja);
        Text t = g.GetComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        if (sombra)
        {
            Outline o = g.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.75f);
            o.effectDistance = new Vector2(2f, -2f);
        }
        return t;
    }

    static Button Boton(string nombre, Transform padre, string etiqueta, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, int tamTexto = 38)
    {
        Image img = Imagen(nombre, padre, SpriteRedondeado, Color.white, true);
        img.raycastTarget = true;
        Colocar(img.rectTransform, ancla, pos, tam);

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
        cb.selectedColor = color;
        cb.pressedColor = Color.Lerp(color, Color.black, 0.3f);
        cb.disabledColor = new Color(0.35f, 0.35f, 0.4f, 0.8f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        Navigation nav = b.navigation;
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;

        Text t = Texto("Texto", img.transform, etiqueta, tamTexto, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, tam);
        Estirar(t.rectTransform);

        img.gameObject.AddComponent<BotonSonido>();
        return b;
    }

    static Slider CrearSlider(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam, Color colorRelleno)
    {
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = SpriteRedondeado,
            background = SpriteFondo,
            knob = SpriteKnob,
            checkmark = SpriteCheck
        };
        GameObject g = DefaultControls.CreateSlider(res);
        g.name = nombre;
        RectTransform rt = Rect(g, padre);
        Colocar(rt, ancla, pos, tam);
        Slider s = g.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        Transform fill = g.transform.Find("Fill Area/Fill");
        if (fill != null) fill.GetComponent<Image>().color = colorRelleno;
        Transform handle = g.transform.Find("Handle Slide Area/Handle");
        if (handle != null) handle.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 0f);
        Navigation nav = s.navigation;
        nav.mode = Navigation.Mode.None;
        s.navigation = nav;
        return s;
    }

    static Toggle CrearToggle(string nombre, Transform padre, string etiqueta, Vector2 ancla, Vector2 pos)
    {
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = SpriteRedondeado,
            background = SpriteFondo,
            knob = SpriteKnob,
            checkmark = SpriteCheck
        };
        GameObject g = DefaultControls.CreateToggle(res);
        g.name = nombre;
        RectTransform rt = Rect(g, padre);
        Colocar(rt, ancla, pos, new Vector2(460f, 50f));
        Transform bg = g.transform.Find("Background");
        bg.GetComponent<RectTransform>().sizeDelta = new Vector2(44f, 44f);
        bg.GetComponent<RectTransform>().anchoredPosition = new Vector2(22f, -22f);
        Text label = g.transform.Find("Label").GetComponent<Text>();
        label.text = etiqueta;
        label.font = fuente;
        label.fontSize = 32;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        RectTransform lrt = label.rectTransform;
        lrt.offsetMin = new Vector2(64f, 0f);
        lrt.offsetMax = Vector2.zero;
        Toggle t = g.GetComponent<Toggle>();
        Navigation nav = t.navigation;
        nav.mode = Navigation.Mode.None;
        t.navigation = nav;
        return t;
    }

    // Fondo oscuro a pantalla completa + caja central (primer hijo, para la animación de aparición).
    static RectTransform CrearPanelModal(string nombre, Transform padre, Vector2 tamCaja, string titulo, Color colorTitulo, out GameObject raiz)
    {
        Image fondo = Imagen(nombre, padre, null, new Color(0f, 0f, 0f, 0.6f));
        fondo.raycastTarget = true;
        Estirar(fondo.rectTransform);
        raiz = fondo.gameObject;

        Image caja = Panel("Caja", fondo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, tamCaja, ColorPanel);
        caja.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image borde = Imagen("Borde", caja.transform, SpriteRedondeado, new Color(1f, 0.85f, 0.4f, 0.35f), true);
        Estirar(borde.rectTransform);
        borde.rectTransform.offsetMin = new Vector2(6f, 6f);
        borde.rectTransform.offsetMax = new Vector2(-6f, -6f);
        borde.fillCenter = false;

        Texto("Titulo", caja.transform, titulo, 64, colorTitulo, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(tamCaja.x - 40f, 90f));
        return caja.rectTransform;
    }

    static PanelOpciones CrearPanelOpciones(Transform padre, out Button volver)
    {
        RectTransform caja = CrearPanelModal("PanelOpciones", padre, new Vector2(760f, 560f), "Opciones", new Color(0.7f, 0.9f, 1f), out GameObject raiz);
        PanelOpciones po = raiz.AddComponent<PanelOpciones>();

        po.textoMusica = Texto("TextoMusica", caja, "Música: 60%", 34, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(560f, 50f), false);
        po.sliderMusica = CrearSlider("SliderMusica", caja, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(560f, 36f), new Color(0.4f, 0.85f, 0.5f));

        po.textoEfectos = Texto("TextoEfectos", caja, "Efectos: 85%", 34, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0.5f, 1f), new Vector2(0f, -265f), new Vector2(560f, 50f), false);
        po.sliderEfectos = CrearSlider("SliderEfectos", caja, new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(560f, 36f), new Color(1f, 0.75f, 0.3f));

        po.togglePantallaCompleta = CrearToggle("TogglePantalla", caja, "Pantalla completa", new Vector2(0.5f, 1f), new Vector2(-50f, -385f));

        volver = Boton("BotonVolver", caja, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 80f), ColorBotonAzul);
        raiz.SetActive(false);
        return po;
    }
}
