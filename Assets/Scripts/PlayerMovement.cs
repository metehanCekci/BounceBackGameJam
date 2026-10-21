using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referanslar")]
    public PlayerInput playerInput;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 6f;
    public float maxSlopeAngle = 55f; // Bazen 45 derece sınırda kalır, biraz daha artırdık.
    public float playerHeight = 2f; 

    private Rigidbody rb;
    private RaycastHit slopeHit;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false; // Kendi yerçekimimizi yazıyoruz
    }

    void FixedUpdate()
    {
        if (playerInput == null) return;

        CheckGround();
        MovePlayer();
    }

    private bool OnSlope()
    {
        if (isGrounded)
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        // Yön vektörünü yokuşun eğimine sıfıra sıfır paralel hale getirir.
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void CheckGround()
    {
        // ÇÖZÜM: Kapsülün altını yuvarlak bir topla değil, düz bir plaka (kutu) ile tarıyoruz.
        // Bu "Sanal Düz Ayak", karakterin köşelerden kayıp düşmesini engeller.
        Vector3 boxHalfExtents = new Vector3(0.35f, 0.05f, 0.35f); // Kapsülün (0.5 çap) içine sığan düz taban
        float castDistance = (playerHeight * 0.5f) - boxHalfExtents.y + 0.15f;

        // BoxCast ile yeri tarıyoruz
        if (Physics.BoxCast(transform.position, boxHalfExtents, Vector3.down, out slopeHit, transform.rotation, castDistance))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            
            // Yokuş açısı uygunsa
            if (angle < maxSlopeAngle)
            {
                isGrounded = true;
            }
            else
            {
                // Açı çok dikse (Duvar vb.)
                isGrounded = false;
            }
        }
        else
        {
            // Altı tamamen boşsa
            isGrounded = false;
        }
    }

    private void MovePlayer()
    {
        Vector3 moveDirection = transform.forward * playerInput.MoveInput.y + transform.right * playerInput.MoveInput.x;

        // EĞER GEÇERLİ BİR ZEMİNDEYSEK (Düz veya çıkılabilir yokuş)
        if (isGrounded)
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            
            if (angle > 0)
            {
                // Yokuş Yukarı/Aşağı Hareket
                rb.linearVelocity = GetSlopeMoveDirection(moveDirection) * moveSpeed;
            }
            else
            {
                // Tam Düz Zemin Hareketi
                rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, 0f, moveDirection.z * moveSpeed);
            }

            // Hareketi kesince yokuşta/düzlükte anında durması için
            if (playerInput.MoveInput.magnitude == 0)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
        // EĞER HAVADAYSAK VEYA ÇOK DİK BİR KENARA SÜRTÜNÜYORSAK (Düşüş/Kayma)
        else
        {
            // Y eksenine (aşağı doğru) sürekli artan bir yerçekimi uyguluyoruz.
            // Bu sayede duvara doğru "W" tuşuna bassa bile sürtünemeden aşağı kayıp düşecek.
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y - (9.81f * Time.fixedDeltaTime * 2.5f), moveDirection.z * moveSpeed);
        }
    }
}
