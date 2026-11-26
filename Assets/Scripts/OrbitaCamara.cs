using UnityEngine;

// Cámara que gira lentamente alrededor de un punto (fondo del menú).
public class OrbitaCamara : MonoBehaviour
{
    public Vector3 centro = Vector3.zero;
    public float radio = 18f;
    public float altura = 7f;
    public float velocidad = 6f;
    public Vector3 mirarOffset = new Vector3(0f, 2f, 0f);

    private float angulo = 200f;

    void LateUpdate()
    {
        angulo += velocidad * Time.unscaledDeltaTime;
        float r = angulo * Mathf.Deg2Rad;
        transform.position = centro + new Vector3(Mathf.Cos(r) * radio, altura, Mathf.Sin(r) * radio);
        transform.LookAt(centro + mirarOffset);
    }
}
