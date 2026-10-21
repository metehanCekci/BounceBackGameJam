using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public PlayerInput playerInput;
    public Transform playerBody;

    public float mouseSensitivity = 15f;
    public float gamepadAimSensitivity = 100f;
    
    [Header("Ağırlık Hissiyatı (Smoothing)")]
    public float lookSmoothTime = 0.05f; // Değer arttıkça kafa daha ağır döner

    private float xRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 mouseDeltaVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (playerInput == null || playerBody == null) return;

        float currentSens = playerInput.IsGamepad ? gamepadAimSensitivity : mouseSensitivity;
        
        // Ham girdiyi yumuşat (Kafanın ağır dönmesini sağlayan kısım)
        Vector2 targetMouseDelta = playerInput.LookInput * currentSens * Time.deltaTime;
        currentMouseDelta = Vector2.SmoothDamp(currentMouseDelta, targetMouseDelta, ref mouseDeltaVelocity, lookSmoothTime);

        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);
        
        // Yukarı/Aşağı bakış
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Gövdeyi sağa/sola çevir
        playerBody.Rotate(Vector3.up * currentMouseDelta.x);
    }
}
