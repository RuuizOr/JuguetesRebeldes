using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static partial class ConstructorJuego
{
    static Material matContorno, matContornoJuguete, matSombra, matEscudo, matCostal;
    static GameObject prefabBolaNieve, efectoNieve;
    static readonly Dictionary<TipoPowerUp, GameObject> prefabsPowerUp = new Dictionary<TipoPowerUp, GameObject>();

    static void CrearMaterialesExtra()
    {
        matContorno = CrearMaterialContorno("MatContorno", new Color(0.05f, 0.04f, 0.08f), 0.035f);
        matContornoJuguete = CrearMaterialContorno("MatContornoJuguete", new Color(1f, 0.95f, 0.75f), 0.04f);
        matSombra = CrearMaterialParticula("MatSombra", "sombra");
        matSombra.color = new Color(0f, 0f, 0f, 0.55f);
        matEscudo = CrearMaterialLit("MatEscudo", new Color(0.45f, 0.8f, 1f, 0.28f), 0.95f, new Color(0.1f, 0.35f, 0.6f), true);
        matCostal = CrearMaterialLit("MatCostal", new Color(0.55f, 0.36f, 0.2f), 0.15f);
    }

    static Material CrearMaterialContorno(string nombre, Color color, float grosor)
    {
        string ruta = RutaMateriales + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("JuguetesRebeldes/Contorno");
            if (sh == null) { Debug.LogWarning("Shader de contorno no encontrado"); return null; }
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.SetColor("_ColorContorno", color);
        m.SetFloat("_Grosor", grosor);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void AgregarContorno(GameObject go, Material mat)
    {
        if (mat == null) return;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
            if (r.GetComponent<TextMesh>() != null) continue;
            List<Material> mats = r.sharedMaterials.ToList();
            if (mats.Contains(mat)) continue;
            mats.Add(mat);
            r.sharedMaterials = mats.ToArray();
        }
    }

    static GameObject Sombra(Transform padre, float tam)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Sombra";
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false);
        q.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(tam, tam, 1f);
        MeshRenderer mr = q.GetComponent<MeshRenderer>();
        mr.sharedMaterial = matSombra;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return q;
    }

    static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g;
    }

    static GameObject CuboHielo(Transform padre, Vector3 tam)
    {
        GameObject c = Primitiva(PrimitiveType.Cube, "CuboHielo", padre, new Vector3(0f, tam.y / 2f, 0f), tam, matHielo);
        c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        c.SetActive(false);
        return c;
    }

    // ---------------- Proyectil y power-ups ----------------

    static void CrearPrefabsPowerUps()
    {
        efectoNieve = CrearEfectoBurst("EfectoNieve", Color.white, new Color(0.7f, 0.9f, 1f), 30, 2f, 5f, 0.6f, 0.3f, 1f);
        GameObject efectoEscudo = CrearEfectoBurst("EfectoEscudo", new Color(0.5f, 0.85f, 1f), Color.white, 40, 3f, 7f, 0.6f, 0.35f, 0f);

        // Bola de nieve
        GameObject b = Primitiva(PrimitiveType.Sphere, "BolaNieve", null, Vector3.zero, Vector3.one * 0.45f, matNieve);
        b.transform.SetParent(null);
        SphereCollider sc = b.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.8f;
        Rigidbody rb = b.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        BolaNieve bn = b.AddComponent<BolaNieve>();
        bn.efectoImpacto = efectoNieve;
        TrailRenderer tr = b.AddComponent<TrailRenderer>();
        tr.time = 0.25f;
        tr.startWidth = 0.35f;
        tr.endWidth = 0f;
        tr.sharedMaterial = matParticula;
        tr.startColor = new Color(1f, 1f, 1f, 0.8f);
        tr.endColor = new Color(0.7f, 0.9f, 1f, 0f);
        prefabBolaNieve = GuardarPrefab(b, "BolaNieve");

        prefabsPowerUp.Clear();
        prefabsPowerUp[TipoPowerUp.Velocidad] = CrearPrefabPowerUp(TipoPowerUp.Velocidad, "candy-cane-red", 1.3f, new Color(1f, 0.5f, 0.6f), "Velocidad", 5f, 0);
        prefabsPowerUp[TipoPowerUp.BolaNieve] = CrearPrefabPowerUp(TipoPowerUp.BolaNieve, "snowflake-a", 1.1f, new Color(0.6f, 0.85f, 1f), "Bolas de nieve", 0f, 3);
        prefabsPowerUp[TipoPowerUp.Iman] = CrearPrefabPowerUp(TipoPowerUp.Iman, "present-a-round", 1f, new Color(0.8f, 0.5f, 1f), "Imán", 4f, 0);
        prefabsPowerUp[TipoPowerUp.Escudo] = CrearPrefabPowerUp(TipoPowerUp.Escudo, "wreath-decorated", 1.1f, new Color(0.45f, 0.8f, 1f), "Escudo", 0f, 0);

        efectoEscudoRotoPrefab = efectoEscudo;
    }

    static GameObject efectoEscudoRotoPrefab;

    static GameObject CrearPrefabPowerUp(TipoPowerUp tipo, string modelo, float altura, Color color, string etiqueta, float duracion, int cantidad)
    {
        GameObject root = new GameObject("PowerUp_" + tipo);
        SphereCollider sc = root.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.9f;
        sc.center = new Vector3(0f, 0.9f, 0f);
        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        PowerUp pu = root.AddComponent<PowerUp>();
        pu.tipo = tipo;
        pu.duracion = duracion > 0f ? duracion : 5f;
        pu.cantidad = cantidad > 0 ? cantidad : 3;
        pu.efecto = efectoPowerUp;
        pu.reaparecer = tipo == TipoPowerUp.Iman ? 50f : tipo == TipoPowerUp.Velocidad ? 20f : 30f;

        GameObject visual = Grupo("Visual", root.transform);
        pu.visual = visual;
        GameObject giro = Grupo("Giro", visual.transform);
        giro.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        Girar g = giro.AddComponent<Girar>();
        g.velocidad = 110f;
        g.amplitud = 0.2f;
        GameObject m = ModeloEstatico(modelo, altura, giro.transform);
        AgregarContorno(m, matContornoJuguete);
        Luz("Brillo", Vector3.zero, color, 2.5f, 4.5f, visual.transform).transform.localPosition = new Vector3(0f, 1f, 0f);
        Sombra(visual.transform, 1.2f);

        GameObject et = new GameObject("Etiqueta");
        et.transform.SetParent(visual.transform, false);
        et.transform.localPosition = new Vector3(0f, altura + 1f, 0f);
        TextMesh tm = et.AddComponent<TextMesh>();
        tm.font = fuente;
        tm.text = etiqueta;
        tm.fontSize = 56;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        et.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        et.AddComponent<Billboard>();

        IgnorarNavMesh(root);
        return GuardarPrefab(root, "PowerUp_" + tipo);
    }

    static void PonerPowerUps(Transform padre, params (TipoPowerUp tipo, Vector3 pos)[] lista)
    {
        foreach (var (tipo, pos) in lista)
        {
            GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(prefabsPowerUp[tipo], padre);
            p.transform.position = pos;
        }
    }

    // ---------------- Extras del jugador ----------------

    static void ExtrasJugador(GameObject root, DuendeController dc)
    {
        AgregarContorno(root, matContorno);
        Sombra(root.transform, 1.6f);

        // Costal en la espalda
        GameObject costal = Grupo("Costal", root.transform);
        costal.transform.localPosition = new Vector3(0f, 0.95f, -0.78f);
        Primitiva(PrimitiveType.Sphere, "Bolsa", costal.transform, Vector3.zero, new Vector3(0.75f, 0.85f, 0.6f), matCostal);
        Primitiva(PrimitiveType.Cylinder, "Cuello", costal.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.22f, 0.1f, 0.22f), matCostal);
        Primitiva(PrimitiveType.Cylinder, "Lazo", costal.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.26f, 0.03f, 0.26f),
            CrearMaterialLit("MatLazo", new Color(0.85f, 0.15f, 0.15f), 0.4f));
        AgregarContorno(costal, matContorno);
        dc.costal = costal.transform;

        // Escudo
        GameObject burbuja = Primitiva(PrimitiveType.Sphere, "BurbujaEscudo", root.transform, new Vector3(0f, 0.95f, 0f), Vector3.one * 2.4f, matEscudo);
        burbuja.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        burbuja.SetActive(false);
        dc.burbujaEscudo = burbuja;
        dc.efectoEscudoRoto = efectoEscudoRotoPrefab;

        // Imán
        GameObject im = new GameObject("EfectoIman");
        im.transform.SetParent(root.transform, false);
        im.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        im.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem ps = im.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        im.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = 1.8f;
        main.startSpeed = -2.6f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.5f, 1f), new Color(1f, 0.6f, 0.9f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission;
        em.rateOverTime = 70f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 5f;
        dc.efectoIman = ps;

        dc.prefabBolaNieve = prefabBolaNieve;
    }

    // ---------------- Santa (hecho con bloques, estilo Kenney) ----------------

    static GameObject CrearSanta(Transform padre)
    {
        Material rojo = CrearMaterialLit("MatSantaRojo", new Color(0.82f, 0.1f, 0.12f), 0.3f);
        Material blanco = CrearMaterialLit("MatSantaBlanco", new Color(0.96f, 0.96f, 0.98f), 0.2f);
        Material piel = CrearMaterialLit("MatSantaPiel", new Color(1f, 0.78f, 0.62f), 0.2f);
        Material negro = CrearMaterialLit("MatSantaNegro", new Color(0.08f, 0.08f, 0.1f), 0.4f);
        Material oro = CrearMaterialLit("MatSantaOro", new Color(1f, 0.8f, 0.25f), 0.7f);

        GameObject s = Grupo("Santa", padre);
        Transform t = s.transform;
        Primitiva(PrimitiveType.Cube, "Cuerpo", t, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.8f, 0.7f), rojo);
        Primitiva(PrimitiveType.Cube, "Cinturon", t, new Vector3(0f, 0.4f, 0f), new Vector3(0.94f, 0.14f, 0.74f), negro);
        Primitiva(PrimitiveType.Cube, "Hebilla", t, new Vector3(0f, 0.4f, 0.37f), new Vector3(0.2f, 0.14f, 0.04f), oro);
        Primitiva(PrimitiveType.Cube, "BrazoIzq", t, new Vector3(-0.55f, 0.65f, 0.1f), new Vector3(0.22f, 0.6f, 0.25f), rojo);
        Primitiva(PrimitiveType.Cube, "BrazoDer", t, new Vector3(0.55f, 0.65f, 0.1f), new Vector3(0.22f, 0.6f, 0.25f), rojo);
        Primitiva(PrimitiveType.Cube, "Cabeza", t, new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 0.55f, 0.55f), piel);
        Primitiva(PrimitiveType.Cube, "Barba", t, new Vector3(0f, 1.08f, 0.24f), new Vector3(0.6f, 0.4f, 0.15f), blanco);
        Primitiva(PrimitiveType.Cube, "Bigote", t, new Vector3(0f, 1.2f, 0.3f), new Vector3(0.36f, 0.08f, 0.08f), blanco);
        Primitiva(PrimitiveType.Cube, "OjoIzq", t, new Vector3(-0.13f, 1.33f, 0.28f), new Vector3(0.07f, 0.09f, 0.02f), negro);
        Primitiva(PrimitiveType.Cube, "OjoDer", t, new Vector3(0.13f, 1.33f, 0.28f), new Vector3(0.07f, 0.09f, 0.02f), negro);
        Primitiva(PrimitiveType.Cube, "Borde", t, new Vector3(0f, 1.55f, 0f), new Vector3(0.62f, 0.12f, 0.62f), blanco);
        Primitiva(PrimitiveType.Cube, "Gorro", t, new Vector3(0f, 1.75f, -0.05f), new Vector3(0.48f, 0.35f, 0.48f), rojo);
        Primitiva(PrimitiveType.Cube, "Punta", t, new Vector3(0f, 1.9f, -0.25f), new Vector3(0.26f, 0.2f, 0.3f), rojo);
        Primitiva(PrimitiveType.Sphere, "Pompon", t, new Vector3(0f, 1.9f, -0.45f), Vector3.one * 0.2f, blanco);
        AgregarContorno(s, matContorno);
        return s;
    }

    // =====================================================================
    // Escena final
    // =====================================================================

    static void Casita(Vector3 pos, float rotY, float esc, Transform padre, System.Random r)
    {
        GameObject casa = Grupo("Casita", padre);
        casa.transform.position = pos;
        casa.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        string[] frente = { "cabin-door-rotate", "cabin-window-a", "cabin-wall-wreath" };
        Poner(frente[r.Next(frente.Length)], pos, rotY + 180f, esc, casa.transform, false, false);
        Poner("cabin-window-b", pos, rotY + 90f, esc, casa.transform, false, false);
        Poner("cabin-window-b", pos, rotY - 90f, esc, casa.transform, false, false);
        Poner("cabin-wall", pos, rotY, esc, casa.transform, false, false);
        Poner("cabin-roof-snow-point", pos + Vector3.up * S * esc, rotY + 90f, esc, casa.transform, false, false);
        Luz("LuzVentana", pos + Vector3.up * 1.3f, LuzCalida, 2.5f, 5f, casa.transform, true);
    }

    static void ConstruirFinal()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.Random r = new System.Random(21);
        Camera cam = PrepararCamara(new Color(0.05f, 0.07f, 0.15f));
        cam.farClipPlane = 600f;
        Iluminacion(true);
        RenderSettings.fogDensity = 0.006f;
        PostProceso();

        Transform deco = Grupo("Mundo").transform;
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloNieve";
        suelo.transform.position = new Vector3(0f, 0f, 80f);
        suelo.transform.localScale = new Vector3(30f, 1f, 40f);
        suelo.GetComponent<Renderer>().sharedMaterial = matNieve;
        Object.DestroyImmediate(suelo.GetComponent<MeshCollider>());

        string[] pinos = { "tree-snow-a", "tree-snow-b", "tree-snow-c" };
        for (int i = 0; i < 160; i++)
        {
            float x = (float)(r.NextDouble() - 0.5) * 140f;
            float z = -20f + (float)r.NextDouble() * 200f;
            if (Mathf.Abs(x) < 17f && z > -12f) continue;
            Poner(pinos[r.Next(3)], new Vector3(x, 0f, z), r.Next(0, 360), 1.5f + (float)r.NextDouble() * 1.5f, deco);
        }

        // Plaza del taller
        Poner("tree-decorated-snow", new Vector3(7f, 0f, -6f), 0f, 1.6f, deco);
        AnilloRegalos(new Vector3(7f, 0f, -6f), 2f, 7, deco, r, 1.1f);
        Luz("LuzArbol", new Vector3(7f, 4f, -7.5f), new Color(1f, 0.6f, 0.35f), 4f, 10f, deco, true);
        foreach (Vector3 p in new[] { new Vector3(-5f, 0f, -2f), new Vector3(4f, 0f, -1f), new Vector3(-6f, 0f, 6f), new Vector3(6f, 0f, 7f) })
        {
            Poner("lantern", p, 0f, 1f, deco);
            Luz("LuzFarol", p + Vector3.up * 3.7f, LuzCalida, 3f, 9f, deco, true);
        }
        Casita(new Vector3(-9f, 0f, -7f), 30f, 1.6f, deco, r);

        // Pueblo
        float[] zs = { 38f, 52f, 66f, 80f, 94f };
        for (int i = 0; i < zs.Length; i++)
        {
            Casita(new Vector3(-7f - (i % 2) * 5f, 0f, zs[i]), 90f, 1.4f, deco, r);
            Casita(new Vector3(7f + ((i + 1) % 2) * 5f, 0f, zs[i] + 6f), -90f, 1.4f, deco, r);
        }

        // Luna y estrellas
        Material luna = CrearMaterialLit("MatLuna", new Color(1f, 0.97f, 0.85f), 0.1f, new Color(1.6f, 1.5f, 1.2f));
        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Luna";
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        moon.transform.position = new Vector3(-6f, 60f, 290f);
        moon.transform.localScale = Vector3.one * 55f;
        moon.GetComponent<Renderer>().sharedMaterial = luna;

        GameObject est = new GameObject("Estrellas");
        est.transform.position = new Vector3(0f, 120f, 200f);
        ParticleSystem eps = est.AddComponent<ParticleSystem>();
        est.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;
        var em = eps.main;
        em.loop = true;
        em.prewarm = true;
        em.startLifetime = 1000f;
        em.startSpeed = 0f;
        em.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        em.maxParticles = 600;
        em.simulationSpace = ParticleSystemSimulationSpace.World;
        var ee = eps.emission;
        ee.rateOverTime = 0f;
        ee.SetBursts(new[] { new ParticleSystem.Burst(0f, 500) });
        var es = eps.shape;
        es.shapeType = ParticleSystemShapeType.Box;
        es.scale = new Vector3(500f, 120f, 30f);
        CrearNieve(deco, new Vector3(0f, 14f, 0f), new Vector3(40f, 1f, 40f), 150f);

        // Trineo de Santa
        GameObject trineo = Grupo("Trineo");
        trineo.transform.position = new Vector3(0f, 0f, 2f);
        GameObject sled = Poner("sled-long", Vector3.zero, 0f, 2.2f, trineo.transform);
        sled.transform.localPosition = new Vector3(0f, 0f, 0f);
        GameObject santa = CrearSanta(trineo.transform);
        santa.transform.localPosition = new Vector3(0f, 0.6f, 0.3f);
        GameObject costal = Grupo("CostalRegalos", trineo.transform);
        costal.transform.localPosition = new Vector3(0f, 0.9f, -0.9f);
        Primitiva(PrimitiveType.Sphere, "Bolsa", costal.transform, Vector3.zero, new Vector3(1.1f, 1.1f, 0.9f), matCostal);
        GameObject regaloTop = Poner("present-a-cube", Vector3.zero, 20f, 0.8f, costal.transform);
        regaloTop.transform.localPosition = new Vector3(0.2f, 0.4f, 0f);
        for (int i = 0; i < 2; i++)
        {
            GameObject reno = Poner("reindeer", Vector3.zero, 0f, 1.7f, trineo.transform);
            reno.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 0f, 3.2f);
            GameObject reno2 = Poner("reindeer", Vector3.zero, 0f, 1.7f, trineo.transform);
            reno2.transform.localPosition = new Vector3(i == 0 ? -0.6f : 0.6f, 0f, 5.2f);
        }
        Luz("LuzTrineo", Vector3.zero, LuzCalida, 3f, 8f, trineo.transform).transform.localPosition = new Vector3(0f, 2.5f, 1f);
        GameObject estelaGo = new GameObject("Estela");
        estelaGo.transform.SetParent(trineo.transform, false);
        estelaGo.transform.localPosition = new Vector3(0f, 0.4f, -1f);
        ParticleSystem estela = estelaGo.AddComponent<ParticleSystem>();
        estela.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        estelaGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;
        var m2 = estela.main;
        m2.playOnAwake = false;
        m2.startLifetime = 1.6f;
        m2.startSpeed = 0.3f;
        m2.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
        m2.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 1f, 1f));
        m2.simulationSpace = ParticleSystemSimulationSpace.World;
        var em2 = estela.emission;
        em2.rateOverTime = 120f;
        var sh2 = estela.shape;
        sh2.shapeType = ParticleSystemShapeType.Sphere;
        sh2.radius = 0.8f;
        var sol = estela.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Ruta de vuelo
        Vector3[] puntos =
        {
            new Vector3(0f, 0f, 2f), new Vector3(0f, 0.6f, 10f), new Vector3(0f, 5f, 24f), new Vector3(0f, 10f, 40f),
            new Vector3(1f, 11f, 55f), new Vector3(-1f, 11f, 70f), new Vector3(0f, 13f, 88f), new Vector3(0f, 22f, 115f), new Vector3(-3f, 42f, 180f)
        };
        Transform rutaG = Grupo("Ruta").transform;
        Transform[] ruta = new Transform[puntos.Length];
        for (int i = 0; i < puntos.Length; i++)
        {
            GameObject p = Grupo("P" + i, rutaG);
            p.transform.position = puntos[i];
            ruta[i] = p.transform;
        }

        // Celebración
        Transform fiesta = Grupo("Celebracion").transform;
        GameObject heroe = Grupo("Heroe", fiesta);
        heroe.transform.position = new Vector3(0f, 0f, -4f);
        heroe.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject[] pjs = InstanciarTodosLosPersonajes(heroe.transform, out Animator[] anims);
        List<Saltarin> celebrantes = new List<Saltarin>();
        for (int i = 0; i < Tipos.Length; i++)
        {
            float a = (i - 3f) * 0.42f;
            Vector3 p = new Vector3(Mathf.Sin(a) * 3.2f, 0f, -4.5f + Mathf.Cos(a) * 1.4f);
            celebrantes.Add(JugueteDecorativo(Tipos[i], p, 180f + (i - 3f) * 8f, fiesta));
        }

        // UI
        Canvas c = CrearCanvas("UI_Final", 50);
        Image negroImg = Imagen("Negro", c.transform, null, Color.black);
        Estirar(negroImg.rectTransform);
        CanvasGroup gNegro = negroImg.gameObject.AddComponent<CanvasGroup>();

        Image cajaSub = Panel("Subtitulo", c.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1500f, 150f), new Color(0f, 0f, 0f, 0.6f));
        CanvasGroup gSub = cajaSub.gameObject.AddComponent<CanvasGroup>();
        Text sub = Texto("Texto", cajaSub.transform, "", 46, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1420f, 140f), false);
        sub.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new GameObject("Titulo", typeof(RectTransform));
        Estirar(Rect(titulo, c.transform));
        CanvasGroup gTit = titulo.AddComponent<CanvasGroup>();
        Image velo = Imagen("Velo", titulo.transform, null, new Color(0f, 0f, 0f, 0.35f));
        Estirar(velo.rectTransform);
        Text t1 = Texto("Feliz", titulo.transform, "<color=#E8403A>¡FELIZ</color> <color=#FFD24A>NAVIDAD!</color>", 150, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1800f, 220f));
        t1.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        t1.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
        Text t2 = Texto("Gracias", titulo.transform, "Gracias por jugar Juguetes Rebeldes", 50, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1500f, 80f));
        t2.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Text pts = Texto("Puntos", titulo.transform, "", 40, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(1500f, 70f));
        pts.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Text cont = Texto("Continuar", titulo.transform, "Espacio, clic o A para volver al menú", 36, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 60f));
        cont.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);
        Text saltar = Texto("Saltar", c.transform, "Espacio / clic / A para saltar", 26, Color.white, TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(-30f, 25f), new Vector2(700f, 50f), false);
        negroImg.transform.SetAsLastSibling();

        FinalCinematica fc = new GameObject("FinalCinematica").AddComponent<FinalCinematica>();
        fc.camara = cam;
        fc.trineo = trineo.transform;
        fc.ruta = ruta;
        fc.celebrantes = celebrantes.ToArray();
        fc.personajes = pjs;
        fc.animadoresPersonajes = anims;
        fc.modelosRegalo = Regalos.Select(CargarModelo).ToArray();
        fc.estela = estela;
        fc.subtitulo = sub;
        fc.grupoSubtitulo = gSub;
        fc.grupoTitulo = gTit;
        fc.negro = gNegro;
        fc.textoSaltar = cont;
        fc.textoPuntos = pts;
        saltar.color = new Color(1f, 1f, 1f, 0.6f);

        cam.transform.position = new Vector3(-4f, 2f, -9f);
        cam.transform.LookAt(new Vector3(0f, 1f, -4f));

        CapturarFondo("Final", new Vector3(-5f, 4f, -12f), new Vector3(0f, 2f, 5f));
        HornearYGuardar(escena, "Final", null);
    }
}
