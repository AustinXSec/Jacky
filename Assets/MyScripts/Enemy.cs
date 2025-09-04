using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    public Animator animator;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;

    [Header("Attack")]
    public float attackRange = 1f;
    public int attackDamage = 20;
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    public Transform player;

    private Rigidbody2D rb;
    private Collider2D col;

    public bool isDead { get; private set; } = false;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 1f;
        col.isTrigger = false;
    }

    void Update()
    {
        if (isDead) return;

        if (player != null)
        {
            Vector2 direction = player.position - transform.position;
            transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange && Time.time >= lastAttackTime)
            {
                Attack();
                lastAttackTime = Time.time + attackCooldown;
            }
        }
    }

    void Attack()
    {
        animator.SetTrigger("Attack");

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Default"));
        foreach (Collider2D playerCollider in hitPlayers)
        {
            IDamageable damageable = playerCollider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(attackDamage, transform);
        }
    }

    public void TakeDamage(int damage, Transform attacker = null)
    {
        if (isDead) return;

        currentHealth -= damage;
        animator.SetTrigger("Hurt");

        if (attacker != null)
        {
            Vector2 knockbackDirection = (transform.position - attacker.position).normalized;
            StartCoroutine(ApplyKnockback(knockbackDirection));
        }

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator ApplyKnockback(Vector2 direction)
    {
        direction.y = 0f;
        direction.Normalize();

        float timer = 0f;
        while (timer < knockbackDuration)
        {
            rb.velocity = new Vector2(direction.x * knockbackForce, rb.velocity.y);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    void Die()
    {
        isDead = true;
        animator.SetBool("IsDead", true);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (col != null)
            col.isTrigger = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
