using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeatballPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 25f;

    [SerializeField] private bool useMeatballBrownTint = true;
    [SerializeField] private Color meatballTint = new Color(0.5f, 0.32f, 0.12f, 1f);

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (!c.isTrigger)
            c.isTrigger = true;

        GameplayDrawOrder.ApplyMeatball(gameObject);

        if (useMeatballBrownTint)
        {
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = meatballTint;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null)
            ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null)
            return;

        ph.Heal(healAmount);
        Destroy(gameObject);
    }
}
