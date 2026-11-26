using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sonido y pequeña animación al pasar el mouse o hacer clic en un botón.
[RequireComponent(typeof(Button))]
public class BotonSonido : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 escalaBase = Vector3.one;
    private bool encima;

    void Awake()
    {
        escalaBase = transform.localScale;
        GetComponent<Button>().onClick.AddListener(() => AudioManager.SFX("click"));
    }

    void OnDisable()
    {
        encima = false;
        transform.localScale = escalaBase;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!GetComponent<Button>().interactable) return;
        encima = true;
        AudioManager.SFX("hover", 0.6f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        encima = false;
    }

    void Update()
    {
        Vector3 objetivo = escalaBase * (encima ? 1.07f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, objetivo, Time.unscaledDeltaTime * 14f);
    }
}
