using UnityEngine;

public class BrassShell : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.AddTorque(Random.insideUnitSphere * 25f, ForceMode.Impulse);
        Invoke("SleepPhysics", 2f);
        Destroy(gameObject, 30f);
    }

    void FixedUpdate()
    {
        if (rb != null && !rb.isKinematic)
        {
            // Kovanlara özel 5 kat yerçekimi uyguluyoruz (Tarkov tarzı tok düşüş)
            rb.AddForce(Physics.gravity * 5f, ForceMode.Acceleration);
        }
    }

    void SleepPhysics()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
    }
}
