using UnityEngine;
using UnityEngine.AI;

// Hace que un modelo sin animaciones "camine" dando saltitos y balanceándose.
public class Saltarin : MonoBehaviour
{
    public NavMeshAgent agente;
    public Transform modelo;
    public float altura = 0.25f;
    public float frecuencia = 10f;
    public float inclinacion = 10f;
    public float velocidadReferencia = 2f;
    [Tooltip("Si es >= 0 ignora al agente y usa esta velocidad (para cinemáticas)")]
    public float velocidadForzada = -1f;
    public bool sonidoPisada;

    private Vector3 posicionBase;
    private Quaternion rotacionBase;
    private float fase;
    private float ultimoPaso;

    void Start()
    {
        if (agente == null)
            agente = GetComponent<NavMeshAgent>();
        if (modelo != null)
        {
            posicionBase = modelo.localPosition;
            rotacionBase = modelo.localRotation;
        }
    }

    void Update()
    {
        if (modelo == null)
            return;

        float v = velocidadForzada >= 0f ? velocidadForzada : (agente != null && agente.enabled ? agente.velocity.magnitude : 0f);
        float k = Mathf.Clamp01(v / velocidadReferencia);

        fase += Time.deltaTime * frecuencia * (0.5f + 0.5f * k);
        float s = Mathf.Abs(Mathf.Sin(fase));

        modelo.localPosition = posicionBase + Vector3.up * s * altura * k;
        modelo.localRotation = rotacionBase * Quaternion.Euler(0f, 0f, Mathf.Sin(fase) * inclinacion * k);

        if (sonidoPisada && k > 0.3f && s < 0.08f && Time.time - ultimoPaso > 0.25f)
        {
            ultimoPaso = Time.time;
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            float d = cam != null ? Vector3.Distance(cam.position, transform.position) : 10f;
            float vol = Mathf.Clamp01(1f - (d - 10f) / 18f);
            if (vol > 0.05f)
            {
                AudioManager.SFX("pisada", vol * 0.8f, 0.1f);
                CamaraSeguimiento.Sacudir(0.06f * vol);
            }
        }
    }
}
