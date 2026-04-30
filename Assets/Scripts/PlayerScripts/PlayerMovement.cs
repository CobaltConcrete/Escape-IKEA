using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    [Tooltip("How fast the player moves.")]
    public float speed = 5f;
    [SerializeField]
    [Tooltip("How much faster the player moves while roller boosting.")]
    private float rollerBoostMultiplier = 1.5f;

    private Rigidbody2D rb;
    private Vector2 move;
    private Vector2 lastMoveDirection = Vector2.down;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private SpriteRenderer animatedSpriteRenderer;

    private string currentAnimationState;
    private float attackAnimationRemaining;
    private string pendingAttackState;
    private bool pendingAttackFlipX;
    private bool rollerBoostActive;

    //Speed Pill Stuffs
    private float originalSpeed;
    private Coroutine speedCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animatedSpriteRenderer == null && animator != null)
            animatedSpriteRenderer = animator.GetComponent<SpriteRenderer>();

        UpdateAnimation();
    }

    void Update()
    {
        move.x = Input.GetAxisRaw("Horizontal");
        move.y = Input.GetAxisRaw("Vertical");

        if (attackAnimationRemaining > 0f)
            attackAnimationRemaining -= Time.deltaTime;

        // Normalize to prevent faster diagonal movement
        move.Normalize();

        if (move.sqrMagnitude > 0.001f)
            lastMoveDirection = move;

        UpdateAnimation();
    }

    void FixedUpdate()
    {
        // Move the player
        rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (attackAnimationRemaining > 0f && !string.IsNullOrEmpty(pendingAttackState))
        {
            if (animatedSpriteRenderer != null)
                animatedSpriteRenderer.flipX = pendingAttackFlipX;

            if (currentAnimationState != pendingAttackState)
            {
                animator.Play(pendingAttackState, 0, 0f);
                currentAnimationState = pendingAttackState;
            }
            return;
        }

        bool isMoving = move.sqrMagnitude > 0.001f;
        if (rollerBoostActive)
        {
            Vector2 rollerDirection = isMoving ? move : lastMoveDirection;
            string rollerState = GetRollerAnimationState(rollerDirection);
            if (animatedSpriteRenderer != null)
            {
                animatedSpriteRenderer.flipX =
                    Mathf.Abs(rollerDirection.x) > Mathf.Abs(rollerDirection.y) &&
                    rollerDirection.x > 0f;
            }
            if (!string.IsNullOrEmpty(rollerState) && PlayStateIfAvailable(rollerState))
                return;
        }

        string direction = GetAnimationDirection(isMoving ? move : lastMoveDirection);
        string nextState = direction + (isMoving ? "_WALKING" : "_IDLE");

        if (animatedSpriteRenderer != null)
            animatedSpriteRenderer.flipX = false;

        PlayStateIfAvailable(nextState);
    }

    private static string GetAnimationDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0f ? "Right" : "Left";

        return direction.y > 0f ? "Back" : "Front";
    }

    private static string GetRollerAnimationState(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return "Roller_LEFT";

        return direction.y > 0f ? "Roller_BACK" : "Roller_FRONT";
    }

    private bool PlayStateIfAvailable(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return false;

        int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
        if (!animator.HasState(0, fullPathHash))
            return false;

        if (currentAnimationState == stateName)
            return true;

        animator.Play(stateName);
        currentAnimationState = stateName;
        return true;
    }

    public void BoostSpeedFor10Seconds()
    {
        BoostSpeedMultiplierForDuration(rollerBoostMultiplier, 10f);
    }

    public void BoostSpeedMultiplierForDuration(float multiplier, float durationSeconds)
    {
        float baseSpeed = speedCoroutine != null ? originalSpeed : speed;
        BoostSpeedForDuration(baseSpeed * Mathf.Max(0f, multiplier), durationSeconds);
    }

    public Vector2 GetFacingDirection()
    {
        return lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection.normalized : Vector2.down;
    }

    public bool IsAttackAnimationPlaying()
    {
        return attackAnimationRemaining > 0f;
    }

    public void PlayAttackAnimation(float durationSeconds)
    {
        Vector2 facing = GetFacingDirection();
        string direction = GetAnimationDirection(facing);
        pendingAttackFlipX = false;

        switch (direction)
        {
            case "Back":
                pendingAttackState = "Back_ATTACK";
                break;
            case "Right":
                pendingAttackState = "Right_ATTACK";
                break;
            case "Left":
                pendingAttackState = "Left_ATTACK";
                pendingAttackFlipX = true;
                break;
            default:
                pendingAttackState = "Front_ATTACK";
                break;
        }

        attackAnimationRemaining = Mathf.Max(0.01f, durationSeconds);
        currentAnimationState = null;
        UpdateAnimation();
    }

    public void BoostSpeedForDuration(float boostedSpeed, float durationSeconds)
    {
        if (speedCoroutine != null)
            StopCoroutine(speedCoroutine);

        speedCoroutine = StartCoroutine(SpeedBoostRoutine(boostedSpeed, durationSeconds));
    }

    private IEnumerator SpeedBoostRoutine(float boostedSpeed, float durationSeconds)
    {
        originalSpeed = speed;
        rollerBoostActive = true;
        speed = boostedSpeed;
        currentAnimationState = null;
        UpdateAnimation();

        yield return new WaitForSeconds(durationSeconds);

        speed = originalSpeed;
        rollerBoostActive = false;
        currentAnimationState = null;
        UpdateAnimation();
        speedCoroutine = null;
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
