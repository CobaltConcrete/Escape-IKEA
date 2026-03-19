using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    [Tooltip("How fast the player moves.")]
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 move;

    //Speed Pill Stuffs
    private float originalSpeed;
    private Coroutine speedCoroutine;

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

    public void BoostSpeedFor10Seconds()
    {
        speedCoroutine = StartCoroutine(SpeedBoostRoutine());
    }

    private IEnumerator SpeedBoostRoutine()
    {
        originalSpeed = speed;

        speed = 8f;

        yield return new WaitForSeconds(10f);

        speed = originalSpeed;
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