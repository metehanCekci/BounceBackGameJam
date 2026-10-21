using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    // Diğer scriptlerin okuyabileceği public değişkenler
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool IsGamepad { get; private set; }

    private InputAction moveAction;
    private InputAction lookAction;

    void Awake()
    {
        // Hareket (Klavye ve Joystick Sol Analog)
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Gamepad>/leftStick/up")
            .With("Down", "<Gamepad>/leftStick/down")
            .With("Left", "<Gamepad>/leftStick/left")
            .With("Right", "<Gamepad>/leftStick/right");

        // Bakış (Fare ve Joystick Sağ Analog)
        lookAction = new InputAction("Look");
        lookAction.AddBinding("<Pointer>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
    }

    void Update()
    {
        MoveInput = moveAction.ReadValue<Vector2>();
        LookInput = lookAction.ReadValue<Vector2>();

        // Güncel kontrol cihazının Gamepad olup olmadığını tespit et
        if (lookAction.activeControl != null)
        {
            IsGamepad = lookAction.activeControl.device is Gamepad;
        }
    }
}
