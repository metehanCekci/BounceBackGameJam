using UnityEngine;

public class CameraMotion : MonoBehaviour
{
    public PlayerInput playerInput;
    public Rigidbody playerRb;

    [Header("Yürüme Sarsıntısı (Headbob)")]
    public float bobSpeed = 12f; // Adım atma hızı
    public float bobAmount = 0.06f; // Sarsıntı şiddeti
    private float defaultYPos = 0;
    private float timer = 0;

    [Header("Yana Yatma (Strafe Tilt)")]
    public float tiltAngle = 2.5f; // A ve D tuşlarına basınca kameranın yatma açısı
    public float tiltSmooth = 6f;
    private float currentTilt;

    void Start()
    {
        defaultYPos = transform.localPosition.y;
    }

    void Update()
    {
        if (playerInput == null || playerRb == null) return;

        // 1. Kafa Sarsıntısı Matematiği (Artık fiziki hıza değil, tuşlara basmaya duyarlı)
        float inputMagnitude = playerInput.MoveInput.magnitude;
        Vector3 newPos = transform.localPosition;

        // Eğer oyuncu yürüme tuşlarına/joystick'e basıyorsa sarsıntıyı başlat
        if (inputMagnitude > 0.1f)
        {
            timer += Time.deltaTime * bobSpeed;
            newPos.y = defaultYPos + Mathf.Sin(timer) * bobAmount;
            newPos.x = Mathf.Cos(timer / 2) * (bobAmount * 0.5f);
        }
        else
        {
            // Olduğu yerde duruyor veya sadece etrafa bakıyorsa (Fare/Sağ Analog) merkeze dön
            timer = 0;
            newPos.y = Mathf.Lerp(newPos.y, defaultYPos, Time.deltaTime * 5f);
            newPos.x = Mathf.Lerp(newPos.x, 0f, Time.deltaTime * 5f);
        }
        transform.localPosition = newPos;

        // 2. Yana Yatma Matematiği (Strafe Tilt)
        float targetTilt = -playerInput.MoveInput.x * tiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmooth);

        transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, currentTilt);
    }
}
