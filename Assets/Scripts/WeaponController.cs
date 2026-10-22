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
    public GameObject bulletPrefab; 
    public float bulletSpeed = 250f; 
    public float adsTracerOffset = 0.5f; // YENİ: ADS'deyken mermiyi kameranın dışına itme mesafesi
    public ParticleSystem hitEffectPrefab; 

    [Header("Duruş & Bağımsız Eğim (Tilt)")]
    public Vector3 hipPosition; 
    public Vector3 hipRotationOffset = new Vector3(5f, -2f, 0f); 
    public Vector3 adsPosition; 
    public Vector3 adsRotation; 
    public float aimSpeed = 15f;
    
    [Header("Duvar Çarpışması")]
    public LayerMask wallLayer; 
    public float wallCheckDistance = 0.6f;
    public float wallCheckRadius = 0.15f; 
    public Vector3 wallBlockRotation = new Vector3(-65f, -20f, 0f); 

    [Header("Nefes & Yalpalama (Sway & Bob)")]
    public float swayMultiplier = 1.5f; 
    public float walkBobAmount = 0.015f; 
    public float breathingSpeed = 2f; 
    public float hipBreathingAmount = 0.005f; 
    public float adsBreathingAmount = 0.001f; 

    [Header("Gerçekçi Geri Tepme (Wobble)")]
    public float kickBackZ = -0.1f;
    public float kickUpY = 0.05f;
    public float kickRotateX = -5f; 
    public float kickRotateY = 3f;  
    public float returnSpeed = 10f;
    public float snapiness = 25f;

    [Header("Efektler & Işık (HDRP)")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem barrelSmoke;
    public Light muzzleLight; 
    public float flashLightIntensity = 80000f; // YENİ: HDRP için devasa ışık patlaması gücü (Lümen)
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

    void Start()
    {
        currentAmmo = maxAmmo;
        defaultRotation = transform.localEulerAngles;
        UpdateAmmoUI();
        
        if(muzzleLight != null) muzzleLight.intensity = 0f;
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

        if (!isNearWall)
        {
            float swayMult = playerInput.IsAiming ? swayMultiplier * 0.15f : swayMultiplier; 
            float mouseX = playerInput.LookInput.x * swayMult;
            float mouseY = playerInput.LookInput.y * swayMult;
            Quaternion swayRot = Quaternion.Euler(mouseY, -mouseX, 0); 
            targetRot *= swayRot;
        }

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
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot * Quaternion.Euler(recoilRotationOffset), currentSpeed * Time.deltaTime);

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

        Vector3 targetPoint;
        if (Physics.Raycast(playerCamera.position, playerCamera.forward, out RaycastHit hit, range))
        {
            targetPoint = hit.point; 
            
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            targetPoint = playerCamera.position + playerCamera.forward * range;
        }

        // ÇÖZÜM 2: Merminin Doğuş Noktası (ADS Koruması)
        if (bulletPrefab != null && muzzlePoint != null)
        {
            Vector3 shootDirection = (targetPoint - muzzlePoint.position).normalized;
            Vector3 spawnPos = muzzlePoint.position;
            
            // Eğer nişan alıyorsak, mermi izi kamerayı delmesin diye onu atış yönünde yarım metre ileri itiyoruz
            if (playerInput.IsAiming)
            {
                spawnPos += shootDirection * adsTracerOffset;
            }

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
        // ÇÖZÜM 1: HDRP için devasa ışık patlaması
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
