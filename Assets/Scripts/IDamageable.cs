using UnityEngine;

/// <summary>
/// Implement this interface on an enemy/target health component to receive
/// damage from WeaponController's hitscan. The visual tracer does not deal damage.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}
