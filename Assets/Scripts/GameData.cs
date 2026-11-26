using UnityEngine;

// Datos globales del juego (progreso y opciones guardadas en PlayerPrefs).
public static class GameData
{
    public const int TotalNiveles = 3;
    public static int NivelActual = 1;

    public static string EscenaNivel(int n) => "Nivel" + n;

    public static int NivelDesbloqueado
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("NivelDesbloqueado", 1), 1, TotalNiveles);
        set { PlayerPrefs.SetInt("NivelDesbloqueado", Mathf.Clamp(value, 1, TotalNiveles)); PlayerPrefs.Save(); }
    }

    public static float VolMusica
    {
        get => PlayerPrefs.GetFloat("VolMusica", 0.6f);
        set { PlayerPrefs.SetFloat("VolMusica", value); PlayerPrefs.Save(); }
    }

    public static float VolSFX
    {
        get => PlayerPrefs.GetFloat("VolSFX", 0.85f);
        set { PlayerPrefs.SetFloat("VolSFX", value); PlayerPrefs.Save(); }
    }

    public static int Personaje
    {
        get => UnityEngine.Mathf.Clamp(PlayerPrefs.GetInt("Personaje", 0), 0, Personajes.Lista.Length - 1);
        set { PlayerPrefs.SetInt("Personaje", value); PlayerPrefs.Save(); }
    }

    // ---------------- Récords ----------------

    public static int RecordPuntos(int nivel) => PlayerPrefs.GetInt("RecordPuntos" + nivel, 0);

    // Tiempo usado en segundos (0 = sin récord)
    public static float RecordTiempo(int nivel) => PlayerPrefs.GetFloat("RecordTiempo" + nivel, 0f);

    public static bool GuardarPuntos(int nivel, int puntos)
    {
        if (puntos <= RecordPuntos(nivel)) return false;
        PlayerPrefs.SetInt("RecordPuntos" + nivel, puntos);
        PlayerPrefs.Save();
        return true;
    }

    public static bool GuardarTiempo(int nivel, float segundos)
    {
        float actual = RecordTiempo(nivel);
        if (actual > 0f && segundos >= actual) return false;
        PlayerPrefs.SetFloat("RecordTiempo" + nivel, segundos);
        PlayerPrefs.Save();
        return true;
    }

    public static string FormatoTiempo(float segundos)
    {
        int m = Mathf.FloorToInt(segundos / 60f);
        int s = Mathf.FloorToInt(segundos % 60f);
        return m + ":" + s.ToString("00");
    }

    public static int Estrellas(int nivel) => PlayerPrefs.GetInt("Estrellas" + nivel, 0);

    public static void GuardarEstrellas(int nivel, int estrellas)
    {
        if (estrellas > Estrellas(nivel))
        {
            PlayerPrefs.SetInt("Estrellas" + nivel, estrellas);
            PlayerPrefs.Save();
        }
    }
}
