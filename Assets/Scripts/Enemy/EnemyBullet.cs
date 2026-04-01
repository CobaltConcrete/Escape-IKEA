using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 10f;

    private Vector2 moveDirection;
    private bool initialized = false;
    private bool useBounds;
    private bool bounceInBounds;
    private Vector2 minBounds;
    private Vector2 maxBounds;

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

    public void SetBounds(Bounds bounds, bool bounce)
    {
        minBounds = bounds.min;
        maxBounds = bounds.max;
        useBounds = true;
        bounceInBounds = bounce;
    }

    private void Update()
    {
        if (!initialized) return;

        Vector3 pos3 = transform.position + (Vector3)(moveDirection * speed * Time.deltaTime);
        if (useBounds)
        {
            Vector2 pos = pos3;
            if (pos.x < minBounds.x)
            {
                pos.x = minBounds.x;
                if (bounceInBounds) moveDirection.x = Mathf.Abs(moveDirection.x);
            }
            else if (pos.x > maxBounds.x)
            {
                pos.x = maxBounds.x;
                if (bounceInBounds) moveDirection.x = -Mathf.Abs(moveDirection.x);
            }

            if (pos.y < minBounds.y)
            {
                pos.y = minBounds.y;
                if (bounceInBounds) moveDirection.y = Mathf.Abs(moveDirection.y);
            }
            else if (pos.y > maxBounds.y)
            {
                pos.y = maxBounds.y;
                if (bounceInBounds) moveDirection.y = -Mathf.Abs(moveDirection.y);
            }

            pos3 = new Vector3(pos.x, pos.y, transform.position.z);
        }

        transform.position = pos3;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Wall"))
        {
            if (useBounds && bounceInBounds) return;
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