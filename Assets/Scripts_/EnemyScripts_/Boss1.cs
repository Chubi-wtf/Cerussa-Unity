using UnityEngine;

public class Boss1 : MonoBehaviour
{
    // DetectPlayer variables
    public Transform player;
    public LayerMask whatIsPlayer; 

    // Projectile variables
    public GameObject projectile; 
    public Transform firePoint;

    // Attack variables
    public float timeBetweenAttacks; 
    bool alreadyAttacked; 
    public float attackRange; 
    public bool playerInAttackRange; 

    private void Awake()
    {
        player = GameObject.Find("Playerobj").transform;
    }

    private void Update()
    {
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (playerInAttackRange)
        {
            Attack();
        }
    }

    private void Attack()
    {
        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            Rigidbody rb = Instantiate(projectile, firePoint.position, Quaternion.identity).GetComponent<Rigidbody>();

            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);

            rb.AddForce(transform.up * 8f, ForceMode.Impulse);

            alreadyAttacked = true;

            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}