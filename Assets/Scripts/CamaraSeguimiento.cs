using UnityEngine;

// Cámara en tercera persona que sigue al duende desde arriba, con sacudida al recibir daño.
public class CamaraSeguimiento : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 distancia = new Vector3(0f, 12.5f, -8.5f);
    public Vector3 mirarOffset = new Vector3(0f, 0f, 1.8f);
    public float suavidad = 6f;

    private static CamaraSeguimiento instancia;
    private float sacudida;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        if (objetivo != null)
        {
            transform.position = objetivo.position + distancia;
            transform.LookAt(objetivo.position + mirarOffset);
        }
    }

    public static void Sacudir(float intensidad)
    {
        if (instancia != null)
            instancia.sacudida = Mathf.Max(instancia.sacudida, intensidad);
    }

    void LateUpdate()
    {
        if (objetivo == null)
            return;

        Vector3 deseada = objetivo.position + distancia;
        transform.position = Vector3.Lerp(transform.position, deseada, 1f - Mathf.Exp(-suavidad * Time.deltaTime));
        transform.LookAt(objetivo.position + mirarOffset);

        if (sacudida > 0f)
        {
            transform.position += Random.insideUnitSphere * sacudida * 0.4f;
            sacudida = Mathf.MoveTowards(sacudida, 0f, Time.deltaTime * 1.5f);
        }
    }
}
