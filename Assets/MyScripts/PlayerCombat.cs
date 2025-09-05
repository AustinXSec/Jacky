using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public static PlayerCombat Instance;

    [Header("References")]
    public Animator animator;
    public HeroKnight heroKnight;     // Assign your HeroKnight component in Inspector
    public Transform firePoint;       // Empty GameObject in front of player
    public GameObject[] slashPrefabs; // Assign 3 slash beam prefabs

    [Header("Attack Settings")]
    public float attackRange = 1.0f;
    public LayerMask enemyLayers;
    public int attackDamage = 40;

    [Header("Attack Timing")]
    public float attackDuration = 0.25f;
    public int hitFrames = 5;

    [Header("Attack Sounds")]
    public AudioClip[] attackSounds;
    public AudioSource audioSource;

    [Header("Special Attack Sound")]
    public AudioClip specialAttackSound; // Assign this in Inspector

    [Header("Special Attack")]
    public float spreadAngle = 15f;    // Spread in degrees
    [HideInInspector]
    public bool specialActive = false;

    private int currentAttack = 0;
    private bool isAttacking = false;
    private bool attackQueued = false;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isAttacking)
                attackQueued = true;
            else
                StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        attackQueued = false;

        currentAttack++;
        if (currentAttack > 3) currentAttack = 1;

        animator.SetTrigger("Attack" + currentAttack);

        // Play appropriate attack sound
        if (audioSource != null)
        {
            AudioClip clip = null;

            if (specialActive && specialAttackSound != null)
            {
                // Play special attack sound
                clip = specialAttackSound;
            }
            else if (attackSounds.Length > 0)
            {
                // Pick random normal attack sound
                clip = attackSounds[Random.Range(0, attackSounds.Length)];
            }

            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip, 1f);
            }
        }

        float interval = attackDuration / hitFrames;
        for (int i = 0; i < hitFrames; i++)
        {
            DealDamage();
            yield return new WaitForSeconds(interval);
        }

        isAttacking = false;

        if (attackQueued)
            StartCoroutine(PerformAttack());
    }

    void DealDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayers);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            IDamageable damageable = enemyCollider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(attackDamage, transform);
        }

        if (specialActive)
            ShootSlashBeam(currentAttack);
    }

    public void EnableSpecialAttack()
    {
        specialActive = true;
        StartCoroutine(SpecialTimer());
    }

    private IEnumerator SpecialTimer()
    {
        yield return new WaitForSeconds(30f);
        specialActive = false;

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.ResetMana();
    }

    void ShootSlashBeam(int attackIndex)
    {
        if (slashPrefabs.Length < 3 || firePoint == null) return;

        GameObject slash = slashPrefabs[attackIndex - 1];
        int beamCount = 3; // Number of beams per attack
        float angleStep = spreadAngle / (beamCount - 1);
        float startAngle = -spreadAngle / 2f;

        int facing = heroKnight.m_facingDirection; // 1 = right, -1 = left

        for (int i = 0; i < beamCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Quaternion rotation;

            if (facing == 1)
                rotation = firePoint.rotation * Quaternion.Euler(0, 0, angle);
            else
                rotation = firePoint.rotation * Quaternion.Euler(0, 180, -angle); // Flip for left

            GameObject proj = Instantiate(slash, firePoint.position, rotation);

            // Scale beam to match facing
            Vector3 scale = proj.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            proj.transform.localScale = scale;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
