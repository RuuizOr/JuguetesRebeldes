using UnityEngine;

// Mueve los vagones de un tren de juguete en círculo (decoración del menú).
public class TrenCircular : MonoBehaviour
{
    public Transform[] vagones;
    public float radio = 6f;
    public float velocidad = 20f;
    public float separacionGrados = 9f;

    private float angulo;

    void Update()
    {
        angulo += velocidad * Time.unscaledDeltaTime;
        for (int i = 0; i < vagones.Length; i++)
        {
            if (vagones[i] == null) continue;
            float a = (angulo - i * separacionGrados) * Mathf.Deg2Rad;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a) * radio, 0f, Mathf.Sin(a) * radio);
            Vector3 tangente = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            vagones[i].position = p;
            vagones[i].rotation = Quaternion.LookRotation(tangente) * Quaternion.Euler(0f, 90f, 0f);
        }
    }
}
