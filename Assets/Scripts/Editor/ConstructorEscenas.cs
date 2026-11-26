using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class ConstructorJuego
{
    static readonly string[] Regalos = { "present-a-cube", "present-a-rectangle", "present-a-round", "present-b-cube", "present-b-rectangle", "present-b-round" };
    static readonly Color LuzCalida = new Color(1f, 0.72f, 0.4f);

    class Sala
    {
        public Transform raiz;
        public float x0, z0, bordeN, bordeS, bordeE, bordeO;
    }

    // =====================================================================
    // Construcción de salas
    // =====================================================================

    static Vector3 Celda(Sala s, int i, int j) => new Vector3(s.x0 + i * S, 0f, s.z0 + j * S);

    static Sala CrearSala(int W, int L, string piso, int celdaPuerta, string nombre = "Taller")
    {
        Sala s = new Sala();
        s.raiz = Grupo(nombre).transform;
        s.x0 = -(W - 1) * S / 2f;
        s.z0 = -(L - 1) * S / 2f;
        s.bordeN = s.z0 + (L - 1) * S + S / 2f;
        s.bordeS = s.z0 - S / 2f;
        s.bordeE = s.x0 + (W - 1) * S + S / 2f;
        s.bordeO = s.x0 - S / 2f;

        Transform pisos = Grupo("Piso", s.raiz).transform;
        for (int i = 0; i < W; i++)
            for (int j = 0; j < L; j++)
                Poner(piso, Celda(s, i, j), 0f, 1f, pisos, false, true);

        GameObject suelo = Grupo("SueloColision", s.raiz);
        BoxCollider bc = suelo.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.2f - 0.5f, 0f);
        bc.size = new Vector3(W * S + 2f, 1f, L * S + 2f);

        LimitesInvisibles(s.raiz, s.bordeO, s.bordeE, s.bordeS, s.bordeN);

        Transform paredes = Grupo("Paredes", s.raiz).transform;
        for (int i = 0; i < W; i++)
        {
            if (i != celdaPuerta)
            {
                string m = (i % 4 == 2) ? "cabin-window-a" : (i % 5 == 1 ? "cabin-wall-wreath" : "cabin-wall");
                Poner(m, Celda(s, i, L - 1), 0f, 1f, paredes, true, false);
            }
            Poner("cabin-wall-low", Celda(s, i, 0), 180f, 1f, paredes, true, false);
        }
        for (int j = 0; j < L; j++)
        {
            string m = (j % 3 == 1) ? "cabin-window-b" : "cabin-wall";
            Poner(m, Celda(s, 0, j), -90f, 1f, paredes, true, false);
            Poner(m, Celda(s, W - 1, j), 90f, 1f, paredes, true, false);
        }
        Poner("cabin-corner", Celda(s, 0, L - 1), 0f, 1f, paredes, false, false);
        Poner("cabin-corner", Celda(s, W - 1, L - 1), 90f, 1f, paredes, false, false);
        Poner("cabin-corner", Celda(s, W - 1, 0), 180f, 1f, paredes, false, false);
        Poner("cabin-corner", Celda(s, 0, 0), -90f, 1f, paredes, false, false);

        // Exterior visto por la puerta
        if (celdaPuerta >= 0)
        {
            Vector3 c = Celda(s, celdaPuerta, L - 1);
            Transform afuera = Grupo("Afuera", s.raiz).transform;
            for (int k = -2; k <= 2; k++)
                Poner("snow-flat-large", new Vector3(c.x + k * 3.5f, 0f, s.bordeN + 3f), 0f, 1f, afuera);
            Poner("tree-snow-a", new Vector3(c.x - 4f, 0f, s.bordeN + 6f), 0f, 1.5f, afuera);
            Poner("tree-snow-b", new Vector3(c.x + 4.5f, 0f, s.bordeN + 7f), 40f, 1.6f, afuera);
            Poner("lantern", new Vector3(c.x + 2.2f, 0f, s.bordeN + 1.5f), 0f, 1f, afuera);
        }
        return s;
    }

    static PuertaSalida CrearPuerta(Vector3 centroBorde, Vector3 haciaDentro, Transform padre, bool modelo = true)
    {
        float rotY = Quaternion.LookRotation(-haciaDentro).eulerAngles.y;
        if (modelo)
            Poner("cabin-doorway", centroBorde, rotY, 1f, padre, false, true);

        GameObject g = Grupo("PuertaSalida", padre);
        g.transform.position = centroBorde;
        g.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        PuertaSalida p = g.AddComponent<PuertaSalida>();

        GameObject punto = Grupo("PuntoEscape", g.transform);
        punto.transform.position = centroBorde + haciaDentro * 1.6f + Vector3.up * 0.2f;
        p.puntoEscape = punto.transform;
        p.radioEscape = 1.3f;

        p.luz = Luz("LuzPuerta", centroBorde + haciaDentro * 0.9f + Vector3.up * 2.4f, new Color(1f, 0.8f, 0.45f), 3f, 7f, g.transform);

        GameObject bloqueo = Grupo("Bloqueo", g.transform);
        bloqueo.transform.position = centroBorde;
        bloqueo.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        BoxCollider bc = bloqueo.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 5f, 0f);
        bc.size = new Vector3(S * 1.1f, 10f, 0.6f);

        GameObject cartel = new GameObject("Cartel");
        cartel.transform.SetParent(g.transform, false);
        cartel.transform.position = centroBorde + haciaDentro * 0.4f + Vector3.up * 3.3f;
        TextMesh tm = cartel.AddComponent<TextMesh>();
        tm.font = fuente;
        tm.text = "SALIDA";
        tm.fontSize = 64;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.35f, 0.3f);
        cartel.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        cartel.AddComponent<Billboard>();
        return p;
    }

    static void PilaRegalos(Vector3 c, int n, Transform padre, System.Random r, float esc = 1.3f)
    {
        for (int i = 0; i < n; i++)
        {
            Vector3 p = c + new Vector3((float)(r.NextDouble() - 0.5) * 2f, 0f, (float)(r.NextDouble() - 0.5) * 2f);
            Poner(Regalos[r.Next(Regalos.Length)], p, r.Next(0, 360), esc * (0.8f + (float)r.NextDouble() * 0.4f), padre, true);
        }
    }

    static void AnilloRegalos(Vector3 c, float radio, int n, Transform padre, System.Random r, float esc = 1.3f)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio;
            Poner(Regalos[r.Next(Regalos.Length)], p, r.Next(0, 360), esc * (0.8f + (float)r.NextDouble() * 0.4f), padre, true);
        }
    }

    static void EstanteRegalos(Vector3 centro, int celdas, Transform padre, System.Random r)
    {
        GameObject e = Grupo("EstanteRegalos", padre);
        e.transform.position = centro;
        float largo = celdas * S;
        int n = celdas * 2;
        for (int k = 0; k < n; k++)
        {
            float z = -largo / 2f + (k + 0.5f) * (largo / n);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Vector3 p = centro + new Vector3(lado * 0.55f, 0f, z);
                GameObject a = Poner(Regalos[r.Next(Regalos.Length)], p, r.Next(0, 4) * 90f, 1.25f, e.transform);
                if (r.NextDouble() < 0.55)
                {
                    float top = BoundsMalla(a).max.y;
                    Poner(Regalos[r.Next(Regalos.Length)], p + new Vector3(0f, top, 0f), r.Next(0, 4) * 90f, 1.05f, e.transform);
                }
            }
        }
        BoxCollider bc = e.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.8f, 0f);
        bc.size = new Vector3(S * 1.05f, 3.6f, largo);
    }

    static void Farol(Vector3 pos, Transform padre, float intensidad = 3f)
    {
        Poner("lantern", pos, 0f, 1f, padre, true);
        Luz("LuzFarol", pos + Vector3.up * 3.7f, LuzCalida, intensidad, 9f, padre, true);
    }

    static GameObject PonerJugador(Vector3 pos, float rotY = 0f)
    {
        GameObject j = (GameObject)PrefabUtility.InstantiatePrefab(prefabDuende);
        j.transform.position = pos;
        j.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        return j;
    }

    static void PonerJuguetes(Vector3[] posiciones, string[] tipos, Vector2 intento, Transform padre)
    {
        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject j = (GameObject)PrefabUtility.InstantiatePrefab(prefabsJuguete[tipos[i % tipos.Length]], padre);
            j.transform.position = posiciones[i];
            j.transform.rotation = Quaternion.Euler(0f, i * 73f, 0f);
            j.GetComponent<Juguete>().tiempoPrimerIntento = intento + new Vector2(i * 1.5f, i * 1.5f);
        }
    }

    static void PonerCascanueces(Vector3[] puntos, Transform jugador, string nombre, Transform padre)
    {
        Transform grupoPuntos = Grupo("Puntos_" + nombre, padre).transform;
        Transform[] ps = new Transform[puntos.Length];
        for (int i = 0; i < puntos.Length; i++)
        {
            GameObject p = Grupo("Punto" + (i + 1).ToString("00"), grupoPuntos);
            p.transform.position = puntos[i];
            ps[i] = p.transform;
        }
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefabCascanueces, padre);
        c.name = nombre;
        c.transform.position = puntos[0];
        Cascanueces cn = c.GetComponent<Cascanueces>();
        cn.jugador = jugador;
        cn.puntosPatrulla = ps;
    }

    // =====================================================================
    // Niveles
    // =====================================================================

    static void ConstruirNivel(int n)
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.Random r = new System.Random(100 + n);
        Camera cam = PrepararCamara(new Color(0.06f, 0.08f, 0.16f));
        Iluminacion(n == 3);
        PostProceso();

        GameObject jugador;
        NivelManager nm = new GameObject("NivelManager").AddComponent<NivelManager>();
        nm.numeroNivel = n;
        Transform deco = Grupo("Decoracion").transform;
        Transform actores = Grupo("Actores").transform;

        if (n == 1)
        {
            nm.nombreNivel = "El Taller de Juguetes";
            nm.tiempoLimite = 150f;
            nm.centroNivel = Vector3.zero;
            nm.radioNivel = 11f;

            Sala s = CrearSala(11, 9, "floor-wood", 5);
            CrearExteriorNevado(s.raiz, s.bordeO, s.bordeE, s.bordeS, s.bordeN, r);
            CrearPuerta(new Vector3(Celda(s, 5, 0).x, 0f, s.bordeN), Vector3.back, s.raiz);

            Poner("tree-decorated", new Vector3(0f, 0.2f, 2f), 0f, 1.05f, deco, true);
            AnilloRegalos(new Vector3(0f, 0.2f, 2f), 2.1f, 8, deco, r);
            Luz("LuzArbol", new Vector3(0f, 3.5f, 0.5f), new Color(1f, 0.6f, 0.3f), 4f, 10f, deco, true);

            foreach (Vector3 p in new[] { new Vector3(-8f, 0.2f, 5f), new Vector3(-8f, 0.2f, -3f), new Vector3(8f, 0.2f, 5f), new Vector3(8f, 0.2f, -3f) })
            {
                GameObject mesa = Poner("bench", p, 90f, 1.05f, deco, true);
                float top = BoundsMalla(mesa).max.y;
                Poner(Regalos[r.Next(Regalos.Length)], p + new Vector3(0f, top - 0.2f, -0.6f), r.Next(0, 360), 0.7f, deco);
                Poner(r.NextDouble() < 0.5 ? "gingerbread-man" : "snowman", p + new Vector3(0f, top - 0.2f, 0.6f), 180f, 0.6f, deco);
            }

            PilaRegalos(new Vector3(-11f, 0.2f, 8.5f), 4, deco, r);
            PilaRegalos(new Vector3(11f, 0.2f, 8.5f), 4, deco, r);
            PilaRegalos(new Vector3(-11f, 0.2f, -8.5f), 3, deco, r);

            string[] tren = { "train-locomotive", "train-tender", "train-wagon", "train-wagon-logs" };
            for (int i = 0; i < tren.Length; i++)
                Poner(tren[i], new Vector3(10f - i * 2.1f, 0.2f, -8.6f), 0f, 2.2f, deco, true);
            Poner("sled", new Vector3(-6f, 0.2f, -8f), 30f, 1.3f, deco, true);

            Farol(new Vector3(-12.3f, 0.2f, 10f), deco);
            Farol(new Vector3(12.3f, 0.2f, 10f), deco);
            Farol(new Vector3(-12.3f, 0.2f, -10f), deco);
            Farol(new Vector3(12.3f, 0.2f, -10f), deco, 2.5f);
            Luz("LuzCentro", new Vector3(0f, 6f, -4f), LuzCalida, 3f, 16f, deco);

            for (int i = -1; i <= 1; i += 2)
            {
                Poner("lights-colored", new Vector3(i * 5f, 2.0f, s.bordeN - 0.45f), 180f, 2.5f, deco);
                Poner("sock-red-cane", new Vector3(s.bordeO + 0.45f, 0.9f, i * 4f), 90f, 1.2f, deco);
                Poner("sock-green", new Vector3(s.bordeE - 0.45f, 0.9f, i * 4f), -90f, 1.2f, deco);
            }

            jugador = PonerJugador(new Vector3(0f, 0.25f, -8f));
            PonerJuguetes(new[]
            {
                new Vector3(-6f, 0.2f, 8f), new Vector3(6f, 0.2f, 8f), new Vector3(-10f, 0.2f, 0f),
                new Vector3(10.5f, 0.2f, 1f), new Vector3(-3f, 0.2f, -3f)
            }, new[] { "jengibre", "galleta", "reno", "robot", "tren" }, new Vector2(50f, 95f), actores);
            PonerPowerUps(actores, (TipoPowerUp.Velocidad, new Vector3(11f, 0.2f, -6f)), (TipoPowerUp.BolaNieve, new Vector3(-11f, 0.2f, -2f)),
                (TipoPowerUp.Escudo, new Vector3(2f, 0.2f, -5.5f)), (TipoPowerUp.Iman, new Vector3(-4f, 0.2f, 8.5f)));
            tamanoMapa = 31f;
        }
        else if (n == 2)
        {
            nm.nombreNivel = "La Bodega de Regalos";
            nm.tiempoLimite = 180f;
            nm.centroNivel = Vector3.zero;
            nm.radioNivel = 14f;

            Sala s = CrearSala(14, 12, "floor-stone", 7);
            CrearExteriorNevado(s.raiz, s.bordeO, s.bordeE, s.bordeS, s.bordeN, r);
            float xPuerta = Celda(s, 7, 0).x;
            CrearPuerta(new Vector3(xPuerta, 0f, s.bordeN), Vector3.back, s.raiz);

            foreach (float x in new[] { -11f, -4f, 4f, 11f })
                foreach (float z in new[] { -5.5f, 5.5f })
                    EstanteRegalos(new Vector3(x, 0.2f, z), 3, deco, r);

            PilaRegalos(new Vector3(-15.5f, 0.2f, 13f), 4, deco, r);
            PilaRegalos(new Vector3(15.5f, 0.2f, -13f), 4, deco, r);
            Poner("sled-long", new Vector3(15.6f, 0.2f, 12.5f), 0f, 1.3f, deco, true);
            Poner("sled", new Vector3(-15.6f, 0.2f, -12.5f), 90f, 1.3f, deco, true);

            foreach (float x in new[] { -7.5f, 7.5f })
                foreach (float z in new[] { -10f, 0f, 10f })
                    Luz("LuzTecho", new Vector3(x, 6f, z), LuzCalida, 3.5f, 13f, deco, true);
            Farol(new Vector3(xPuerta - 2.8f, 0.2f, s.bordeN - 1.2f), deco);
            Farol(new Vector3(xPuerta + 2.8f, 0.2f, s.bordeN - 1.2f), deco);

            jugador = PonerJugador(new Vector3(0f, 0.25f, -12.5f));
            PonerJuguetes(new[]
            {
                new Vector3(-14.5f, 0.2f, 8f), new Vector3(14.5f, 0.2f, -8f), new Vector3(-7.5f, 0.2f, -12f),
                new Vector3(7.5f, 0.2f, 12f), new Vector3(-7.5f, 0.2f, 5f), new Vector3(7.5f, 0.2f, -5f),
                new Vector3(-14.5f, 0.2f, -6f), new Vector3(14.5f, 0.2f, 6f)
            }, new[] { "reno", "soldado", "jengibre", "nieve", "robot", "galleta", "tren", "soldado" }, new Vector2(38f, 70f), actores);

            PonerCascanueces(new[]
            {
                new Vector3(1f, 0.2f, 12f), new Vector3(14.5f, 0.2f, 12f), new Vector3(14.5f, 0.2f, 0f),
                new Vector3(7.5f, 0.2f, 0f), new Vector3(-7.5f, 0.2f, 0f), new Vector3(-14.5f, 0.2f, 0f), new Vector3(-14.5f, 0.2f, 12f)
            }, jugador.transform, "Cascanueces", actores);

            PonerPowerUps(actores, (TipoPowerUp.Velocidad, new Vector3(-14.5f, 0.2f, -13f)), (TipoPowerUp.Velocidad, new Vector3(14.5f, 0.2f, 13f)),
                (TipoPowerUp.BolaNieve, new Vector3(0f, 0.2f, -7f)), (TipoPowerUp.BolaNieve, new Vector3(0f, 0.2f, 8f)),
                (TipoPowerUp.Escudo, new Vector3(-7.5f, 0.2f, -12.5f)), (TipoPowerUp.Iman, new Vector3(-14.5f, 0.2f, 3f)));
            tamanoMapa = 39f;
        }
        else
        {
            nm.nombreNivel = "El Patio Nevado";
            nm.tiempoLimite = 210f;
            nm.centroNivel = Vector3.zero;
            nm.radioNivel = 17f;

            // Suelo nevado
            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "SueloNieve";
            suelo.transform.localScale = new Vector3(7f, 1f, 6.5f);
            suelo.GetComponent<Renderer>().sharedMaterial = matNieve;
            Object.DestroyImmediate(suelo.GetComponent<MeshCollider>());
            BoxCollider bs = suelo.AddComponent<BoxCollider>();
            bs.center = new Vector3(0f, -0.5f / 1f, 0f);
            bs.size = new Vector3(10f, 1f, 10f);

            for (int i = 0; i < 26; i++)
            {
                Vector3 p = new Vector3((float)(r.NextDouble() - 0.5) * 38f, 0f, (float)(r.NextDouble() - 0.5) * 33f);
                Poner(r.NextDouble() < 0.6 ? "snow-flat-large" : "snow-pile", p, r.Next(0, 360), 1f, deco);
            }

            // Límites: bosque + paredes invisibles
            Transform bosque = Grupo("Bosque", deco).transform;
            string[] pinos = { "tree-snow-a", "tree-snow-b", "tree-snow-c" };
            for (float x = -21f; x <= 21.1f; x += 3f)
            {
                if (Mathf.Abs(x) >= 3f)
                    Poner(pinos[r.Next(3)], new Vector3(x, 0f, 18.8f), r.Next(0, 360), 1.4f + (float)r.NextDouble() * 0.5f, bosque, true);
                // Borde sur bajo para no tapar la cámara
                Poner("snow-pile", new Vector3(x, 0f, -18.6f), r.Next(0, 360), 2.2f, bosque);
                if (Mathf.Abs(x) < 20f && (int)(x + 21f) % 6 == 0)
                    Poner("lantern", new Vector3(x + 1.5f, 0f, -18.4f), 0f, 0.8f, bosque);
            }
            for (float z = -12.8f; z <= 15.9f; z += 3f)
                foreach (float x in new[] { -21.3f, 21.3f })
                    if (!(x < 0f && Mathf.Abs(z) < 3f))
                        Poner(pinos[r.Next(3)], new Vector3(x, 0f, z), r.Next(0, 360), 1.4f + (float)r.NextDouble() * 0.5f, bosque, true);
            for (int i = 0; i < 18; i++)
            {
                Vector3 p = new Vector3((float)(r.NextDouble() - 0.5) * 60f, 0f, 22f + (float)r.NextDouble() * 10f);
                Poner(pinos[r.Next(3)], p, r.Next(0, 360), 1.8f, bosque);
                Poner(pinos[r.Next(3)], new Vector3(-24f - (float)r.NextDouble() * 10f, 0f, (float)(r.NextDouble() - 0.5) * 50f), r.Next(0, 360), 1.8f, bosque);
                Poner(pinos[r.Next(3)], new Vector3(24f + (float)r.NextDouble() * 10f, 0f, (float)(r.NextDouble() - 0.5) * 50f), r.Next(0, 360), 1.8f, bosque);
            }
            GameObject limites = Grupo("Limites");
            foreach (var (c, t) in new[]
            {
                (new Vector3(0f, 5f, 18.3f), new Vector3(46f, 10f, 1.2f)), (new Vector3(0f, 5f, -18.3f), new Vector3(46f, 10f, 1.2f)),
                (new Vector3(20.8f, 5f, 0f), new Vector3(1.2f, 10f, 40f)), (new Vector3(-20.8f, 5f, 0f), new Vector3(1.2f, 10f, 40f))
            })
            {
                BoxCollider b = limites.AddComponent<BoxCollider>();
                b.center = c;
                b.size = t;
            }

            Transform puertas = Grupo("Puertas").transform;
            CrearPuerta(new Vector3(0f, 0f, 17.8f), Vector3.back, puertas);
            CrearPuerta(new Vector3(-20.3f, 0f, 0f), Vector3.right, puertas);

            Poner("tree-decorated-snow", new Vector3(0f, 0f, 3f), 0f, 1.35f, deco, true);
            AnilloRegalos(new Vector3(0f, 0f, 3f), 2.6f, 9, deco, r, 1.3f);
            Luz("LuzArbol", new Vector3(0f, 4.5f, 1f), new Color(1f, 0.55f, 0.35f), 5f, 12f, deco, true);

            // Lagos congelados
            Transform hielo = Grupo("Hielo").transform;
            foreach (var (c, t) in new[] { (new Vector3(-9f, 0f, -7f), new Vector2(8f, 6f)), (new Vector3(10f, 0f, 7f), new Vector2(7f, 7f)), (new Vector3(9f, 0f, -9f), new Vector2(6f, 5f)) })
            {
                GameObject h = GameObject.CreatePrimitive(PrimitiveType.Cube);
                h.name = "LagoCongelado";
                h.transform.SetParent(hielo, false);
                h.transform.position = c + Vector3.up * 0.03f;
                h.transform.localScale = new Vector3(t.x, 0.06f, t.y);
                h.GetComponent<Renderer>().sharedMaterial = matHielo;
                Object.DestroyImmediate(h.GetComponent<BoxCollider>());
                GameObject zona = Grupo("ZonaHielo", h.transform);
                zona.transform.localPosition = Vector3.zero;
                BoxCollider bz = zona.AddComponent<BoxCollider>();
                bz.isTrigger = true;
                bz.center = new Vector3(0f, 15f, 0f);
                bz.size = new Vector3(1f, 40f, 1f);
                Rigidbody rb = zona.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                zona.AddComponent<ZonaHielo>();
                IgnorarNavMesh(h);
            }

            foreach (Vector3 p in new[] { new Vector3(-14f, 0f, 12f), new Vector3(14f, 0f, -14f), new Vector3(-5f, 0f, 12.5f), new Vector3(16f, 0f, 2f) })
                Poner("snowman-hat", p, r.Next(0, 360), 1.4f, deco, true);
            Poner("rocks-large", new Vector3(-16.5f, 0f, -14f), 0f, 0.8f, deco, true);
            Poner("rocks-medium", new Vector3(17f, 0f, 14f), 0f, 1f, deco, true);
            Poner("bench", new Vector3(6f, 0f, 13.5f), 0f, 1f, deco, true);
            Poner("sled-long", new Vector3(-4f, 0f, -12f), 20f, 1.3f, deco, true);
            PilaRegalos(new Vector3(-4f, 0f, -10f), 3, deco, r, 1.1f);
            string[] tren = { "train-locomotive", "train-tender", "train-wagon", "train-wagon-flat" };
            for (int i = 0; i < tren.Length; i++)
                Poner(tren[i], new Vector3(-11f, 0f, 2f + i * 2.1f), 90f, 2.2f, deco, true);

            foreach (Vector3 p in new[] { new Vector3(-6f, 0f, 10f), new Vector3(6f, 0f, 10f), new Vector3(-13f, 0f, -3f), new Vector3(13f, 0f, -3f), new Vector3(0f, 0f, -11f), new Vector3(-2.5f, 0f, 15.5f), new Vector3(2.5f, 0f, 15.5f), new Vector3(-18f, 0f, -2.5f), new Vector3(-18f, 0f, 2.5f) })
                Farol(p, deco, 3.5f);

            CrearNieve(deco, new Vector3(0f, 9.5f, 0f), new Vector3(55f, 1f, 50f), 300f);

            jugador = PonerJugador(new Vector3(0f, 0.1f, -13.5f));
            PonerJuguetes(new[]
            {
                new Vector3(-15f, 0f, 5f), new Vector3(15f, 0f, -3f), new Vector3(-7f, 0f, 0f), new Vector3(7f, 0f, 2f),
                new Vector3(-3f, 0f, 9f), new Vector3(4f, 0f, 10f), new Vector3(-12f, 0f, -12f), new Vector3(12f, 0f, -13f),
                new Vector3(-17f, 0f, 13f), new Vector3(17f, 0f, 10f)
            }, new[] { "nieve", "reno", "robot", "jengibre", "soldado", "galleta", "tren", "reno", "nieve", "robot" }, new Vector2(30f, 60f), actores);

            PonerCascanueces(new[] { new Vector3(12f, 0f, 12f), new Vector3(17f, 0f, -5f), new Vector3(6f, 0f, -2f) }, jugador.transform, "CascanuecesEste", actores);
            PonerCascanueces(new[] { new Vector3(-12f, 0f, 12f), new Vector3(-16f, 0f, -6f), new Vector3(-6f, 0f, -2f) }, jugador.transform, "CascanuecesOeste", actores);
            PonerPowerUps(actores, (TipoPowerUp.Velocidad, new Vector3(-17f, 0f, -16f)), (TipoPowerUp.Velocidad, new Vector3(17f, 0f, -16f)),
                (TipoPowerUp.Velocidad, new Vector3(0f, 0f, 13f)), (TipoPowerUp.BolaNieve, new Vector3(-6f, 0f, -12f)), (TipoPowerUp.BolaNieve, new Vector3(5f, 0f, -14f)),
                (TipoPowerUp.Escudo, new Vector3(0f, 0f, -6f)), (TipoPowerUp.Escudo, new Vector3(15.5f, 0f, 8f)), (TipoPowerUp.Iman, new Vector3(-15f, 0f, -8f)));
            tamanoMapa = 44f;
        }

        nm.jugador = jugador.GetComponent<DuendeController>();
        CamaraSeguimiento cs = cam.gameObject.AddComponent<CamaraSeguimiento>();
        cs.objetivo = jugador.transform;
        cam.transform.position = jugador.transform.position + cs.distancia;
        cam.transform.LookAt(jugador.transform.position + Vector3.up);

        if (n == 1) CapturarFondo("Nivel1", new Vector3(0f, 17f, -18f), new Vector3(0f, 0f, 1f));
        else if (n == 2) CapturarFondo("Nivel2", new Vector3(0f, 20f, -22f), new Vector3(0f, 0f, 0f));
        else CapturarFondo("Nivel3", new Vector3(0f, 19f, -25f), new Vector3(0f, 0f, 2f));

        CrearHUD(nm);
        CrearEventSystem();

        NavMeshSurface surface = CrearNavegacion();
        HornearYGuardar(escena, "Nivel" + n, surface);
    }

    // =====================================================================
    // HUD y paneles del nivel
    // =====================================================================

    static float tamanoMapa = 32f;

    static void CrearHUD(NivelManager nm)
    {
        Canvas c = CrearCanvas("HUD", 10);
        Transform t = c.transform;
        Vector2 arribaIzq = new Vector2(0f, 1f);

        Image marcador = Panel("Marcador", t, arribaIzq, new Vector2(30f, -30f), new Vector2(470f, 240f), new Color(0.05f, 0.07f, 0.14f, 0.75f));
        Image ic1 = Imagen("IconoRegalo", marcador.transform, SpriteUI("regalo"), Color.white);
        Colocar(ic1.rectTransform, arribaIzq, new Vector2(22f, -18f), new Vector2(60f, 60f));
        Texto("LabelAtrapados", marcador.transform, "Atrapados", 30, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleLeft, arribaIzq, new Vector2(95f, -22f), new Vector2(200f, 50f));
        nm.textoAtrapados = Texto("TextoAtrapados", marcador.transform, "0 / 0", 46, Color.white, TextAnchor.MiddleRight, arribaIzq, new Vector2(270f, -16f), new Vector2(170f, 60f));

        Image ic2 = Imagen("IconoPuerta", marcador.transform, SpriteUI("puerta"), Color.white);
        Colocar(ic2.rectTransform, arribaIzq, new Vector2(22f, -92f), new Vector2(60f, 60f));
        Texto("LabelEscapados", marcador.transform, "Escapados", 30, new Color(1f, 0.75f, 0.7f), TextAnchor.MiddleLeft, arribaIzq, new Vector2(95f, -96f), new Vector2(200f, 50f));
        nm.textoEscapados = Texto("TextoEscapados", marcador.transform, "0 / 3", 46, new Color(1f, 0.6f, 0.55f), TextAnchor.MiddleRight, arribaIzq, new Vector2(270f, -90f), new Vector2(170f, 60f));

        nm.corazones = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Image h = Imagen("Corazon" + (i + 1), marcador.transform, SpriteUI("corazon"), Color.white);
            Colocar(h.rectTransform, arribaIzq, new Vector2(22f + i * 72f, -165f), new Vector2(60f, 60f));
            h.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            h.rectTransform.anchoredPosition += new Vector2(30f, -30f);
            nm.corazones[i] = h;
        }
        nm.spriteCorazon = SpriteUI("corazon");
        nm.spriteCorazonVacio = SpriteUI("corazon_vacio");

        Image reloj = Panel("Reloj", t, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(290f, 100f), new Color(0.05f, 0.07f, 0.14f, 0.75f));
        Image icr = Imagen("IconoReloj", reloj.transform, SpriteUI("reloj"), Color.white);
        Colocar(icr.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(64f, 64f));
        nm.textoTiempo = Texto("TextoTiempo", reloj.transform, "2:30", 62, Color.white, TextAnchor.MiddleCenter, new Vector2(1f, 0.5f), new Vector2(-15f, 0f), new Vector2(190f, 90f));

        nm.textoNivel = Texto("TextoNivel", t, "Nivel", 32, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(800f, 50f));
        Texto("Controles", t, "WASD/Stick mover  •  Espacio/A saltar  •  Shift/RT correr  •  E/Clic/X lanzar bola  •  Esc/Start pausa", 22, new Color(1f, 1f, 1f, 0.7f),
            TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(30f, 22f), new Vector2(1300f, 36f), false);

        // Power-ups activos
        string[] iconosPU = { "rayo", "bola_nieve", "iman", "escudo" };
        nm.slotsPowerUp = new CanvasGroup[4];
        nm.textosPowerUp = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            Image slot = Panel("PowerUp" + i, t, new Vector2(0f, 0f), new Vector2(30f + i * 100f, 70f), new Vector2(90f, 90f), new Color(0.05f, 0.07f, 0.14f, 0.8f));
            slot.raycastTarget = false;
            nm.slotsPowerUp[i] = slot.gameObject.AddComponent<CanvasGroup>();
            Image ic = Imagen("Icono", slot.transform, SpriteUI(iconosPU[i]), Color.white);
            Colocar(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(56f, 56f));
            ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            nm.textosPowerUp[i] = Texto("Texto", slot.transform, "", 24, Color.white, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-6f, 2f), new Vector2(80f, 30f));
        }

        // Puntos y combo
        nm.textoPuntos = Texto("TextoPuntos", t, "0 pts", 38, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(500f, 50f));
        Text combo = Texto("TextoCombo", t, "", 56, new Color(1f, 0.6f, 0.2f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(1200f, 160f));
        combo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        nm.textoCombo = combo;

        // Minimapa
        Image marco = Panel("Minimapa", t, new Vector2(1f, 1f), new Vector2(-25f, -80f), new Vector2(300f, 300f), new Color(0.05f, 0.07f, 0.14f, 0.85f));
        marco.raycastTarget = false;
        marco.gameObject.AddComponent<RectMask2D>();
        GameObject rawGo = new GameObject("Mapa", typeof(RectTransform), typeof(RawImage));
        RectTransform rawRt = Rect(rawGo, marco.transform);
        Estirar(rawRt);
        rawRt.offsetMin = new Vector2(8f, 8f);
        rawRt.offsetMax = new Vector2(-8f, -8f);
        RawImage raw = rawGo.GetComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = new Color(0.85f, 0.85f, 0.95f);
        MiniMapa mm = marco.gameObject.AddComponent<MiniMapa>();
        mm.fondo = raw;
        mm.contenedor = rawRt;
        mm.centro = nm.centroNivel;
        mm.tamano = tamanoMapa;
        mm.spritePunto = SpriteKnob;
        mm.spriteJugador = SpriteUI("flecha_der");
        GameObject camMapa = new GameObject("CamaraMinimapa");
        Camera cm = camMapa.AddComponent<Camera>();
        cm.clearFlags = CameraClearFlags.SolidColor;
        cm.backgroundColor = new Color(0.75f, 0.8f, 0.9f);
        cm.depth = -10f;
        cm.orthographic = true;
        cm.farClipPlane = 100f;
        UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cm).renderPostProcessing = false;
        mm.camaraMapa = cm;

        // Flechas indicadoras
        GameObject ind = new GameObject("Indicadores", typeof(RectTransform));
        RectTransform indRt = Rect(ind, t);
        Estirar(indRt);
        Indicadores indicadores = ind.AddComponent<Indicadores>();
        indicadores.area = indRt;
        indicadores.spriteFlecha = SpriteUI("flecha_der");

        // Energía
        Image energia = Panel("Energia", t, new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(560f, 70f), new Color(0.05f, 0.07f, 0.14f, 0.75f));
        Image rayo = Imagen("Rayo", energia.transform, SpriteUI("rayo"), Color.white);
        Colocar(rayo.rectTransform, new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(50f, 50f));
        Image bf = Imagen("BarraFondo", energia.transform, SpriteRedondeado, new Color(0f, 0f, 0f, 0.6f), true);
        Colocar(bf.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(455f, 30f));
        Image bar = Imagen("Barra", bf.transform, SpriteFondo, new Color(0.45f, 0.85f, 1f));
        Estirar(bar.rectTransform);
        bar.rectTransform.offsetMin = new Vector2(4f, 4f);
        bar.rectTransform.offsetMax = new Vector2(-4f, -4f);
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        nm.barraEnergia = bar;

        Text centro = Texto("TextoCentro", t, "", 140, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1600f, 400f));
        centro.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        centro.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
        nm.textoCentro = centro;

        Text aviso = Texto("TextoAviso", t, "", 42, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1200f, 70f));
        nm.textoAviso = aviso;

        // ---- Pausa ----
        RectTransform cp = CrearPanelModal("PanelPausa", t, new Vector2(640f, 600f), "Pausa", new Color(0.7f, 0.9f, 1f), out GameObject pausa);
        nm.panelPausa = pausa;
        Vector2 arriba = new Vector2(0.5f, 1f);
        nm.botonContinuar = Boton("BotonContinuar", cp, "Continuar", arriba, new Vector2(0f, -150f), new Vector2(420f, 80f), ColorBoton);
        nm.botonReiniciarPausa = Boton("BotonReiniciar", cp, "Reiniciar nivel", arriba, new Vector2(0f, -250f), new Vector2(420f, 80f), ColorBotonAzul);
        nm.botonOpcionesPausa = Boton("BotonOpciones", cp, "Opciones", arriba, new Vector2(0f, -350f), new Vector2(420f, 80f), ColorBotonAzul);
        nm.botonMenuPausa = Boton("BotonMenu", cp, "Salir al menú", arriba, new Vector2(0f, -450f), new Vector2(420f, 80f), ColorBotonRojo);

        // ---- Opciones ----
        PanelOpciones po = CrearPanelOpciones(t, out Button volver);
        nm.panelOpciones = po.gameObject;
        nm.botonVolverOpciones = volver;

        // ---- Completado ----
        string subNivel = "Nivel " + nm.numeroNivel + "  •  " + nm.nombreNivel;
        nm.resultadoCompletado = CrearPanelResultado(t, "PanelCompletado", TipoResultado.Completado, "¡NIVEL COMPLETADO!", subNivel,
            out GameObject completado, out nm.botonSiguiente, out nm.botonMenuCompletado,
            "Siguiente nivel", "play", ColorBoton, "Menú", "casa", ColorBotonAzul);
        nm.panelCompletado = completado;
        nm.estrellas = new Image[0];
        nm.spriteEstrella = SpriteUI("estrella");
        nm.spriteEstrellaVacia = SpriteUI("estrella_vacia");

        // ---- Game Over ----
        nm.resultadoGameOver = CrearPanelResultado(t, "PanelGameOver", TipoResultado.Derrota, "¡OH NO!", subNivel,
            out GameObject gameOver, out nm.botonReintentar, out nm.botonMenuGameOver,
            "Reintentar", "reintentar", ColorBoton, "Menú", "casa", ColorBotonRojo);
        nm.panelGameOver = gameOver;

        // ---- Victoria final ----
        nm.resultadoVictoria = CrearPanelResultado(t, "PanelVictoriaFinal", TipoResultado.Victoria, "¡SALVASTE LA NAVIDAD!",
            "Todos los juguetes volvieron al taller a tiempo",
            out GameObject victoria, out nm.botonVerFinal, out nm.botonMenuFinal,
            "Ver el final", "regalo", ColorBoton, "Menú principal", "casa", ColorBotonAzul);
        nm.panelVictoriaFinal = victoria;
    }

    // =====================================================================
    // Intro (cinemática)
    // =====================================================================

    static Saltarin JugueteDecorativo(TipoJuguete t, Vector3 pos, float rotY, Transform padre)
    {
        GameObject root = Grupo("Juguete_" + t.id, padre);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        GameObject m;
        if (t.animado)
        {
            RuntimeAnimatorController ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaAnimaciones + "/Juguete_" + t.id + ".controller");
            m = InstanciarPersonaje(t.modelo, t.altura, t.alturaObj, root.transform, ctrl, t.rotModelo);
        }
        else
            m = ModeloEstatico(t.modelo, t.altura, root.transform, t.rotModelo);
        Saltarin s = root.AddComponent<Saltarin>();
        s.modelo = m.transform;
        s.altura = 0.3f;
        s.frecuencia = 11f;
        s.inclinacion = 12f;
        return s;
    }

    static void ConstruirIntro()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.Random r = new System.Random(7);
        Camera cam = PrepararCamara(new Color(0.06f, 0.08f, 0.16f));
        Iluminacion(false);
        RenderSettings.ambientSkyColor *= 0.6f;
        RenderSettings.ambientEquatorColor *= 0.6f;
        PostProceso();
        Light sol = Object.FindAnyObjectByType<Light>();
        if (sol != null) { sol.intensity = 0.25f; sol.color = new Color(0.6f, 0.65f, 1f); }

        Sala s = CrearSala(9, 8, "floor-wood", 4);
        CrearExteriorNevado(s.raiz, s.bordeO, s.bordeE, s.bordeS, s.bordeN, r);
        Transform deco = Grupo("Decoracion").transform;
        Vector3 puerta = new Vector3(Celda(s, 4, 0).x, 0f, s.bordeN);
        Poner("cabin-doorway", puerta, 0f, 1f, deco);

        Poner("tree-decorated", new Vector3(0f, 0.2f, 4f), 0f, 1.6f, deco, true);
        AnilloRegalos(new Vector3(0f, 0.2f, 4f), 2.3f, 7, deco, r, 1.1f);
        PilaRegalos(new Vector3(-8f, 0.2f, 7f), 4, deco, r);
        PilaRegalos(new Vector3(7.5f, 0.2f, -6.5f), 3, deco, r);
        Poner("bench", new Vector3(-7.5f, 0.2f, -2f), 90f, 1f, deco, true);
        Poner("sled", new Vector3(-6f, 0.2f, -6.5f), 25f, 1.3f, deco, true);

        List<Light> luces = new List<Light>();
        luces.Add(Luz("LuzArbol", new Vector3(0f, 3.5f, 2.5f), new Color(1f, 0.6f, 0.3f), 4f, 10f, deco, true));
        foreach (Vector3 p in new[] { new Vector3(-9.5f, 0.2f, 8f), new Vector3(9.5f, 0.2f, 8f), new Vector3(-9.5f, 0.2f, -7.5f), new Vector3(9.5f, 0.2f, -7.5f) })
        {
            Poner("lantern", p, 0f, 1f, deco, true);
            luces.Add(Luz("LuzFarol", p + Vector3.up * 3.7f, LuzCalida, 2.5f, 9f, deco, true));
        }
        luces.Add(Luz("LuzCentro", new Vector3(0f, 5f, -3f), LuzCalida, 2f, 14f, deco));

        // Juguetes en fila
        Transform juguetes = Grupo("Juguetes").transform;
        List<Saltarin> lista = new List<Saltarin>();
        for (int i = 0; i < Tipos.Length; i++)
        {
            float x = -4.5f + i * 1.5f;
            lista.Add(JugueteDecorativo(Tipos[i], new Vector3(x, 0.2f, 0f), 180f, juguetes));
        }
        GameObject destino = Grupo("DestinoJuguetes");
        destino.transform.position = puerta + Vector3.back * 1.2f;

        // Cascanueces gigante
        GameObject cn = Grupo("CascanuecesGigante");
        cn.transform.position = new Vector3(6.5f, 0.2f, 5f);
        cn.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        ModeloEstatico("nutcracker", 3.3f, cn.transform);
        Light ojos = Luz("LuzOjos", Vector3.zero, new Color(1f, 0.15f, 0.1f), 0f, 6f, cn.transform);
        ojos.transform.localPosition = new Vector3(0f, 2.7f, 0.8f);

        // Duende
        GameObject duende = Grupo("Duende");
        duende.transform.position = new Vector3(0f, 0.2f, -6f);
        duende.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject md = InstanciarPersonaje("character-soldier", 1.85f, 0.7549f, duende.transform, ctrlDuende);
        Luz("LuzDuende", new Vector3(0f, 3f, -7.5f), new Color(0.8f, 0.9f, 1f), 2f, 6f, deco);

        // Magia
        GameObject mg = new GameObject("Magia");
        mg.transform.position = new Vector3(0f, 1.2f, 0f);
        ParticleSystem ps = mg.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        mg.GetComponent<ParticleSystemRenderer>().sharedMaterial = matParticula;
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.5f, 1f), new Color(1f, 0.9f, 0.4f));
        main.maxParticles = 800;
        var em = ps.emission;
        em.rateOverTime = 120f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(12f, 2.5f, 3f);
        var nz = ps.noise;
        nz.enabled = true;
        nz.strength = 0.6f;

        // UI
        Canvas c = CrearCanvas("UI_Intro", 50);
        Image negro = Imagen("Negro", c.transform, null, Color.black);
        Estirar(negro.rectTransform);
        CanvasGroup gNegro = negro.gameObject.AddComponent<CanvasGroup>();

        Image cajaSub = Panel("Subtitulo", c.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1500f, 150f), new Color(0f, 0f, 0f, 0.6f));
        CanvasGroup gSub = cajaSub.gameObject.AddComponent<CanvasGroup>();
        Text sub = Texto("Texto", cajaSub.transform, "", 44, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1420f, 140f), false);
        sub.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        GameObject titulo = new GameObject("Titulo", typeof(RectTransform));
        RectTransform trt = Rect(titulo, c.transform);
        Estirar(trt);
        CanvasGroup gTit = titulo.AddComponent<CanvasGroup>();
        Text t1 = Texto("Nombre", titulo.transform, "<color=#E8403A>JUGUETES</color> <color=#FFD24A>REBELDES</color>", 140, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1800f, 220f));
        t1.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        t1.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
        Text t2 = Texto("Lema", titulo.transform, "Una Navidad fuera de control", 52, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1400f, 80f));
        t2.rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Text saltar = Texto("Saltar", c.transform, "Espacio o clic para saltar la intro", 28, Color.white, TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(-30f, 25f), new Vector2(700f, 50f), false);

        negro.transform.SetAsLastSibling();

        IntroCinematica ic = new GameObject("IntroCinematica").AddComponent<IntroCinematica>();
        ic.camara = cam;
        ic.juguetes = lista.ToArray();
        ic.destinoJuguetes = destino.transform;
        ic.cascanueces = cn.transform;
        ic.luzCascanueces = ojos;
        ic.lucesTaller = luces.ToArray();
        ic.magia = ps;
        ic.duende = md.GetComponent<Animator>();
        ic.subtitulo = sub;
        ic.grupoSubtitulo = gSub;
        ic.grupoTitulo = gTit;
        ic.negro = gNegro;
        ic.textoSaltar = saltar;
        ic.tomas = new[]
        {
            Toma(new Vector3(-10f, 9f, -9f), new Vector3(-5f, 6f, -6.5f), new Vector3(0f, 2f, 3f), new Vector3(0f, 1.5f, 3f), 6f,
                "Nochebuena. En el taller de Santa todo estaba listo para el gran viaje..."),
            Toma(new Vector3(-5f, 2.2f, -3.5f), new Vector3(5f, 2.2f, -3.5f), new Vector3(-4f, 0.9f, 0f), new Vector3(4f, 0.9f, 0f), 5.5f,
                "Los juguetes esperaban en silencio a que Santa los subiera al trineo."),
            Toma(new Vector3(0f, 2.5f, -4.5f), new Vector3(0f, 3.5f, -6.5f), new Vector3(0f, 1.2f, 0f), new Vector3(0f, 1.5f, 1f), 5f,
                "Pero a la medianoche, una magia extraña recorrió el taller..."),
            Toma(new Vector3(6f, 6f, -5f), new Vector3(2f, 8f, -9f), new Vector3(0f, 1f, 1f), new Vector3(0f, 1f, 5f), 6f,
                "¡Los juguetes cobraron vida y decidieron escapar!"),
            Toma(new Vector3(5f, 1.4f, -2.5f), new Vector3(5.8f, 1.8f, -0.8f), new Vector3(6.5f, 2.2f, 5f), new Vector3(6.5f, 2.6f, 5f), 5f,
                "Y el Cascanueces gigante se convirtió en el líder de los rebeldes."),
            Toma(new Vector3(0f, 1.5f, -9.8f), new Vector3(0f, 1.3f, -8.5f), new Vector3(0f, 1.1f, -6f), new Vector3(0f, 1.1f, -6f), 5.5f,
                "Solo un duende puede devolverlos a su lugar antes del amanecer: ¡Pino!"),
        };
        cam.transform.position = ic.tomas[0].camaraDesde;
        cam.transform.LookAt(ic.tomas[0].mirarDesde);

        CapturarFondo("Intro", new Vector3(-10f, 9f, -9f), new Vector3(0f, 2f, 3f));
        HornearYGuardar(escena, "Intro", null);
    }

    static IntroCinematica.Toma Toma(Vector3 d, Vector3 h, Vector3 md, Vector3 mh, float dur, string texto)
    {
        return new IntroCinematica.Toma { camaraDesde = d, camaraHasta = h, mirarDesde = md, mirarHasta = mh, duracion = dur, texto = texto };
    }

    // =====================================================================
    // Menú principal
    // =====================================================================

    static void ConstruirMenu()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.Random r = new System.Random(3);
        Camera cam = PrepararCamara(new Color(0.06f, 0.08f, 0.16f));
        Iluminacion(true);
        PostProceso();

        Transform deco = Grupo("Diorama").transform;
        GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "SueloNieve";
        suelo.transform.localScale = new Vector3(8f, 1f, 8f);
        suelo.GetComponent<Renderer>().sharedMaterial = matNieve;
        for (int i = 0; i < 20; i++)
        {
            Vector3 p = new Vector3((float)(r.NextDouble() - 0.5) * 34f, 0f, (float)(r.NextDouble() - 0.5) * 34f);
            if (p.magnitude > 8f) Poner(r.NextDouble() < 0.6 ? "snow-flat-large" : "snow-pile", p, r.Next(0, 360), 1f, deco);
        }

        Poner("tree-decorated-snow", Vector3.zero, 0f, 3f, deco);
        AnilloRegalos(Vector3.zero, 3.2f, 10, deco, r, 1.4f);
        Luz("LuzArbol", new Vector3(0f, 5f, -2f), new Color(1f, 0.6f, 0.35f), 5f, 14f, deco, true);

        string[] pinos = { "tree-snow-a", "tree-snow-b", "tree-snow-c" };
        for (int i = 0; i < 26; i++)
        {
            float a = i * Mathf.PI * 2f / 26f + (float)r.NextDouble() * 0.2f;
            float rad = 20f + (float)r.NextDouble() * 8f;
            Poner(pinos[r.Next(3)], new Vector3(Mathf.Cos(a) * rad, 0f, Mathf.Sin(a) * rad), r.Next(0, 360), 1.8f + (float)r.NextDouble() * 0.8f, deco);
        }
        foreach (Vector3 p in new[] { new Vector3(-16f, 0f, 16f), new Vector3(18f, 0f, -14f), new Vector3(-19f, 0f, -12f) })
            Poner("snowman-hat", p, r.Next(0, 360), 1.6f, deco);
        foreach (Vector3 p in new[] { new Vector3(-7f, 0f, -7f), new Vector3(7f, 0f, 7f), new Vector3(10f, 0f, 1f), new Vector3(-11f, 0f, 0f) })
            Farol(p, deco, 3.5f);
        Poner("sled-long", new Vector3(5f, 0f, -7f), 30f, 1.4f, deco);
        PilaRegalos(new Vector3(6.5f, 0f, -8.5f), 3, deco, r);

        // Tren dando vueltas al árbol
        GameObject tren = Grupo("TrenCircular", deco);
        string[] vagones = { "train-locomotive", "train-tender", "train-wagon", "train-wagon-logs", "train-wagon-flat" };
        List<Transform> lv = new List<Transform>();
        foreach (string v in vagones)
        {
            GameObject holder = Grupo(v, tren.transform);
            GameObject m = Poner(v, Vector3.zero, 0f, 2f, null);
            m.transform.SetParent(holder.transform, true);
            lv.Add(holder.transform);
        }
        TrenCircular tc = tren.AddComponent<TrenCircular>();
        tc.vagones = lv.ToArray();
        tc.radio = 6f;
        tc.separacionGrados = 18f;
        tc.velocidad = 18f;

        // Juguetes y duende en escena
        for (int i = 0; i < Tipos.Length; i++)
        {
            float a = (i * 51f + 20f) * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 9f, 0f, Mathf.Sin(a) * 9f);
            Saltarin s = JugueteDecorativo(Tipos[i], p, -i * 51f - 110f, deco);
            s.velocidadForzada = 1.5f;
        }
        GameObject duende = Grupo("Personaje", deco);
        duende.transform.position = new Vector3(0f, 0f, -6f);
        duende.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject[] personajesDiorama = InstanciarTodosLosPersonajes(duende.transform, out _);

        CrearNieve(null, new Vector3(0f, 14f, 0f), new Vector3(45f, 1f, 45f), 220f);

        OrbitaCamara oc = cam.gameObject.AddComponent<OrbitaCamara>();
        oc.radio = 17f;
        oc.altura = 6.5f;
        oc.velocidad = 5f;
        oc.mirarOffset = new Vector3(0f, 2.5f, 0f);

        // ---------------- UI ----------------
        CapturarFondo("MenuPrincipal", new Vector3(13f, 6.5f, -11f), new Vector3(0f, 2.5f, 0f));

        Canvas c = CrearCanvas("UI_Menu", 10);
        Transform t = c.transform;
        MenuPrincipal mp = new GameObject("MenuPrincipal").AddComponent<MenuPrincipal>();
        mp.personajesDiorama = personajesDiorama;

        Image arriba = Imagen("DegradadoArriba", t, SpriteUI("degradado_arriba"), new Color(0f, 0f, 0f, 0.65f));
        arriba.rectTransform.anchorMin = new Vector2(0f, 1f); arriba.rectTransform.anchorMax = new Vector2(1f, 1f);
        arriba.rectTransform.pivot = new Vector2(0.5f, 1f); arriba.rectTransform.sizeDelta = new Vector2(0f, 420f);
        arriba.rectTransform.anchoredPosition = Vector2.zero;
        Image abajo = Imagen("DegradadoAbajo", t, SpriteUI("degradado_abajo"), new Color(0f, 0f, 0f, 0.85f));
        abajo.rectTransform.anchorMin = new Vector2(0f, 0f); abajo.rectTransform.anchorMax = new Vector2(1f, 0f);
        abajo.rectTransform.pivot = new Vector2(0.5f, 0f); abajo.rectTransform.sizeDelta = new Vector2(0f, 480f);
        abajo.rectTransform.anchoredPosition = Vector2.zero;

        Text tit = Texto("Titulo", t, "<color=#E8403A>JUGUETES</color> <color=#FFD24A>REBELDES</color>", 128, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1800f, 170f));
        tit.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
        tit.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        tit.rectTransform.anchoredPosition = new Vector2(0f, -130f);
        mp.titulo = tit.rectTransform;
        Texto("Lema", t, "Una Navidad fuera de control", 42, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -225f), new Vector2(1200f, 60f));

        GameObject principal = new GameObject("PanelPrincipal", typeof(RectTransform));
        Estirar(Rect(principal, t));
        mp.panelPrincipal = principal;
        Vector2 ab = new Vector2(0.5f, 0f);

        mp.textoPersonaje = Texto("TextoPersonaje", principal.transform, "Jugando como: Pino", 32, Color.white, TextAnchor.MiddleCenter,
            ab, new Vector2(0f, 345f), new Vector2(800f, 50f));
        mp.botonJugar = BotonIcono("BotonJugar", principal.transform, "JUGAR", "play", ab, new Vector2(0f, 205f), new Vector2(600f, 125f), ColorBoton, 60);
        mp.botonJugar.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        mp.textoJugar = mp.botonJugar.GetComponentInChildren<Text>();

        string[] etiquetas = { "Personaje", "Niveles", "Récords", "Opciones", "Ver intro", "Salir" };
        string[] iconos = { "persona", "bandera", "estrella", "engrane", "play", "salir" };
        Button[] fila = new Button[6];
        for (int i = 0; i < 6; i++)
        {
            fila[i] = BotonIcono("Boton" + etiquetas[i].Replace(" ", "").Replace("é", "e"), principal.transform, etiquetas[i], iconos[i], ab,
                new Vector2((i - 2.5f) * 285f, 60f), new Vector2(268f, 96f), i == 5 ? ColorBotonRojo : ColorBotonAzul, 32);
            fila[i].GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        }
        mp.botonPersonaje = fila[0];
        mp.botonNiveles = fila[1];
        mp.botonRecords = fila[2];
        mp.botonOpciones = fila[3];
        mp.botonIntro = fila[4];
        mp.botonSalir = fila[5];

        // Récords
        RectTransform cr = CrearPanelModal("PanelRecords", t, new Vector2(1250f, 660f), "Récords", new Color(1f, 0.85f, 0.3f), out GameObject records);
        mp.panelRecords = records;
        mp.textosRecords = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            Image fila2 = Imagen("Fila" + (i + 1), cr, SpriteRedondeado, new Color(1f, 1f, 1f, 0.06f), true);
            Colocar(fila2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -135f - i * 135f), new Vector2(1130f, 120f));
            mp.textosRecords[i] = Texto("Texto", fila2.transform, "", 34, Color.white, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(1080f, 110f), false);
        }
        mp.botonVolverRecords = Boton("BotonVolver", cr, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(320f, 80f), ColorBotonAzul);

        // Niveles
        RectTransform cn = CrearPanelModal("PanelNiveles", t, new Vector2(1150f, 660f), "Elige un nivel", new Color(1f, 0.85f, 0.3f), out GameObject niveles);
        mp.panelNiveles = niveles;
        string[] nombres = { "Nivel 1\n<size=30>El Taller</size>", "Nivel 2\n<size=30>La Bodega</size>", "Nivel 3\n<size=30>El Patio Nevado</size>" };
        Color[] colores = { new Color(0.75f, 0.3f, 0.2f), new Color(0.25f, 0.45f, 0.7f), new Color(0.3f, 0.6f, 0.75f) };
        mp.botonesNivel = new Button[3];
        mp.candados = new GameObject[3];
        mp.estrellasNivel = new Image[9];
        for (int i = 0; i < 3; i++)
        {
            Button b = Boton("BotonNivel" + (i + 1), cn, nombres[i], new Vector2(0.5f, 1f), new Vector2((i - 1) * 350f, -140f), new Vector2(310f, 360f), colores[i], 50);
            Text bt = b.GetComponentInChildren<Text>();
            bt.rectTransform.offsetMin = new Vector2(0f, 90f);
            mp.botonesNivel[i] = b;
            for (int e = 0; e < 3; e++)
            {
                Image st = Imagen("Estrella", b.transform, SpriteUI("estrella_vacia"), Color.white);
                Colocar(st.rectTransform, new Vector2(0.5f, 0f), new Vector2((e - 1) * 80f, 30f), new Vector2(70f, 70f));
                mp.estrellasNivel[i * 3 + e] = st;
            }
            Image cand = Imagen("Candado", b.transform, SpriteUI("candado"), Color.white);
            Colocar(cand.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(130f, 130f));
            cand.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mp.candados[i] = cand.gameObject;
        }
        mp.spriteEstrella = SpriteUI("estrella");
        mp.spriteEstrellaVacia = SpriteUI("estrella_vacia");
        mp.botonVolverNiveles = Boton("BotonVolver", cn, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 35f), new Vector2(320f, 80f), ColorBotonAzul);

        PanelOpciones po = CrearPanelOpciones(t, out Button volver);
        mp.panelOpciones = po.gameObject;
        mp.botonVolverOpciones = volver;

        Texto("Version", t, "v1.1", 24, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-25f, 20f), new Vector2(200f, 40f), false);

        CrearEventSystem();
        HornearYGuardar(escena, "MenuPrincipal", null);
    }
}
