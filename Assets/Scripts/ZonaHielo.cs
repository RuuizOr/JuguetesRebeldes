using UnityEngine;

// Zona de hielo: el duende resbala (le cuesta frenar y cambiar de dirección).
public class ZonaHielo : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        DuendeController d = other.GetComponent<DuendeController>();
        if (d != null) d.EntrarHielo();
    }

    void OnTriggerExit(Collider other)
    {
        DuendeController d = other.GetComponent<DuendeController>();
        if (d != null) d.SalirHielo();
    }
}
