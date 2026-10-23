# Hardcore FPS — İlk Mekanik Paketi

Bu paket senin ZIP'inde bulunan sistemlere göre hazırlanmış bir **ilk entegrasyon geçişidir**. Unity Editor içinde çalıştırılmış bir proje derlemesi değildir; prefab/sahne referansları elle doğrulanmalıdır.

## Paketteki dosyalar

- `PlayerInput.cs`: Klavye/fare ve gamepad girdileri; Left Shift / gamepad stick press ile sprint.
- `PlayerCamera.cs`: Kamera bakışı ve **otomatik geri toplamayan** recoil. Ateş kamerayı yukarı iter; oyuncu fareyle aşağı düzeltene kadar açı kalır.
- `PlayerMovement.cs`: Ağırlıklı hızlanma/yavaşlama, sprint, ADS ve ateş sırasında hız kısıtlaması.
- `WeaponController.cs`: Tek kamera recoil kaynağı, silahın görsel kick/sway'i, atış sesi, namlu efektleri, kovan ve muzzle-forward hitscan.
- `DynamicCrosshair.cs`: Crosshair aynı muzzle transformunu, menzili ve `WeaponController.shootLayer` maskesini kullanır.
- `CameraMotion.cs`: Yürüyüş bob'u ve strafe roll'u; ayrı bir child pivot üzerinde çalışmalıdır.
- `IDamageable.cs`: Hedef sağlık scriptlerinin hitscan hasarı alması için arayüz.

## Önerilen Transform hiyerarşisi

```text
PlayerRoot (Rigidbody, PlayerInput, PlayerMovement)
└── CameraPitchPivot (PlayerCamera)
    └── CameraEffectsPivot (CameraMotion)
        ├── Main Camera (Camera)
        └── WeaponHolder
            └── Weapon (WeaponController)
                └── ...
                    └── MuzzlePoint
```

- `PlayerCamera.playerBody` → `PlayerRoot`
- `CameraMotion` → `CameraEffectsPivot` üzerinde; `PlayerCamera` ile aynı Transform'a takma.
- `WeaponController.cameraController` → `CameraPitchPivot` üzerindeki `PlayerCamera` bileşeni.
- `WeaponController.playerCamera` → `Main Camera` Transform'u (duvar kontrolü için).
- `WeaponController.muzzlePoint` ve `DynamicCrosshair.muzzlePoint` → aynı `MuzzlePoint` Transform'u.
- `DynamicCrosshair.weaponController` → ilgili `WeaponController`. Böylece crosshair `range` ve `shootLayer` değerlerini doğrudan silah scriptinden alır.
- `DynamicCrosshair.mainCamera` → `Main Camera`; `crosshairUI` → crosshair `RectTransform`; `canvasGroup` → crosshair'i gizleyen CanvasGroup.
- Crosshair UI öğesinin anchor/pivot ayarlarını Canvas içinde merkeze göre düzenle. Oyun ekranının ortasında değilse `crosshairUI` bir merkez tabanlı RectTransform altında olmalı.
- `PlayerMovement.playerInput` ve `WeaponController.playerInput` aynı `PlayerInput` bileşenini kullanmalı.
- `PlayerMovement.playerRb` / `WeaponController.playerRb` alanları için `PlayerRoot` Rigidbody'sini kullan.

## Önemli geçiş adımları

1. Projeyi veya en azından ilgili `Assets` klasörünü yedekle.
2. Bu paketteki `.cs` dosyalarını mevcut aynı adlı dosyalarla değiştir. `IDamageable.cs` yeni dosyadır.
3. Unity'de **eski `CameraRecoil` bileşenini sahnedeki kamera/pivot nesnelerinden kaldır**. Kod dosyası projede kalabilir ama canlı kamera Transform'unu kontrol etmemeli.
4. `CameraMotion` bileşenini `PlayerCamera` ile aynı GameObject'ten alıp `CameraEffectsPivot` child'ına taşı.
5. `WeaponController` Inspector'ında alanları yeniden kontrol et. Alan adları değiştiğinden bazı eski serialized değerler yeni alanlara otomatik taşınmayabilir.
6. `shootLayer` içinden oyuncu ve silahın kendi collider katmanlarını çıkar; düşmanları ve vurulabilir çevreyi dahil et. `DynamicCrosshair.weaponController` atanırsa crosshair aynı LayerMask'i kullanır.
7. `weaponAudioSource`, `shotClip` ve `reloadClip` alanlarını atarsan atış/yükleme sesi oynatılır.
8. Hedeflerin `Health` scripti `IDamageable` arayüzünü uygulamalı. Gerekli imza: `TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)`. Arşivde düşman Health scripti olmadığı için mevcut sağlık sistemine otomatik entegrasyon burada varsayılmadı.

## Bu paketin tasarım kararları

- Atış yönü hâlâ `muzzlePoint.forward`.
- Crosshair aynı namlu yönündeki ray'i projekte eder.
- Hareket sway'i silah Transform'unu değiştirir; crosshair namlunun gerçek yönünü takip ettiği için bunu görsel olarak gösterir.
- Kamera recoil'i **kalıcıdır**. Silah modelinin kendi görsel kick'i toparlanır; kamera eski açısına otomatik dönmez.
- Sprint yalnızca nişan almıyorken ve ateş girdisi basılı değilken kullanılabilir. ADS/ateş hâlinde hareket hızı azaltılır.
- Hitscan hasarı tek gerçek hasar kaynağıdır. `bulletPrefab` yalnızca hareketli görsel izdir; bu sayede çift hasar üretilmez.

## Başlangıç ayarları

- `PlayerCamera.mouseSensitivity`: mevcut hissine göre `0.15–0.25`
- `PlayerCamera.maxPitch`: `75`
- `WeaponController.cameraRecoilAmount`: `1.2–1.8` derece/atış
- `WeaponController.cameraHorizontalRecoil`: `0.1–0.3` derece/atış
- `PlayerMovement.aimingMoveMultiplier`: `0.55–0.7`
- `PlayerMovement.firingMoveMultiplier`: `0.65–0.8`
- `WeaponController.maxLookSwayAngle`: `4–6`
- `WeaponController.movementRollSway`: `3–5`

Bunlar başlangıç değerleri; oyunun silahına göre playtest ile ayarlanmalı.
