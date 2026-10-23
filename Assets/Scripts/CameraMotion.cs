using UnityEngine;

/// <summary>
/// Optional secondary camera motion. Attach to a child pivot under the pitch pivot,
/// NOT the same Transform which has PlayerCamera. Put the actual camera below it.
/// </summary>
public class CameraMotion : MonoBehaviour
{
    public PlayerInput playerInput;
    public Rigidbody playerRb;

    [Header("Walk Bob")]
    public float bobSpeed = 11f;
    public float bobAmount = 0.035f;
    public float bobReturnSpeed = 8f;

    [Header("Strafe Tilt")]
    public float tiltAngle = 2f;
    public float tiltSmooth = 7f;

    private Vector3 defaultLocalPosition;
    private Vector3 currentBobOffset;
    private float timer;
    private float currentTilt;

    private void Awake()
    {
        defaultLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (playerInput == null)
            return;

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

        Vector3 bobTarget = Vector3.zero;
        if (planarSpeed > 0.15f)
        {
            timer += Time.deltaTime * bobSpeed * Mathf.Clamp(planarSpeed / 5.5f, 0.5f, 1.5f);
            bobTarget.y = Mathf.Sin(timer) * bobAmount;
            bobTarget.x = Mathf.Cos(timer * 0.5f) * bobAmount * 0.45f;
        }

        float bobBlend = 1f - Mathf.Exp(-bobReturnSpeed * Time.deltaTime);
        currentBobOffset = Vector3.Lerp(currentBobOffset, bobTarget, bobBlend);
        transform.localPosition = defaultLocalPosition + currentBobOffset;

        float targetTilt = -playerInput.MoveInput.x * tiltAngle;
        float tiltBlend = 1f - Mathf.Exp(-tiltSmooth * Time.deltaTime);
        currentTilt = Mathf.LerpAngle(currentTilt, targetTilt, tiltBlend);

        // This component owns only the child pivot's roll; PlayerCamera owns pitch/yaw.
        transform.localRotation = Quaternion.Euler(0f, 0f, currentTilt);
    }
}
