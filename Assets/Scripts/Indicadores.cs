using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Flechas en el borde de la pantalla que apuntan a los juguetes que van hacia la puerta (fuera de cámara).
public class Indicadores : MonoBehaviour
{
    public RectTransform area;
    public Sprite spriteFlecha;
    public float margen = 70f;

    private readonly List<Image> flechas = new List<Image>();

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || area == null) return;

        int usadas = 0;
        foreach (Juguete j in JuguetesNivel())
        {
            if (j == null || !j.Activo || j.Estado != EstadoJuguete.Escapar) continue;

            Vector3 sp = cam.WorldToScreenPoint(j.transform.position + Vector3.up);
            bool detras = sp.z < 0f;
            if (detras) sp = new Vector3(Screen.width - sp.x, Screen.height - sp.y, 0f);

            bool dentro = !detras && sp.x > 0 && sp.x < Screen.width && sp.y > 0 && sp.y < Screen.height;
            if (dentro) continue;

            Vector2 centroPantalla = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 dir = ((Vector2)sp - centroPantalla).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;

            Image f = ObtenerFlecha(usadas++);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, centroPantalla, null, out Vector2 centroLocal);
            Vector2 mitad = area.rect.size / 2f - new Vector2(margen, margen);
            float k = Mathf.Min(Mathf.Abs(mitad.x / (dir.x == 0 ? 0.0001f : dir.x)), Mathf.Abs(mitad.y / (dir.y == 0 ? 0.0001f : dir.y)));
            f.rectTransform.anchoredPosition = centroLocal + dir * k;
            f.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            float pulso = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.12f;
            f.rectTransform.localScale = Vector3.one * pulso;
        }

        for (int i = 0; i < flechas.Count; i++)
            flechas[i].gameObject.SetActive(i < usadas);
    }

    Image ObtenerFlecha(int i)
    {
        while (flechas.Count <= i)
        {
            GameObject g = new GameObject("Flecha", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(area, false);
            Image img = g.GetComponent<Image>();
            img.sprite = spriteFlecha;
            img.color = new Color(1f, 0.25f, 0.2f);
            img.raycastTarget = false;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.sizeDelta = new Vector2(70f, 70f);
            Outline o = g.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.8f);
            o.effectDistance = new Vector2(3f, -3f);
            flechas.Add(img);
        }
        flechas[i].gameObject.SetActive(true);
        return flechas[i];
    }

    static IReadOnlyList<Juguete> JuguetesNivel()
    {
        if (NivelManager.Instancia != null) return NivelManager.Instancia.Juguetes;
        return new List<Juguete>();
    }
}
