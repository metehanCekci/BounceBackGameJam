using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 2f); // 2 saniye sonra yok et
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
