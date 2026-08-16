using UnityEngine;
using UnityEngine.InputSystem;

public enum GunslingerItem
{
    Flamethrower,
    Knives,
    Catapult
}

public sealed class Gunslinger : MonoBehaviour
{
    [Header("Aim")]
    [SerializeField] private WorldAimMarker aimMarker;
    [SerializeField] private Transform projectileOrigin;

    [Header("Equipped item")]
    [SerializeField] private GunslingerItem equippedItem = GunslingerItem.Knives;

    [Header("Projectile prefabs")]
    [SerializeField] private CombatProjectile flamethrowerProjectile;
    [SerializeField] private CombatProjectile knifeProjectile;
    [SerializeField] private CombatProjectile catapultProjectile;

    [Header("Flamethrower")]
    [SerializeField] private int flameProjectileCount = 7;
    [SerializeField] private float flameArcAngle = 45f;
    [SerializeField] private float flameSpeed = 15f;
    [SerializeField] private float flameCooldown = .18f;
    [SerializeField] private float flameProjectileLifetime = 1.2f;

    [Header("Knives")]
    [SerializeField] private float knifeSpeed = 28f;
    [SerializeField] private float knifeCooldown = .32f;
    [SerializeField] private float knifeProjectileLifetime = 3f;

    [Header("Catapult")]
    [SerializeField] private float catapultHorizontalSpeed = 10f;
    [SerializeField] private float catapultUpwardSpeed = 13f;
    [SerializeField] private float catapultCooldown = .9f;
    [SerializeField] private float catapultExplosionRadius = 3.5f;
    [SerializeField] private float catapultProjectileLifetime = 5f;

    [Header("Shared")]
    [SerializeField] private int projectileDamage = 1;

    private float nextFireTime;

    private void Awake()
    {
        if (aimMarker == null) aimMarker = GetComponent<WorldAimMarker>();
        if (projectileOrigin == null) projectileOrigin = transform;
    }

    private void Update()
    {
        SelectItem();
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryFire();
    }

    private void SelectItem()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) equippedItem = GunslingerItem.Flamethrower;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) equippedItem = GunslingerItem.Knives;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) equippedItem = GunslingerItem.Catapult;
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime || aimMarker == null || !aimMarker.TryGetAimPoint(out Vector3 aimPoint)) return;

        Vector3 origin = projectileOrigin.position + Vector3.up * 1.1f;
        Vector3 flatDirection = aimPoint - origin;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < .01f) return;
        flatDirection.Normalize();

        switch (equippedItem)
        {
            case GunslingerItem.Flamethrower:
                FireFlameArc(origin, flatDirection);
                nextFireTime = Time.time + flameCooldown;
                break;
            case GunslingerItem.Knives:
                SpawnProjectile(knifeProjectile, origin, flatDirection * knifeSpeed, knifeProjectileLifetime);
                nextFireTime = Time.time + knifeCooldown;
                break;
            case GunslingerItem.Catapult:
                Vector3 velocity = flatDirection * catapultHorizontalSpeed + Vector3.up * catapultUpwardSpeed;
                SpawnProjectile(catapultProjectile, origin, velocity, catapultProjectileLifetime, catapultExplosionRadius);
                nextFireTime = Time.time + catapultCooldown;
                break;
        }
    }

    private void FireFlameArc(Vector3 origin, Vector3 direction)
    {
        for (int i = 0; i < flameProjectileCount; i++)
        {
            float t = flameProjectileCount == 1 ? .5f : i / (float)(flameProjectileCount - 1);
            float angle = Mathf.Lerp(-flameArcAngle * .5f, flameArcAngle * .5f, t);
            Vector3 arcDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
            arcDirection = (arcDirection + Vector3.up * Random.Range(.02f, .16f)).normalized;
            SpawnProjectile(flamethrowerProjectile, origin, arcDirection * flameSpeed, flameProjectileLifetime);
        }
    }

    private void SpawnProjectile(
        CombatProjectile prefab,
        Vector3 origin,
        Vector3 velocity,
        float lifetime,
        float explosionRadius = 0f)
    {
        if (prefab == null) return;
        CombatProjectile projectile = Instantiate(prefab, origin + velocity.normalized * .8f, Quaternion.LookRotation(velocity));
        IgnorePlayerProjectileCollisions(projectile);
        projectile.Launch(velocity, projectileDamage, lifetime, explosionRadius);
    }

    private void IgnorePlayerProjectileCollisions(CombatProjectile projectile)
    {
        foreach (Collider playerCollider in GetComponents<Collider>())
            projectile.IgnoreCollisionWith(playerCollider);

        foreach (CombatProjectile otherProjectile in FindObjectsByType<CombatProjectile>())
        {
            if (otherProjectile != projectile)
                projectile.IgnoreCollisionWith(otherProjectile.GetComponent<Collider>());
        }
    }
}
