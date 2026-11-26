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
