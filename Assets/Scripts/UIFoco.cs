using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Selecciona el primer botón de un panel para poder navegar con el control.
public static class UIFoco
{
    public static void Seleccionar(GameObject panel)
    {
        if (EventSystem.current == null || panel == null) return;
        foreach (Selectable s in panel.GetComponentsInChildren<Selectable>())
        {
            if (s.interactable && s.gameObject.activeInHierarchy && s.navigation.mode != Navigation.Mode.None)
            {
                EventSystem.current.SetSelectedGameObject(s.gameObject);
                return;
            }
        }
    }

    public static void Seleccionar(Selectable s)
    {
        if (EventSystem.current != null && s != null)
            EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    public static void Limpiar()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
