using UnityEngine;

// Proyectil de bola de nieve: congela juguetes y al Cascanueces.
[RequireComponent(typeof(Rigidbody))]
public class BolaNieve : MonoBehaviour
{
    public float congelarJuguete = 3f;
    public float congelarCascanueces = 2.5f;
    public float vida = 3f;
    public GameObject efectoImpacto;

    private bool usada;

    void Start()
    {
        Destroy(gameObject, vida);
    }

    void OnTriggerEnter(Collider other)
    {
        if (usada || other.CompareTag("Player") || other.GetComponentInParent<PowerUp>() != null || other.GetComponent<ZonaHielo>() != null)
            return;

        Juguete j = other.GetComponentInParent<Juguete>();
        if (j != null)
        {
            if (!j.Congelar(congelarJuguete)) return;
            if (NivelManager.Instancia != null)
                NivelManager.Instancia.MostrarAviso("¡" + Capitalizar(j.nombreJuguete) + " congelado!", new Color(0.6f, 0.9f, 1f));
            Impacto();
            return;
        }

        Cascanueces c = other.GetComponentInParent<Cascanueces>();
        if (c != null)
        {
            c.Congelar(congelarCascanueces);
            Impacto();
            return;
        }

        if (!other.isTrigger)
            Impacto();
    }

    static string Capitalizar(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

    void Impacto()
    {
        usada = true;
        AudioManager.SFX("pop", 0.8f, 0.2f);
        if (efectoImpacto != null)
            Instantiate(efectoImpacto, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
