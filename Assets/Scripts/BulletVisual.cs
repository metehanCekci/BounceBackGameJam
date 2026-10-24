using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Merminin bir önceki kareden şu anki kareye kadar kat ettiği mesafeyi hesapla
        Vector3 direction = transform.position - lastPosition;
        float distance = direction.magnitude;

        // Bu aralığı ışınla (Raycast) tara. İncecik bir duvar olsa bile KESİN yakalar.
        if (Physics.Raycast(lastPosition, direction, out RaycastHit hit, distance))
        {
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Player") && 
                hit.collider.gameObject.layer != LayerMask.NameToLayer("Debris"))
            {
                // Duvarı yakaladığı an mermiyi yok et
                Destroy(gameObject);
            }
        }
        
        lastPosition = transform.position;
    }
}
