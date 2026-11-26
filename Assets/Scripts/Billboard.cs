using UnityEngine;

// Mantiene un texto/objeto mirando a la cámara.
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation;
    }
}
