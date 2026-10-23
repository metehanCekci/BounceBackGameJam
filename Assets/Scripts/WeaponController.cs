using UnityEngine;
using System.Collections;
using TMPro;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public Rigidbody playerRb;
    public PlayerCamera cameraController;
    public Transform muzzlePoint;
    public Transform playerCamera; // Main Camera transform used for wall checks.

    [Header("Weapon")]
    [Min(0.01f)] public float fireRate = 0.15f;
    [Min(1)] public int maxAmmo = 12;
    [Min(0f)] public float reloadDuration = 1.5f;
    [Header("Reload Pose (weapon stays visible)")]
    [Min(0f)] public float reloadDropAmount = 0.12f;
    [Range(0f, 45f)] public float reloadTiltAngle = 16f;
    [Min(0.1f)] public float reloadPoseSpeed = 7f;
    private float reloadPoseWeight;
    private int currentAmmo;
    private float nextTimeToFire;
    private bool isReloading;
    private Coroutine muzzleLightCoroutine;

    [Header("Hitscan + Visual Tracer")]
    public float damage = 25f;
    public float range = 100f;
    public LayerMask shootLayer = ~0;
    public GameObject bulletPrefab;
    public float bulletSpeed = 250f;
    public float tracerSpawnOffset = 0.05f;
    public ParticleSystem hitEffectPrefab;
    public GameObject bulletHoleDecal;

    [Header("Audio")]
    public AudioSource weaponAudioSource;
    public AudioClip shotClip;
    public AudioClip reloadClip;
    [Range(0f, 1f)] public float shotVolume = 1f;
    [Range(0f, 1f)] public float reloadVolume = 0.8f;

    [Header("Hipfire / ADS Pose")]
    public Vector3 hipPosition;
    public Vector3 hipRotationOffset;
    public Vector3 adsPosition;
    public Vector3 adsRotation;
    public float aimSpeed = 15f;

    [Header("Wall Blocking")]
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.6f;
    public float wallCheckRadius = 0.15f;
    public Vector3 wallBlockRotation = new Vector3(-65f, -20f, 0f);

    [Header("Weapon Sway / Weight")]
    [Tooltip("Mouse delta -> weapon angle. Lower values make the weapon less twitchy.")]
    public float mouseSwayDegreesPerPixel = 0.08f;
    public float maxLookSwayAngle = 5f;
    public float gamepadSwayAngle = 3f;
    public float swayFollowSpeed = 16f;
    [Range(0f, 1f)] public float adsSwayMultiplier = 0.25f;
    public float movementPitchSway = 2f;
    public float movementYawSway = 2.5f;
    public float movementRollSway = 4f;
    public Vector2 movementPositionSway = new Vector2(0.025f, 0.02f);
    public float walkBobAmount = 0.015f;
    public float breathingSpeed = 2f;
    public float hipBreathingAmount = 0.005f;
    public float adsBreathingAmount = 0.001f;

    [Header("Weapon Recoil (visual only; camera recoil is separate)")]
    public float kickBackZ = -0.1f;
    public float kickUpY = 0.05f;
    public float kickRotateX = -5f;
    public float kickRotateY = 3f;
    public float returnSpeed = 10f;
    public float snapiness = 25f;
    public float climbPerShot = 2.5f;
    private float currentRecoilClimb;
    private Vector3 recoilPositionOffset;
    private Vector3 recoilRotationOffset;
    private Vector3 defaultRotation;
    private Vector3 currentLookSway;
    private Vector3 currentMovementSway;
    private float bobTimer;
    private float breathingTimer;

    [Header("Camera Recoil (persistent; player counters it manually)")]
    [Min(0f)] public float cameraRecoilAmount = 1.5f;
    [Min(0f)] public float cameraHorizontalRecoil = 0.2f;

    [Header("Muzzle FX / Shells")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem barrelSmoke;
    public Light muzzleLight;
    public float flashLightIntensity = 80000f;
    public float muzzleLightDuration = 0.05f;
    public GameObject shellPrefab;
    public Transform ejectionPort;
    public float shellEjectionForce = 3f;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    private void Start()
    {
        maxAmmo = Mathf.Max(1, maxAmmo);
        fireRate = Mathf.Max(0.01f, fireRate);
        range = Mathf.Max(0.1f, range);
        currentAmmo = maxAmmo;
        defaultRotation = transform.localEulerAngles;
        UpdateAmmoUI();

        if (muzzleLight != null)
            muzzleLight.intensity = 0f;

        if (playerInput == null)
            Debug.LogError("WeaponController: PlayerInput referansı atanmamış.", this);
        if (playerCamera == null)
            Debug.LogWarning("WeaponController: Wall check için Player Camera referansı atanmamış.", this);
        if (muzzlePoint == null)
            Debug.LogError("WeaponController: Muzzle Point atanmamış; atış yapılamaz.", this);
        if (cameraController == null)
            Debug.LogWarning("WeaponController: Camera Controller atanmamış; kamera recoil çalışmayacak.", this);
    }

    private void Update()
    {
        if (playerInput == null || playerCamera == null)
            return;

        bool isNearWall = Physics.SphereCast(
            playerCamera.position,
            wallCheckRadius,
            playerCamera.forward,
            out _,
            wallCheckDistance,
            wallLayer,
            QueryTriggerInteraction.Ignore
        );

        float dt = Time.deltaTime;
        float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, swayFollowSpeed) * dt);
        float adsMultiplier = playerInput.IsAiming ? adsSwayMultiplier : 1f;

        Vector3 targetPos = (playerInput.IsAiming && !isNearWall) ? adsPosition : hipPosition;
        Vector3 baseRotation = (playerInput.IsAiming && !isNearWall)
            ? adsRotation
            : defaultRotation + hipRotationOffset;
        Quaternion targetRot = Quaternion.Euler(baseRotation);

        if (isNearWall)
        {
            targetPos = hipPosition + new Vector3(0f, -0.1f, -0.2f);
            targetRot = Quaternion.Euler(defaultRotation + wallBlockRotation);
        }

        // Look sway is bounded and smoothed. Mouse delta and gamepad stick are handled differently.
        Vector2 look = playerInput.LookInput;
        Vector3 targetLookSway;
        if (playerInput.IsGamepad)
        {
            targetLookSway = new Vector3(-look.y, look.x, -look.x * 0.75f) * gamepadSwayAngle;
        }
        else
        {
            targetLookSway = new Vector3(-look.y, look.x, -look.x * 0.75f) * mouseSwayDegreesPerPixel;
        }

        targetLookSway.x = Mathf.Clamp(targetLookSway.x, -maxLookSwayAngle, maxLookSwayAngle);
        targetLookSway.y = Mathf.Clamp(targetLookSway.y, -maxLookSwayAngle, maxLookSwayAngle);
        targetLookSway.z = Mathf.Clamp(targetLookSway.z, -maxLookSwayAngle, maxLookSwayAngle);
        targetLookSway *= adsMultiplier;
        currentLookSway = Vector3.Lerp(currentLookSway, targetLookSway, blend);

        // Moving and strafing physically tilt the gun, so muzzle-forward and the crosshair agree.
        Vector2 move = playerInput.MoveInput;
        Vector3 targetMovementSway = new Vector3(
            -move.y * movementPitchSway,
            move.x * movementYawSway,
            -move.x * movementRollSway
        ) * adsMultiplier;
        currentMovementSway = Vector3.Lerp(currentMovementSway, targetMovementSway, blend);
        targetRot *= Quaternion.Euler(currentLookSway + currentMovementSway);

        targetPos += new Vector3(
            -move.x * movementPositionSway.x,
            -move.y * movementPositionSway.y,
            0f
        ) * adsMultiplier;

        breathingTimer += dt * breathingSpeed;
        float breathAmount = playerInput.IsAiming ? adsBreathingAmount : hipBreathingAmount;
        targetPos += new Vector3(
            Mathf.Cos(breathingTimer) * breathAmount,
            Mathf.Sin(breathingTimer * 2f) * breathAmount,
            0f
        );

        float planarSpeed = 0f;
        if (playerRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 velocity = playerRb.linearVelocity;
#else
            Vector3 velocity = playerRb.velocity;
#endif
            planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        }

        if (planarSpeed > 0.1f && !isNearWall && !playerInput.IsAiming)
        {
            bobTimer += dt * 12f;
            targetPos += new Vector3(
                Mathf.Cos(bobTimer) * walkBobAmount,
                Mathf.Sin(bobTimer * 2f) * walkBobAmount,
                0f
            );
        }

        // A small, smoothed reload pose. The previous fixed -0.4 m drop could
        // move a low-held weapon entirely below the camera view.
        float reloadTargetWeight = isReloading ? 1f : 0f;
        reloadPoseWeight = Mathf.MoveTowards(
            reloadPoseWeight,
            reloadTargetWeight,
            Mathf.Max(0.1f, reloadPoseSpeed) * dt
        );
        targetPos += Vector3.down * (reloadDropAmount * reloadPoseWeight);
        targetRot *= Quaternion.Euler(reloadTiltAngle * reloadPoseWeight, 0f, 0f);

        float recoilBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, returnSpeed) * dt);
        recoilPositionOffset = Vector3.Lerp(recoilPositionOffset, Vector3.zero, recoilBlend);
        recoilRotationOffset = Vector3.Lerp(recoilRotationOffset, Vector3.zero, recoilBlend);
        currentRecoilClimb = Mathf.MoveTowards(currentRecoilClimb, 0f, returnSpeed * dt);

        float poseSpeed = playerInput.IsAiming ? aimSpeed : snapiness;
        float poseBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, poseSpeed) * dt);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos + recoilPositionOffset, poseBlend);

        Quaternion visualKick = Quaternion.Euler(
            recoilRotationOffset + new Vector3(-currentRecoilClimb, 0f, 0f)
        );
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot * visualKick,
            poseBlend
        );

        if (isReloading)
            return;

        if (playerInput.IsShooting && Time.time >= nextTimeToFire && currentAmmo > 0 && !isNearWall)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }

        // PlayerInput.IsReloading is a one-frame WasPressedThisFrame pulse.
        if (playerInput.IsReloading && currentAmmo < maxAmmo && !isReloading)
            StartCoroutine(ReloadCoroutine());
    }

    private void Shoot()
    {
        // Validate before spending ammo or triggering feedback.
        if (muzzlePoint == null)
        {
            Debug.LogError("WeaponController: Muzzle Point boş; atış iptal edildi.", this);
            return;
        }

        currentAmmo--;
        UpdateAmmoUI();

        if (cameraController != null)
        {
            float horizontalKick = Random.Range(-cameraHorizontalRecoil, cameraHorizontalRecoil);
            cameraController.AddRecoil(cameraRecoilAmount, horizontalKick);
        }

        // Visual weapon kick recovers; camera recoil does NOT auto-recover.
        currentRecoilClimb += climbPerShot;
        recoilPositionOffset += new Vector3(0f, kickUpY, kickBackZ);
        recoilRotationOffset += new Vector3(
            kickRotateX,
            Random.Range(-kickRotateY, kickRotateY),
            Random.Range(-1f, 1f)
        );

        if (weaponAudioSource != null && shotClip != null)
            weaponAudioSource.PlayOneShot(shotClip, shotVolume);

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }
        if (barrelSmoke != null)
            barrelSmoke.Play();
        if (muzzleLight != null)
        {
            if (muzzleLightCoroutine != null)
                StopCoroutine(muzzleLightCoroutine);
            muzzleLightCoroutine = StartCoroutine(FlashLight());
        }

        EjectShell();

        // Intentional design: the actual shot uses the CURRENT muzzle direction.
        // Keep this same muzzle + layer mask assigned in DynamicCrosshair.
        Vector3 shootDirection = muzzlePoint.forward;
        if (Physics.Raycast(
                muzzlePoint.position,
                shootDirection,
                out RaycastHit hit,
                range,
                shootLayer,
                QueryTriggerInteraction.Ignore))
        {
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            if (bulletHoleDecal != null)
            {
                Quaternion decalRotation = Quaternion.LookRotation(-hit.normal) *
                                           Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                GameObject hole = Instantiate(
                    bulletHoleDecal,
                    hit.point + hit.normal * 0.001f,
                    decalRotation
                );
                hole.transform.SetParent(hit.transform, true);
            }

            // Only the hitscan applies damage; the moving tracer is visual-only.
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, hit.point, hit.normal);
        }

        // This projectile is a visual tracer. The hitscan above determines instant impact.
        if (bulletPrefab != null)
        {
            Vector3 spawnPos = muzzlePoint.position + shootDirection * Mathf.Max(0f, tracerSpawnOffset);
            GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDirection));
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

            if (bulletRb != null)
            {
#if UNITY_6000_0_OR_NEWER
                bulletRb.linearVelocity = shootDirection * bulletSpeed;
#else
                bulletRb.velocity = shootDirection * bulletSpeed;
#endif
            }
        }
    }

    private void EjectShell()
    {
        if (shellPrefab == null || ejectionPort == null)
            return;

        GameObject shell = Instantiate(shellPrefab, ejectionPort.position, ejectionPort.rotation);
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();
        if (shellRb != null)
        {
            Vector3 direction = ejectionPort.right + Vector3.up * 0.2f - ejectionPort.forward * 0.1f;
            shellRb.AddForce(direction.normalized * shellEjectionForce, ForceMode.Impulse);
        }
    }

    private IEnumerator FlashLight()
    {
        muzzleLight.intensity = flashLightIntensity;
        yield return new WaitForSeconds(muzzleLightDuration);
        if (muzzleLight != null)
            muzzleLight.intensity = 0f;
        muzzleLightCoroutine = null;
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        if (weaponAudioSource != null && reloadClip != null)
            weaponAudioSource.PlayOneShot(reloadClip, reloadVolume);

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = maxAmmo;
        isReloading = false;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = currentAmmo + " / " + maxAmmo;
    }
}
