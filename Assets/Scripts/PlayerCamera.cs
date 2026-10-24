using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public Transform playerBody;

    [Header("Mouse Look")]
    [Min(0.001f)]
    public float mouseSensitivity = 0.25f;

    [Range(1f, 89f)]
    public float maxPitch = 75f;

    [Header("Recoil")]
    [Min(0f)]
    public float recoilRecoverySpeed = 12f;

    [Min(0f)]
    public float maxRecoilPitch = 20f;

    // Fareyle kontrol edilen temel dikey açı.
    private float lookPitch;

    // Karakterin yatay dönüş açısı.
    private float yaw;

    // Silahın geçici dikey geri tepmesi.
    private float recoilPitch;

    private void Awake()
    {
        // Kameranın dikey sınırını 90 derecenin altında tut.
        maxPitch = Mathf.Clamp(maxPitch, 1f, 89f);
        maxRecoilPitch = Mathf.Max(0f, maxRecoilPitch);

        // Euler açılarını -180 / +180 aralığında güvenle oku.
        lookPitch = Mathf.Clamp(
            Mathf.DeltaAngle(
                0f,
                transform.localEulerAngles.x
            ),
            -maxPitch,
            maxPitch
        );

        if (playerBody != null)
        {
            yaw = Mathf.DeltaAngle(
                0f,
                playerBody.localEulerAngles.y
            );
        }

        // Kamera ve karakter gövdesi aynı Transform olamaz.
        if (playerBody == transform)
        {
            Debug.LogError(
                "PlayerCamera: playerBody için ayrı bir " +
                "karakter kök Transform'u atamalısın.",
                this
            );
        }
        else if (
            playerBody != null &&
            !transform.IsChildOf(playerBody)
        )
        {
            Debug.LogWarning(
                "Kamera pivotu normalde playerBody nesnesinin " +
                "çocuğu olmalıdır.",
                this
            );
        }

        if (playerInput == null)
        {
            Debug.LogWarning(
                "PlayerCamera: PlayerInput referansını Inspector'dan ata.",
                this
            );
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1. Recoil'i zamandan bağımsız, kontrollü hızla söndür.
        recoilPitch = Mathf.MoveTowards(
            recoilPitch,
            0f,
            recoilRecoverySpeed * dt
        );

        // 2. Fare girdisini işle.
        // LookInput ham Mouse Delta değerleri içeriyorsa
        // burada Time.deltaTime ile çarpmıyoruz.
        if (playerInput != null)
        {
            Vector2 look = playerInput.LookInput;

            yaw += look.x * mouseSensitivity;
            lookPitch -= look.y * mouseSensitivity;

            // Yatay açının gereksiz yere büyümesini engelle.
            yaw = Mathf.Repeat(yaw, 360f);
        }

        // 3. Temel açıyı recoil'e göre sınırlandır.
        // Böylece toplam açı limitte kalırken fare kontrolü
        // ters yönde hemen cevap vermeye devam eder.
        lookPitch = Mathf.Clamp(
            lookPitch,
            -maxPitch - recoilPitch,
             maxPitch - recoilPitch
        );

        // 4. Son güvenlik katmanı.
        float finalPitch = Mathf.Clamp(
            lookPitch + recoilPitch,
            -maxPitch,
             maxPitch
        );

        // 5. Dikey dönüşü kamera pivotuna uygula.
        transform.localRotation = Quaternion.Euler(
            finalPitch,
            0f,
            0f
        );

        // 6. Yatay dönüşü karakter gövdesine uygula.
        if (playerBody != null && playerBody != transform)
        {
            playerBody.localRotation = Quaternion.Euler(
                0f,
                yaw,
                0f
            );
        }
    }

    /// <summary>
    /// Silah ateş ettiğinde çağır.
    /// Pozitif değer kamerayı yukarı doğru tepme yönünde hareket ettirir.
    /// Örnek: AddRecoil(2f) = 2 derecelik yukarı tepme.
    /// </summary>
    public void AddRecoil(float amount)
    {
        // Biriken recoil'e üst sınır uygula.
        float requestedRecoil = Mathf.Clamp(
            recoilPitch - amount,
            -maxRecoilPitch,
             maxRecoilPitch
        );

        // Mevcut bakış açısını sınırların dışına itecek
        // recoil bileşenini kabul etme.
        recoilPitch = Mathf.Clamp(
            requestedRecoil,
            -maxPitch - lookPitch,
             maxPitch - lookPitch
        );
    }

    // Menü açıldığında fareyi serbest bırakmak için kullanılabilir.
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }
}
