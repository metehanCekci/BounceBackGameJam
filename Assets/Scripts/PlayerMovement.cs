using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public PlayerInput playerInput;

    [Header("Movement Speeds")]
    public float moveSpeed = 5.5f;
    public float sprintSpeed = 7.5f;
    [Range(0.1f, 1f)] public float aimingMoveMultiplier = 0.62f;
    [Range(0.1f, 1f)] public float firingMoveMultiplier = 0.72f;
    [Range(0f, 1f)] public float airControlMultiplier = 0.3f;

    [Header("Weight / Acceleration")]
    public float groundAcceleration = 18f;
    public float groundDeceleration = 24f;
    public float airAcceleration = 5f;
    public float gravity = 24f;

    [Header("Ground / Slope")]
    [Range(1f, 89f)] public float maxSlopeAngle = 55f;
    [Min(0.5f)] public float playerHeight = 2f;
    public Vector3 groundBoxHalfExtents = new Vector3(0.35f, 0.05f, 0.35f);
    public float groundCheckExtraDistance = 0.15f;
    public LayerMask groundMask = ~0;

    private Rigidbody rb;
    private RaycastHit groundHit;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false; // Custom gravity is applied in FixedUpdate.
    }

    private void FixedUpdate()
    {
        if (playerInput == null)
            return;

        CheckGround();
        MovePlayer();
    }

    private void CheckGround()
    {
        float castDistance = Mathf.Max(0.05f, playerHeight * 0.5f - groundBoxHalfExtents.y + groundCheckExtraDistance);
        isGrounded = Physics.BoxCast(
            transform.position,
            groundBoxHalfExtents,
            Vector3.down,
            out groundHit,
            transform.rotation,
            castDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, groundHit.normal);
            isGrounded = slopeAngle <= maxSlopeAngle;
        }
    }

    private void MovePlayer()
    {
        Vector2 input = Vector2.ClampMagnitude(playerInput.MoveInput, 1f);
        Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        bool wantsSprint = playerInput.IsSprinting && !playerInput.IsAiming && !playerInput.IsShooting;
        float targetSpeed = wantsSprint ? sprintSpeed : moveSpeed;

        // Handling a weapon or aiming reduces acceleration and top speed.
        if (playerInput.IsAiming)
            targetSpeed *= aimingMoveMultiplier;
        if (playerInput.IsShooting)
            targetSpeed *= firingMoveMultiplier;

        Vector3 velocity = GetVelocity();
        Vector3 currentHorizontal = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 desiredDirection = moveDirection;
        float acceleration = input.sqrMagnitude > 0.001f ? groundAcceleration : groundDeceleration;

        if (isGrounded)
        {
            bool onSlope = Vector3.Angle(Vector3.up, groundHit.normal) > 0.5f;
            Vector3 desiredVelocity;

            if (onSlope && moveDirection.sqrMagnitude > 0.001f)
                desiredVelocity = Vector3.ProjectOnPlane(moveDirection, groundHit.normal).normalized * targetSpeed;
            else
                desiredVelocity = desiredDirection * targetSpeed;

            Vector3 desiredHorizontal = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            currentHorizontal = Vector3.MoveTowards(currentHorizontal, desiredHorizontal, acceleration * Time.fixedDeltaTime);
            velocity.x = currentHorizontal.x;
            velocity.z = currentHorizontal.z;

            if (onSlope && moveDirection.sqrMagnitude > 0.001f)
                velocity.y = Mathf.MoveTowards(velocity.y, desiredVelocity.y, acceleration * Time.fixedDeltaTime);
            else
                velocity.y = 0f;
        }
        else
        {
            Vector3 desiredAirVelocity = moveDirection * targetSpeed * airControlMultiplier;
            currentHorizontal = Vector3.MoveTowards(currentHorizontal, desiredAirVelocity, airAcceleration * Time.fixedDeltaTime);
            velocity.x = currentHorizontal.x;
            velocity.z = currentHorizontal.z;
            velocity.y -= gravity * Time.fixedDeltaTime;
        }

        SetVelocity(velocity);
    }

    private Vector3 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void SetVelocity(Vector3 value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = value;
#else
        rb.velocity = value;
#endif
    }
}
