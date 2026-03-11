using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    [Tooltip("How fast the player moves.")]
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 move;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");

        // Normalize to prevent faster diagonal movement
        move.Normalize();
    }

    void FixedUpdate()
    {
        // Move the player
        rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);
    }

    // Collide with walls
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Wall sound
        }
    }

    // Go through doors
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Door"))
        {
            // Door sound
        }
    }
}