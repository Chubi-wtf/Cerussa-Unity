using UnityEngine;

public enum ProjectileType
{
    Flame,
    Knife,
    Catapult
}

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class CombatProjectile : MonoBehaviour
{
    [SerializeField] private ProjectileType projectileType;

    private Rigidbody projectileBody;
    private Collider projectileCollider;
    private int damage;
    private float explosionRadius;
    private bool hasImpacted;

    private void Awake()
    {
        projectileBody = GetComponent<Rigidbody>();
        projectileBody.useGravity = projectileType == ProjectileType.Catapult;
        projectileBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = projectileType == ProjectileType.Catapult ? .42f : .18f;
        projectileCollider = sphereCollider;
        CreateVisual();
    }

    public void IgnoreCollisionWith(Collider otherCollider)
    {
        if (projectileCollider != null && otherCollider != null)
            Physics.IgnoreCollision(projectileCollider, otherCollider);
    }

    public void Launch(Vector3 velocity, int projectileDamage, float lifetime, float radius = 0f)
    {
        damage = projectileDamage;
        explosionRadius = radius;
        projectileBody.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted) return;
        hasImpacted = true;

        if (projectileType == ProjectileType.Catapult)
            Explode();

        Destroy(gameObject);
    }

    private void Explode()
    {
        foreach (Collider hit in Physics.OverlapSphere(transform.position, explosionRadius))
        {
            if (hit.attachedRigidbody != null)
                hit.attachedRigidbody.AddExplosionForce(650f, transform.position, explosionRadius, 1.2f);

            // El daño queda preparado para conectarlo al sistema de vida de enemigos.
            hit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void CreateVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(
            projectileType == ProjectileType.Knife ? PrimitiveType.Cube : PrimitiveType.Sphere);
        visual.name = "Projectile Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = projectileType switch
        {
            ProjectileType.Knife => new Vector3(.12f, .12f, .7f),
            ProjectileType.Catapult => Vector3.one * .8f,
            _ => Vector3.one * .35f
        };

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) Destroy(visualCollider);

        Renderer renderer = visual.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (renderer != null && shader != null)
        {
            renderer.material = new Material(shader)
            {
                color = projectileType switch
                {
                    ProjectileType.Flame => new Color(1f, .28f, .04f),
                    ProjectileType.Knife => new Color(.75f, .9f, 1f),
                    _ => new Color(.95f, .48f, .08f)
                }
            };
        }
    }
}
