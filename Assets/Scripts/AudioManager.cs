using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Música y efectos de sonido. Persiste entre escenas (se carga desde Resources/AudioManager).
public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sonido
    {
        public string nombre;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen = 1f;
    }

    public AudioClip musicaMenu;
    public AudioClip musicaJuego;
    public Sonido[] sonidos;

    private static AudioManager instancia;
    private AudioSource fuenteMusica;
    private AudioSource fuenteSFX;
    private Dictionary<string, Sonido> mapa;
    private float factorFade = 1f;
    private Coroutine rutinaFade;

    public static AudioManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
                if (prefab != null)
                    Instantiate(prefab).name = "AudioManager";
                else
                    new GameObject("AudioManager").AddComponent<AudioManager>();
            }
            return instancia;
        }
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.ignoreListenerPause = true;

        fuenteSFX = gameObject.AddComponent<AudioSource>();
        fuenteSFX.playOnAwake = false;
        fuenteSFX.ignoreListenerPause = true;

        mapa = new Dictionary<string, Sonido>();
        if (sonidos != null)
            foreach (Sonido s in sonidos)
                if (s != null && s.clip != null && !string.IsNullOrEmpty(s.nombre))
                    mapa[s.nombre] = s;

        AplicarVolumen();
    }

    public void AplicarVolumen()
    {
        if (fuenteMusica != null)
            fuenteMusica.volume = GameData.VolMusica * factorFade;
    }

    public static void MusicaMenu() => Instancia.CambiarMusica(Instancia.musicaMenu);
    public static void MusicaJuego() => Instancia.CambiarMusica(Instancia.musicaJuego);

    public static void SFX(string nombre, float volumen = 1f, float variacionTono = 0.05f)
    {
        AudioManager am = Instancia;
        if (am == null || am.mapa == null || !am.mapa.TryGetValue(nombre, out Sonido s))
            return;

        am.fuenteSFX.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        am.fuenteSFX.PlayOneShot(s.clip, s.volumen * volumen * GameData.VolSFX);
    }

    public void CambiarMusica(AudioClip clip)
    {
        if (clip == null || (fuenteMusica.clip == clip && fuenteMusica.isPlaying))
            return;

        if (rutinaFade != null)
            StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(Fundido(clip));
    }

    IEnumerator Fundido(AudioClip nuevo)
    {
        if (fuenteMusica.isPlaying)
        {
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.6f)
            {
                factorFade = t;
                AplicarVolumen();
                yield return null;
            }
        }

        fuenteMusica.clip = nuevo;
        fuenteMusica.Play();

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
        {
            factorFade = t;
            AplicarVolumen();
            yield return null;
        }

        factorFade = 1f;
        AplicarVolumen();
    }
}
