using UnityEngine;

// Destruye el objeto después de unos segundos (efectos de partículas).
public class AutoDestruir : MonoBehaviour
{
    public float segundos = 2f;

    void Start()
    {
        Destroy(gameObject, segundos);
    }
}
