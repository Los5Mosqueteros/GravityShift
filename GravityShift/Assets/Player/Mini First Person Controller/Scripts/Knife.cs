using UnityEngine;

public class Knife : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float damage = 50f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] attackSounds;
    
    private bool isAttacking = false;
    private bool canCombo = false;
    private bool inputQueued = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            Attack();
        }
        else if ((Input.GetMouseButton(0) || Input.GetMouseButtonDown(0)) && canCombo)
        {
            inputQueued = true;
        }
    }

    private void Attack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
    }

    public void EnableCombo()
    {
        canCombo = true;
    }

    public void CheckCombo()
    {
        if (inputQueued)
        {
            inputQueued = false;
            canCombo = false;
            animator.SetTrigger("Attack2");
        }
        else
        {
            canCombo = false;
            isAttacking = false;
        }
    }

    public void ResetAttack()
    {
        isAttacking = false;
        canCombo = false;
        inputQueued = false;
    }

    public void PlayAttackSound()
    {
        if (audioSource != null && attackSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, attackSounds.Length);
            audioSource.PlayOneShot(attackSounds[randomIndex]);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Aplply damage to enemy
    }

    public float GetDamage() => damage;
}
