using UnityEngine;
public class DecalLife : MonoBehaviour {
    void Start() { Destroy(gameObject, 15f); } // 15 saniye sonra silinir
}
