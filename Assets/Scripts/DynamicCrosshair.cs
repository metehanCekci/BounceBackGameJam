using UnityEngine;

/// <summary>
/// Projects the actual muzzle-forward ray onto the HUD, so the crosshair follows
/// the direction used by WeaponController rather than an ideal camera-center ray.
/// </summary>
public class DynamicCrosshair : MonoBehaviour
{
    [Header("References")]
    public Transform muzzlePoint;
    public Camera mainCamera;
    public RectTransform crosshairUI;
    public PlayerInput playerInput;
    public WeaponController weaponController;

    [Header("Fallback Ray Settings")]
    public float maxDistance = 100f;
    public LayerMask hitLayer = ~0;

    [Header("HUD")]
    public CanvasGroup canvasGroup;
    public float crosshairMoveSpeed = 45f;

    private RectTransform parentRect;
    private Canvas parentCanvas;

    private void Awake()
    {
        if (crosshairUI != null)
        {
            parentRect = crosshairUI.parent as RectTransform;
            parentCanvas = crosshairUI.GetComponentInParent<Canvas>();
        }

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (muzzlePoint == null || mainCamera == null || crosshairUI == null || parentRect == null)
            return;

        bool aiming = playerInput != null && playerInput.IsAiming;
        float alpha = aiming ? 0f : 1f;

        float rayDistance = weaponController != null ? weaponController.range : maxDistance;
        LayerMask rayMask = weaponController != null ? weaponController.shootLayer : hitLayer;

        Vector3 targetPoint;
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit,
                rayDistance, rayMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = muzzlePoint.position + muzzlePoint.forward * rayDistance;
        }

        Vector3 screenPoint = mainCamera.WorldToScreenPoint(targetPoint);
        bool inFront = screenPoint.z > 0f;
        bool onScreen = inFront && screenPoint.x >= 0f && screenPoint.x <= Screen.width &&
                        screenPoint.y >= 0f && screenPoint.y <= Screen.height;

        if (canvasGroup != null)
            canvasGroup.alpha = alpha * (onScreen ? 1f : 0f);

        if (!onScreen)
            return;

        Camera canvasCamera = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = parentCanvas.worldCamera != null ? parentCanvas.worldCamera : mainCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPoint, canvasCamera, out Vector2 localPoint))
        {
            float blend = 1f - Mathf.Exp(-crosshairMoveSpeed * Time.deltaTime);
            crosshairUI.anchoredPosition = Vector2.Lerp(crosshairUI.anchoredPosition, localPoint, blend);
        }
    }
}
