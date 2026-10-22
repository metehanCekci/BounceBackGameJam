using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    [Header("Geri Tepme Ayarları (Tabanca için)")]
    public float recoilX = -2f; // Yukarı tepme
    public float recoilY = 2f;  // Sağa/Sola rastgele tepme
    public float recoilZ = 0.5f; // Kamera yatması

    [Header("Hız Ayarları")]
    public float snapiness = 20f; // Ne kadar sert tepeceği
    public float returnSpeed = 5f; // Namlunun eski yerine ne kadar sürede ineceği

    void Update()
    {
        // Hedef rotasyonu yavaşça sıfıra çek (silahı indirme)
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        // Mevcut rotasyonu hedefe hızlıca götür (patlama anı)
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snapiness * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void FireRecoil()
    {
        targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }
}
