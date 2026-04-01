using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 10f;

    private Vector2 moveDirection;
    private bool initialized = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnDisable()
    {
        // hid the room and destroy the bullet
        Destroy(gameObject);
    }

    public void SetDirection(Vector2 direction)
    {
        moveDirection = direction.normalized;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        if (collision.CompareTag("Player") ||
            (collision.transform.parent != null && collision.transform.parent.CompareTag("Player")))
        {
            PlayerHealth ph = collision.GetComponent<PlayerHealth>() ?? collision.GetComponentInParent<PlayerHealth>();

            if (ph != null)
            {
                ph.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}