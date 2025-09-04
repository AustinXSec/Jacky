using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;

    [Header("References")]
    public Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Health UI")]
    public Image healthBar;      // Assign your health bar Image (filled type)
    public Text healthText;      // Optional: shows "current/max"

    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        UpdateHealthUI();
    }

    public void TakeDamage(int damage, Transform attacker = null)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log("Player Health: " + currentHealth);
        animator?.SetTrigger("Hurt");

        if (attacker != null)
        {
            Vector2 knockbackDirection = (transform.position - attacker.position).normalized;
            StartCoroutine(ApplyKnockback(knockbackDirection));
        }

        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private IEnumerator ApplyKnockback(Vector2 direction)
    {
        float timer = 0f;
        while (timer < knockbackDuration)
        {
            rb.velocity = new Vector2(direction.x * knockbackForce, rb.velocity.y);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        animator?.SetTrigger("Death");
        rb.velocity = new Vector2(0, rb.velocity.y);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Disable other player scripts if needed
        var hero = GetComponent<HeroKnight>();
        if (hero != null) hero.enabled = false;

        var combat = GetComponent<PlayerCombat>();
        if (combat != null) combat.enabled = false;

        StartCoroutine(FreezeWhenGrounded());
    }

    private IEnumerator FreezeWhenGrounded()
    {
        while (!IsGrounded())
            yield return null;

        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
}
