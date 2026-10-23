using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central input reader. Other gameplay components read these values instead
/// of creating their own competing InputActions.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsGamepad { get; private set; }
    public bool IsAiming { get; private set; }
    public bool IsShooting { get; private set; }
    public bool IsReloading { get; private set; }
    public bool IsSprinting { get; private set; }

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction shootAction;
    private InputAction reloadAction;
    private InputAction aimAction;
    private InputAction sprintAction;

    private void Awake()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");

        shootAction = new InputAction("Shoot", InputActionType.Button, "<Mouse>/leftButton");
        shootAction.AddBinding("<Gamepad>/rightTrigger");

        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        reloadAction.AddBinding("<Gamepad>/buttonWest");

        aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        aimAction.AddBinding("<Gamepad>/buttonEast");

        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftStickPress");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        shootAction?.Enable();
        reloadAction?.Enable();
        aimAction?.Enable();
        sprintAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        shootAction?.Disable();
        reloadAction?.Disable();
        aimAction?.Disable();
        sprintAction?.Disable();
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null)
            return;

        MoveInput = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        LookInput = lookAction.ReadValue<Vector2>();

        if (lookAction.activeControl != null)
            IsGamepad = lookAction.activeControl.device is Gamepad;

        IsShooting = shootAction != null && shootAction.IsPressed();
        IsAiming = aimAction != null && aimAction.IsPressed();
        IsReloading = reloadAction != null && reloadAction.WasPressedThisFrame();
        IsSprinting = sprintAction != null && sprintAction.IsPressed();
    }
}
