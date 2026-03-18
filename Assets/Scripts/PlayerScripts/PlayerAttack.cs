using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private KeyCode attackKey = KeyCode.J;

    private void Update()
    {
        if (!WasAttackPressed())
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
        foreach (Collider2D col in hits)
        {
            Enemy e = col.GetComponent<Enemy>();
            if (e != null)
                e.TakeDamage(attackDamage);
        }
    }

    private bool WasAttackPressed()
    {
        return Input.GetKeyDown(attackKey);
    }
}

