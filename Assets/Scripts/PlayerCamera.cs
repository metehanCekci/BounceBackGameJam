using UnityEngine;

/// <summary>
/// Owns player look and persistent camera recoil.
/// Positive recoilDegrees means an upward kick. There is intentionally no
/// automatic recoil return: the player must counter-steer with the look input.
/// Attach this to a pitch pivot which is a child of playerBody.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;
    public Transform playerBody;

    [Header("Look")]
    [Min(0.001f)] public float mouseSensitivity = 0.25f;
    [Min(1f)] public float gamepadLookSpeed = 180f;
    [Range(1f, 89f)] public float maxPitch = 75f;

    private float pitch;
    private float yaw;

    private void Awake()
    {
        maxPitch = Mathf.Clamp(maxPitch, 1f, 89f);
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.localEulerAngles.x), -maxPitch, maxPitch);

        if (playerBody != null)
            yaw = Mathf.DeltaAngle(0f, playerBody.localEulerAngles.y);

        if (playerBody == transform)
            Debug.LogError("PlayerCamera: Player Body ayrı bir karakter kökü olmalı.", this);
        else if (playerBody != null && !transform.IsChildOf(playerBody))
            Debug.LogWarning("PlayerCamera: Kamera pitch pivotunu Player Body altına yerleştirmen önerilir.", this);

        if (playerInput == null)
            Debug.LogWarning("PlayerCamera: PlayerInput referansı atanmadı.", this);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (playerInput != null)
        {
            Vector2 look = playerInput.LookInput;

            if (playerInput.IsGamepad)
            {
                float dt = Time.deltaTime;
                yaw += look.x * gamepadLookSpeed * dt;
                pitch -= look.y * gamepadLookSpeed * dt;
            }
            else
            {
                // Mouse delta already represents this update's movement; do not multiply by deltaTime.
                yaw += look.x * mouseSensitivity;
                pitch -= look.y * mouseSensitivity;
            }
        }

        // One pitch value owns both mouse look and recoil. No hidden recoil offset
        // accumulates behind the clamp, and reverse mouse input works immediately.
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
        yaw = Mathf.Repeat(yaw, 360f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (playerBody != null && playerBody != transform)
            playerBody.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>Applies a persistent upward camera kick in degrees.</summary>
    public void AddRecoil(float recoilDegrees)
    {
        pitch = Mathf.Clamp(pitch - recoilDegrees, -maxPitch, maxPitch);
    }

    /// <summary>Optional persistent horizontal kick, also manually correctable by the player.</summary>
    public void AddRecoil(float verticalDegrees, float horizontalDegrees)
    {
        pitch = Mathf.Clamp(pitch - verticalDegrees, -maxPitch, maxPitch);
        yaw = Mathf.Repeat(yaw + horizontalDegrees, 360f);
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
