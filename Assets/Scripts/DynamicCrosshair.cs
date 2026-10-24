using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform muzzlePoint; 
    public Camera mainCamera; 
    public RectTransform crosshairUI; 
    public PlayerInput playerInput; 
    
    [Header("Ayarlar")]
    public float maxDistance = 100f;
    public LayerMask hitLayer; 
    public CanvasGroup canvasGroup; 

    void Update()
    {
        if (muzzlePoint == null || mainCamera == null || crosshairUI == null) return;

        if (playerInput != null && playerInput.IsAiming)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return; 
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        Vector3 targetPoint;
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit, maxDistance, hitLayer))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = muzzlePoint.position + muzzlePoint.forward * maxDistance;
        }

        // ÇÖZÜM: Ekran pikseli yerine Viewport oranını kullanıyoruz.
        Vector2 viewportPos = mainCamera.WorldToViewportPoint(targetPoint);
        
        // Eğer hedef arkamızda kalmıyorsa (Z > 0)
        if (viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1)
        {
            // Ekranın tam ortasını (0,0) baz alan AnchoredPosition hesabı
            Vector2 screenCenterBasedPos = new Vector2(
                (viewportPos.x - 0.5f) * Screen.width,
                (viewportPos.y - 0.5f) * Screen.height
            );

            // Noktayı uçmadan direkt hedefe kaydır
            crosshairUI.anchoredPosition = Vector3.Lerp(crosshairUI.anchoredPosition, screenCenterBasedPos, Time.deltaTime * 35f);
        }
    }
}
