using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public static partial class ConstructorJuego
{
    public class TipoJuguete
    {
        public string id, nombre, modelo;
        public bool animado;
        public float altura, alturaObj, rotModelo;

        public TipoJuguete(string id, string nombre, string modelo, bool animado, float altura, float alturaObj = 0f, float rotModelo = 0f)
        {
            this.id = id; this.nombre = nombre; this.modelo = modelo; this.animado = animado;
            this.altura = altura; this.alturaObj = alturaObj; this.rotModelo = rotModelo;
        }
    }

    public static readonly TipoJuguete[] Tipos =
    {
        new TipoJuguete("jengibre", "el muñeco de jengibre", "gingerbread-man", false, 1.2f),
        new TipoJuguete("galleta", "la galleta de jengibre", "gingerbread-woman", false, 1.2f),
        new TipoJuguete("reno", "el reno", "reindeer", false, 1.45f),
        new TipoJuguete("nieve", "el muñeco de nieve", "snowman-hat", false, 1.5f),
        new TipoJuguete("tren", "el trenecito", "train-locomotive", false, 0.95f, 0f, 90f),
        new TipoJuguete("robot", "el robot", "character-robot", true, 1.3f, 0.8445f),
        new TipoJuguete("soldado", "el soldadito", "character-knight", true, 1.3f, 0.8136f),
    };

    static GameObject prefabDuende, prefabCascanueces, prefabBaston, prefabPuertaLuz;
    static GameObject efectoAtrapar, efectoEscape, efectoPowerUp;
    static RuntimeAnimatorController ctrlDuende;
    static readonly System.Collections.Generic.Dictionary<string, GameObject> prefabsJuguete = new System.Collections.Generic.Dictionary<string, GameObject>();

    static void CrearTodosLosPrefabs()
    {
        efectoAtrapar = CrearEfectoBurst("EfectoAtrapar", new Color(1f, 0.85f, 0.25f), Color.white, 45, 3f, 7f, 0.8f, 0.4f, 0.6f);
        efectoEscape = CrearEfectoBurst("EfectoEscape", new Color(0.7f, 0.3f, 1f), new Color(1f, 0.3f, 0.3f), 35, 2f, 5f, 0.9f, 0.5f, -0.3f);
        efectoPowerUp = CrearEfectoBurst("EfectoPowerUp", new Color(1f, 0.45f, 0.65f), Color.white, 40, 3f, 6f, 0.7f, 0.35f, 0.3f);

        ctrlDuende = CrearControlador("Duende", RutaPersonajes + "/character-soldier.fbx", true);
        prefabDuende = CrearPrefabDuende();
        prefabCascanueces = CrearPrefabCascanueces();
        prefabBaston = CrearPrefabBaston();

        prefabsJuguete.Clear();
        foreach (TipoJuguete t in Tipos)
            prefabsJuguete[t.id] = CrearPrefabJuguete(t);

        CrearAudioManager();
        CrearPantallaCarga();
        AssetDatabase.SaveAssets();
    }

    // ---------------- Animaciones ----------------

    static AnimationClip Clip(string fbx, string nombre)
    {
        AnimationClip[] todos = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToArray();
        return todos.FirstOrDefault(c => c.name == nombre)
            ?? todos.FirstOrDefault(c => c.name.EndsWith("|" + nombre))
            ?? todos.FirstOrDefault(c => c.name.EndsWith(nombre));
    }

    static RuntimeAnimatorController CrearControlador(string nombre, string fbx, bool completo)
    {
        string ruta = RutaAnimaciones + "/" + nombre + ".controller";
        AnimatorController existente = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
        if (existente != null && existente.layers.Length > 0 && existente.layers[0].stateMachine.states.Length > 0)
            return existente;
        if (existente != null)
            AssetDatabase.DeleteAsset(ruta);

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter(new AnimatorControllerParameter { name = "EnSuelo", type = AnimatorControllerParameterType.Bool, defaultBool = true });
        ctrl.AddParameter("Morir", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Celebrar", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Velocidad";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(Clip(fbx, "idle"), 0f);
        arbol.AddChild(Clip(fbx, "walk"), completo ? 3f : 2f);
        arbol.AddChild(Clip(fbx, "sprint"), completo ? 8.5f : 6f);
        sm.defaultState = loco;

        if (completo)
        {
            AnimatorState aire = sm.AddState("Aire");
            aire.motion = Clip(fbx, "jump") ?? Clip(fbx, "fall");
            AnimatorStateTransition t1 = loco.AddTransition(aire);
            t1.hasExitTime = false; t1.duration = 0.08f;
            t1.AddCondition(AnimatorConditionMode.IfNot, 0f, "EnSuelo");
            AnimatorStateTransition t2 = aire.AddTransition(loco);
            t2.hasExitTime = false; t2.duration = 0.1f;
            t2.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");

            AnimatorState morir = sm.AddState("Morir");
            morir.motion = Clip(fbx, "die");
            AnimatorStateTransition tm = sm.AddAnyStateTransition(morir);
            tm.hasExitTime = false; tm.duration = 0.1f; tm.canTransitionToSelf = false;
            tm.AddCondition(AnimatorConditionMode.If, 0f, "Morir");

            AnimatorState celebrar = sm.AddState("Celebrar");
            celebrar.motion = Clip(fbx, "emote-yes");
            AnimatorStateTransition tc = sm.AddAnyStateTransition(celebrar);
            tc.hasExitTime = false; tc.duration = 0.15f; tc.canTransitionToSelf = false;
            tc.AddCondition(AnimatorConditionMode.If, 0f, "Celebrar");
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // ---------------- Personajes ----------------

    static GameObject InstanciarPersonaje(string modelo, float altura, float alturaObj, Transform padre, RuntimeAnimatorController ctrl, float rotY = 0f)
    {
        GameObject src = CargarModelo(modelo);
        GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(src);
        m.name = "Modelo";
        m.transform.SetParent(padre, false);
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        m.transform.localScale = Vector3.one * Unidad * (altura / alturaObj);

        Animator an = m.GetComponent<Animator>();
        if (an == null) an = m.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;
        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return m;
    }

    static GameObject ModeloEstatico(string modelo, float altura, Transform padre, float rotY = 0f)
    {
        GameObject m = Poner(modelo, Vector3.zero, rotY, 1f, null, false, true);
        EscalarAAltura(m, altura);
        Bounds b = BoundsMalla(m);
        m.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        m.name = "Modelo";
        m.transform.SetParent(padre, false);
        return m;
    }

    static GameObject GuardarPrefab(GameObject g, string nombre)
    {
        string ruta = RutaPrefabs + "/" + nombre + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(g, ruta);
        Object.DestroyImmediate(g);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
    }

    static GameObject CrearPrefabDuende()
    {
        GameObject root = new GameObject("Duende");
        root.tag = "Player";

        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.92f, 0f);
        cc.stepOffset = 0.45f;
        cc.skinWidth = 0.05f;
        cc.slopeLimit = 50f;

        DuendeController dc = root.AddComponent<DuendeController>();
        GameObject m = InstanciarPersonaje("character-soldier", 1.85f, 0.7549f, root.transform, ctrlDuende);
        dc.animator = m.GetComponent<Animator>();

        // Estela del bastón de dulce
        GameObject est = new GameObject("EstelaBoost");
        est.transform.SetParent(root.transform, false);
        est.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        ParticleSystem ps = est.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        est.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.7f), Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission;
        em.rateOverTime = 50f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.4f;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        dc.efectoBoost = ps;

        IgnorarNavMesh(root);
        return GuardarPrefab(root, "Duende");
    }

    static GameObject CrearPrefabCascanueces()
    {
        GameObject root = new GameObject("Cascanueces");

        NavMeshAgent ag = root.AddComponent<NavMeshAgent>();
        ag.radius = 0.7f;
        ag.height = 3.2f;
        ag.baseOffset = 0f;
        ag.speed = 2.5f;
        ag.acceleration = 14f;
        ag.angularSpeed = 300f;
        ag.stoppingDistance = 0.4f;

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.7f;
        col.height = 3.2f;
        col.center = new Vector3(0f, 1.6f, 0f);

        Cascanueces cn = root.AddComponent<Cascanueces>();
        GameObject m = ModeloEstatico("nutcracker", 3.3f, root.transform);

        Saltarin sal = root.AddComponent<Saltarin>();
        sal.modelo = m.transform;
        sal.altura = 0.12f;
        sal.frecuencia = 7f;
        sal.inclinacion = 5f;
        sal.velocidadReferencia = 2.5f;
        sal.sonidoPisada = true;

        Light ojos = Luz("LuzOjos", Vector3.zero, new Color(1f, 0.75f, 0.3f), 1.5f, 7f, root.transform);
        ojos.transform.localPosition = new Vector3(0f, 2.7f, 0.7f);
        cn.luzOjos = ojos;

        IgnorarNavMesh(root);
        return GuardarPrefab(root, "Cascanueces");
    }

    static GameObject CrearPrefabBaston()
    {
        GameObject root = new GameObject("BastonDulce");
        SphereCollider sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.9f;
        sc.center = new Vector3(0f, 0.9f, 0f);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        PowerUpBaston pu = root.AddComponent<PowerUpBaston>();
        pu.efecto = efectoPowerUp;

        GameObject giro = new GameObject("Giro");
        giro.transform.SetParent(root.transform, false);
        giro.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        Girar g = giro.AddComponent<Girar>();
        g.velocidad = 120f;
        g.amplitud = 0.2f;
        ModeloEstatico("candy-cane-red", 1.3f, giro.transform);
        Luz("Brillo", Vector3.zero, new Color(1f, 0.5f, 0.6f), 2f, 4f, root.transform).transform.localPosition = new Vector3(0f, 1f, 0f);

        IgnorarNavMesh(root);
        return GuardarPrefab(root, "BastonDulce");
    }

    static GameObject CrearPrefabJuguete(TipoJuguete t)
    {
        GameObject root = new GameObject("Juguete_" + t.id);

        NavMeshAgent ag = root.AddComponent<NavMeshAgent>();
        ag.radius = 0.45f;
        ag.height = 1.4f;
        ag.baseOffset = 0f;
        ag.acceleration = 24f;
        ag.angularSpeed = 600f;
        ag.stoppingDistance = 0.2f;

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.7f;
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Juguete j = root.AddComponent<Juguete>();
        j.nombreJuguete = t.nombre;
        j.efectoAtrapar = efectoAtrapar;
        j.efectoEscape = efectoEscape;

        if (t.animado)
        {
            RuntimeAnimatorController ctrl = CrearControlador("Juguete_" + t.id, RutaPersonajes + "/" + t.modelo + ".fbx", false);
            GameObject m = InstanciarPersonaje(t.modelo, t.altura, t.alturaObj, root.transform, ctrl, t.rotModelo);
            j.animator = m.GetComponent<Animator>();
        }
        else
        {
            GameObject m = ModeloEstatico(t.modelo, t.altura, root.transform, t.rotModelo);
            Saltarin s = root.AddComponent<Saltarin>();
            s.modelo = m.transform;
            s.altura = 0.3f;
            s.frecuencia = 11f;
            s.inclinacion = 12f;
        }

        GameObject et = new GameObject("Etiqueta");
        et.transform.SetParent(root.transform, false);
        et.transform.localPosition = new Vector3(0f, t.altura + 0.55f, 0f);
        TextMesh tm = et.AddComponent<TextMesh>();
        tm.font = fuente;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.text = "";
        et.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        et.AddComponent<Billboard>();
        j.etiqueta = tm;

        IgnorarNavMesh(root);
        return GuardarPrefab(root, "Juguete_" + t.id);
    }

    // ---------------- Resources: audio y pantalla de carga ----------------

    static void CrearAudioManager()
    {
        GameObject g = new GameObject("AudioManager");
        AudioManager am = g.AddComponent<AudioManager>();
        am.musicaMenu = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musica/MusicaMenu.wav");
        am.musicaJuego = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musica/MusicaJuego.ogg");

        var lista = new System.Collections.Generic.List<AudioManager.Sonido>();
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" }))
        {
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            float vol = c.name == "hover" ? 0.5f : c.name == "pisada" ? 0.9f : 1f;
            lista.Add(new AudioManager.Sonido { nombre = c.name, clip = c, volumen = vol });
        }
        am.sonidos = lista.ToArray();
        PrefabUtility.SaveAsPrefabAsset(g, "Assets/Resources/AudioManager.prefab");
        Object.DestroyImmediate(g);
    }

    static void CrearPantallaCarga()
    {
        Canvas c = CrearCanvas("PantallaCarga", 999);
        CanvasGroup cg = c.gameObject.AddComponent<CanvasGroup>();
        SceneLoader sl = c.gameObject.AddComponent<SceneLoader>();
        sl.grupo = cg;

        Image fondo = Imagen("Fondo", c.transform, SpriteUI("fondo_degradado"), Color.white);
        fondo.raycastTarget = true;
        Estirar(fondo.rectTransform);

        Vector2[] copos = { new Vector2(-760, 380), new Vector2(720, 300), new Vector2(-600, -330), new Vector2(650, -380), new Vector2(0, 430), new Vector2(-250, 250), new Vector2(330, -260) };
        for (int i = 0; i < copos.Length; i++)
        {
            Image cp = Imagen("CopoDeco", c.transform, SpriteUI("copo"), new Color(1f, 1f, 1f, 0.08f + 0.04f * (i % 3)));
            float t = 90f + 50f * (i % 3);
            Colocar(cp.rectTransform, new Vector2(0.5f, 0.5f), copos[i], new Vector2(t, t));
            cp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 23f);
        }

        Image icono = Imagen("Icono", c.transform, SpriteUI("copo"), new Color(0.85f, 0.95f, 1f));
        Colocar(icono.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(180f, 180f));
        icono.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        sl.icono = icono.rectTransform;

        Text tit = Texto("Titulo", c.transform, "Cargando...", 72, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1400f, 100f));
        tit.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        sl.textoTitulo = tit;

        Image barraFondo = Imagen("BarraFondo", c.transform, SpriteRedondeado, new Color(0f, 0f, 0f, 0.5f), true);
        Colocar(barraFondo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(900f, 44f));
        barraFondo.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image barra = Imagen("Barra", barraFondo.transform, SpriteFondo, new Color(0.35f, 0.85f, 0.45f));
        Estirar(barra.rectTransform);
        barra.rectTransform.offsetMin = new Vector2(6f, 6f);
        barra.rectTransform.offsetMax = new Vector2(-6f, -6f);
        barra.type = Image.Type.Filled;
        barra.fillMethod = Image.FillMethod.Horizontal;
        barra.fillAmount = 0f;
        sl.barra = barra;

        Text pct = Texto("Porcentaje", c.transform, "0%", 36, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(300f, 60f));
        pct.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        sl.textoPorcentaje = pct;

        Text tip = Texto("Tip", c.transform, "", 34, new Color(0.85f, 0.9f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(1500f, 120f), false);
        tip.fontStyle = FontStyle.Italic;
        sl.textoTip = tip;

        sl.tips = new[]
        {
            "Tip: los juguetes se cansan después de correr un rato. ¡Es tu oportunidad!",
            "Tip: mantén Shift para correr, pero vigila tu barra de energía.",
            "Tip: si un juguete va hacia la puerta, la puerta se ilumina en rojo.",
            "Tip: los bastones de dulce te dan súper velocidad por 5 segundos.",
            "Tip: el Cascanueces es lento al patrullar, pero rápido cuando te ve.",
            "Tip: acorrala a los juguetes contra las paredes para atraparlos.",
            "Tip: en el hielo es difícil frenar. ¡Cuidado con las curvas!",
            "Tip: si se escapan 3 juguetes, la Navidad estará en peligro.",
            "Tip: termina rápido y sin escapes para ganar 3 estrellas."
        };

        PrefabUtility.SaveAsPrefabAsset(c.gameObject, "Assets/Resources/PantallaCarga.prefab");
        Object.DestroyImmediate(c.gameObject);
    }
}
