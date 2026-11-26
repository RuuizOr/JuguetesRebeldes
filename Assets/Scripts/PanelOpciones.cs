using UnityEngine;
using UnityEngine.UI;

// Panel de opciones: volumen de música, efectos y pantalla completa.
public class PanelOpciones : MonoBehaviour
{
    public Slider sliderMusica;
    public Slider sliderEfectos;
    public Toggle togglePantallaCompleta;
    public Text textoMusica;
    public Text textoEfectos;

    void OnEnable()
    {
        if (sliderMusica != null) sliderMusica.SetValueWithoutNotify(GameData.VolMusica);
        if (sliderEfectos != null) sliderEfectos.SetValueWithoutNotify(GameData.VolSFX);
        if (togglePantallaCompleta != null) togglePantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
        ActualizarTextos();
    }

    void Awake()
    {
        if (sliderMusica != null) sliderMusica.onValueChanged.AddListener(v =>
        {
            GameData.VolMusica = v;
            AudioManager.Instancia.AplicarVolumen();
            ActualizarTextos();
        });

        if (sliderEfectos != null) sliderEfectos.onValueChanged.AddListener(v =>
        {
            GameData.VolSFX = v;
            ActualizarTextos();
        });

        if (sliderEfectos != null)
        {
            // Sonido de prueba al soltar
            var trigger = sliderEfectos.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entrada = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
            entrada.callback.AddListener(_ => AudioManager.SFX("atrapar"));
            trigger.triggers.Add(entrada);
        }

        if (togglePantallaCompleta != null)
            togglePantallaCompleta.onValueChanged.AddListener(v => Screen.fullScreen = v);
    }

    void ActualizarTextos()
    {
        if (textoMusica != null) textoMusica.text = "Música: " + Mathf.RoundToInt(GameData.VolMusica * 100f) + "%";
        if (textoEfectos != null) textoEfectos.text = "Efectos: " + Mathf.RoundToInt(GameData.VolSFX * 100f) + "%";
    }
}
