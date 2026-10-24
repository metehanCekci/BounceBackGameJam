using UnityEngine;
using System.Collections;
using TMPro;

public class WeaponController : MonoBehaviour
{
    [Header("Referanslar")]
    public PlayerInput playerInput;
    public Rigidbody playerRb;
    public CameraRecoil cameraRecoil;
    public Transform muzzlePoint;
    public Transform playerCamera;

    [Header("Silah Özellikleri")]
    public float fireRate = 0.15f;
    public int maxAmmo = 12;
    private int currentAmmo;
    private float nextTimeToFire = 0f;
    private bool isReloading = false;

    [Header("Vuruş (Hitscan) & Görsel Mermi")]
    public float damage = 25f;
    public float range = 100f;
    public LayerMask shootLayer;
    public GameObject bulletPrefab;
    public float bulletSpeed = 250f;
    public float adsTracerOffset = 0.5f;
    public ParticleSystem hitEffectPrefab;
    public GameObject bulletHoleDecal;

    [Header("Duruş & Bağımsız Eğim (Tilt)")]
    public Vector3 hipPosition;
    public Vector3 hipRotationOffset = new Vector3(0f, 0f, 0f);
    public Vector3 adsPosition;
    public Vector3 adsRotation;
    public float aimSpeed = 15f;

    [Header("Duvar Çarpışması")]
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.6f;
    public float wallCheckRadius = 0.15f;
    public Vector3 wallBlockRotation = new Vector3(-65f, -20f, 0f);

    [Header("Hardcore Yalpalama (Sway & Lag Ayarları)")]
    public float swayAmount = 15f;       // 1. SWAY AYARI: Burayı artırarak yalpalama şiddetini büyütebilirsin!
    public float maxSwayAngle = 45f;     // 2. SWAY AYARI: Maksimum esneme açısı (İstediğin gibi 30 veya 45 yapabilirsin)
    public float walkBobAmount = 0.015f;
    public float breathingSpeed = 2f;
    public float hipBreathingAmount = 0.005f;
    public float adsBreathingAmount = 0.001f;

    [Header("Gerçekçi Geri Tepme ve Yukarı Tırmanma (Recoil Climb)")]
    public float kickBackZ = -0.1f;
    public float kickUpY = 0.05f;
    public float kickRotateX = -5f;
    public float kickRotateY = 3f;
    public float returnSpeed = 10f;
    public float snapiness = 25f;

    // YENİ: Seri atışta merminin yukarı doğru tırmanma birikimi
    private float currentRecoilClimb = 0f;

    [Header("Efektler & Işık (HDRP)")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem barrelSmoke;
    public Light muzzleLight;
    public float flashLightIntensity = 80000f;
    public GameObject shellPrefab;
    public Transform ejectionPort;
    public float shellEjectionForce = 3f;

    [Header("Arayüz")]
    public TextMeshProUGUI ammoText;

    private Vector3 defaultRotation;
    private Vector3 recoilPositionOffset;
    private Vector3 recoilRotationOffset;
    private float bobTimer = 0f;
    private float breathingTimer = 0f;

    [Header("Kamera Tepmesi")]
    public Transform playerCameraTransform; // Main Camera objesini Inspector'dan buraya sürükleyeceksin
    public float cameraRecoilAmount = 1.5f; // Kameranın yukarı kalkış şiddeti

    void Start()
    {
        currentAmmo = maxAmmo;
        defaultRotation = transform.localEulerAngles;
        UpdateAmmoUI();

        if (muzzleLight != null) muzzleLight.intensity = 0f;
    }

    void Update()
    {
        if (playerInput == null || playerCamera == null) return;

        bool isNearWall = Physics.SphereCast(playerCamera.position, wallCheckRadius, playerCamera.forward, out RaycastHit wallHit, wallCheckDistance, wallLayer);

        Vector3 targetPos = (playerInput.IsAiming && !isNearWall) ? adsPosition : hipPosition;
        Vector3 currentRotEuler = (playerInput.IsAiming && !isNearWall) ? adsRotation : (defaultRotation + hipRotationOffset);
        Quaternion targetRot = Quaternion.Euler(currentRotEuler);

        if (isNearWall)
        {
            targetPos = hipPosition + new Vector3(0, -0.1f, -0.2f);
            targetRot = Quaternion.Euler(defaultRotation + wallBlockRotation);
        }

        // --- SİLAHIN MERKEZ PİVOTTAN ESNEYEREK GERİDE KALMASI (Y EKSENİ YALPALAMA) ---
        if (!isNearWall)
        {
            float swayMult = playerInput.IsAiming ? 0.15f : 1f;

            float rawX = playerInput.LookInput.x * swayAmount * swayMult;
            float rawY = playerInput.LookInput.y * swayAmount * swayMult;

            float swayPitch = Mathf.Clamp(rawY, -maxSwayAngle, maxSwayAngle);
            float swayYaw = Mathf.Clamp(-rawX, -maxSwayAngle, maxSwayAngle);  // Sağa dönünce namlu SOLA esner
            float swayRoll = Mathf.Clamp(rawX * 0.8f, -maxSwayAngle, maxSwayAngle);

            Quaternion swayRot = Quaternion.Euler(swayPitch, swayYaw, swayRoll);
            Vector3 swayPos = new Vector3(-rawX * 0.002f, -rawY * 0.002f, 0);

            targetRot *= swayRot;
            targetPos += swayPos;
        }

        // Ateş etmediğimiz sürece yukarı tırmanma (recoil climb) yavaşça sıfırlanır
        currentRecoilClimb = Mathf.Lerp(currentRecoilClimb, 0f, returnSpeed * Time.deltaTime);

        breathingTimer += Time.deltaTime * breathingSpeed;
        float currentBreath = playerInput.IsAiming ? adsBreathingAmount : hipBreathingAmount;
        targetPos += new Vector3(Mathf.Cos(breathingTimer) * currentBreath, Mathf.Sin(breathingTimer * 2f) * currentBreath, 0);

        float speed = (playerRb != null) ? new Vector3(playerRb.linearVelocity.x, 0, playerRb.linearVelocity.z).magnitude : 0;
        if (speed > 0.1f && !isNearWall && !playerInput.IsAiming)
        {
            bobTimer += Time.deltaTime * 12f;
            targetPos += new Vector3(Mathf.Cos(bobTimer) * walkBobAmount, Mathf.Sin(bobTimer * 2f) * walkBobAmount, 0);
        }

        if (isReloading)
        {
            targetPos += new Vector3(0, -0.4f, 0);
            targetRot *= Quaternion.Euler(30f, 0, 0);
        }

        recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, returnSpeed * Time.deltaTime);
        recoilRotationOffset = Vector3.Lerp(recoilRotationOffset, Vector3.zero, returnSpeed * Time.deltaTime);

        float currentSpeed = playerInput.IsAiming ? aimSpeed : snapiness;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos + recoilPositionOffset, currentSpeed * Time.deltaTime);

        // KRİTİK: Recoil tırmanmasını (yukarı kaymayı) silahın anlık rotasyonuna doğrudan ekliyoruz!
        Quaternion finalRecoilRot = Quaternion.Euler(recoilRotationOffset + new Vector3(-currentRecoilClimb, 0, 0));
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot * finalRecoilRot, currentSpeed * Time.deltaTime);

        if (isReloading) return;

        if (playerInput.IsShooting && Time.time >= nextTimeToFire && currentAmmo > 0 && !isNearWall)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }

        if (playerInput.IsReloading && currentAmmo < maxAmmo)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    void Shoot()
    {
        currentAmmo--;
        UpdateAmmoUI();

        if (cameraRecoil != null) cameraRecoil.FireRecoil();

        // Ateş anında kamera scriptine tepme komutu gönderiyoruz
        // Ateş anında kameranın baktığı açıyı doğrudan yukarı itip orada sabitliyoruz
        if (playerCameraTransform != null)
        {
            // Kameranın yerel (local) rotasyonunu alıyoruz
            Vector3 camEuler = playerCameraTransform.localEulerAngles;

            // 0-360 açı çevirimi (-180 ile 180 arası çalışması için)
            float currentCamX = camEuler.x;
            if (currentCamX > 180f) currentCamX -= 360f;

            // Kamerayı yukarı itiyoruz ve tavan sınırını (-75 derece) asla aşmamasını sağlıyoruz
            float newCamX = Mathf.Clamp(currentCamX - (cameraRecoilAmount * 3f), -75f, 75f);

            camEuler.x = newCamX;
            playerCameraTransform.localEulerAngles = camEuler;
        }
        // 1. Seri atışta merminin yukarı doğru tırmanma birikimi
        currentRecoilClimb += 2.5f;

        // 2. Silahın kasasına binen fiziksel darbe ve rotasyon sarsıntısı
        recoilPositionOffset += new Vector3(0, kickUpY, kickBackZ);
        recoilRotationOffset += new Vector3(kickRotateX, Random.Range(-kickRotateY, kickRotateY), Random.Range(-1f, 1f));

        if (muzzleFlash != null) muzzleFlash.Play();
        if (barrelSmoke != null) barrelSmoke.Play();
        if (muzzleLight != null) StartCoroutine(FlashLight());

        if (shellPrefab != null && ejectionPort != null)
        {
            GameObject shell = Instantiate(shellPrefab, ejectionPort.position, ejectionPort.rotation);
            Rigidbody shellRb = shell.GetComponent<Rigidbody>();
            if (shellRb != null)
            {
                Vector3 ejectionDirection = ejectionPort.right + (Vector3.up * 0.2f) - (ejectionPort.forward * 0.1f);
                shellRb.AddForce(ejectionDirection * shellEjectionForce, ForceMode.Impulse);
            }
        }

        Vector3 shootDirection = muzzlePoint.forward;

        if (Physics.Raycast(muzzlePoint.position, shootDirection, out RaycastHit hit, range, shootLayer))
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }

            if (bulletHoleDecal != null)
            {
                Quaternion decalRotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                GameObject hole = Instantiate(bulletHoleDecal, hit.point + hit.normal * 0.001f, decalRotation);
                hole.transform.SetParent(hit.transform);
            }
        }

        if (bulletPrefab != null && muzzlePoint != null)
        {
            Vector3 spawnPos = muzzlePoint.position + (shootDirection * 0.15f);
            if (playerInput.IsAiming) spawnPos += shootDirection * adsTracerOffset;

            GameObject bullet = Instantiate(bulletPrefab, spawnPos, muzzlePoint.rotation);
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

            if (bulletRb != null)
            {
                bulletRb.linearVelocity = shootDirection * bulletSpeed;
            }
        }
    }

    IEnumerator FlashLight()
    {
        muzzleLight.intensity = flashLightIntensity;
        yield return new WaitForSeconds(0.05f);
        muzzleLight.intensity = 0f;
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(1.5f);
        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null) ammoText.text = currentAmmo + " / " + maxAmmo;
    }
}
