using UnityEngine;

public class BossAudio : MonoBehaviour
{
    [Header("Sound Keys")]
    [SerializeField] private string moveLoopKey = "Boss";
    [SerializeField] private string hitKey = "BossHit";
    [SerializeField] private string deathKey = "BossDeath";
    [SerializeField] private string throwKey = "BossThrow";

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 18f;
    [Range(0f, 1f)][SerializeField] private float spatialBlend = 1f;

    [Header("Volume Multipliers")]
    [SerializeField] private float moveVolumeMultiplier = 0.45f;
    [SerializeField] private float hitVolumeMultiplier = 1f;
    [SerializeField] private float deathVolumeMultiplier = 1.2f;
    [SerializeField] private float throwVolumeMultiplier = 0.8f;

    [Header("Anti Spam")]
    [SerializeField] private float throwCooldown = 0.6f;
    [SerializeField] private float hitCooldown = 0.08f;

    private AudioSource moveLoopSource;
    private EnemyCombat combat;
    private float lastThrowTime = -999f;
    private float lastHitTime = -999f;
    private bool hasPlayedDeath;

    private void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.OnDeath += HandleDeath;
            combat.OnDamaged += PlayHit;
        }
    }

    private void OnEnable()
    {
        StartMoveLoop();
    }

    private void OnDisable()
    {
        StopMoveLoop();
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnDeath -= HandleDeath;
            combat.OnDamaged -= PlayHit;
        }

        StopMoveLoop();
    }

    public void StartMoveLoop()
    {
        if (moveLoopSource != null)
            return;

        if (SoundManager.Instance == null)
            return;

        AudioClip clip = SoundManager.Instance.GetClip(moveLoopKey);
        if (clip == null)
            return;

        moveLoopSource = gameObject.AddComponent<AudioSource>();
        moveLoopSource.clip = clip;
        moveLoopSource.loop = true;
        moveLoopSource.playOnAwake = false;
        moveLoopSource.spatialBlend = spatialBlend;
        moveLoopSource.rolloffMode = AudioRolloffMode.Linear;
        moveLoopSource.minDistance = minDistance;
        moveLoopSource.maxDistance = maxDistance;

        float baseVolume = SoundManager.Instance.GetSoundVolume(moveLoopKey);
        moveLoopSource.volume = baseVolume * moveVolumeMultiplier;

        moveLoopSource.Play();
    }

    public void StopMoveLoop()
    {
        if (moveLoopSource == null)
            return;

        moveLoopSource.Stop();
        Destroy(moveLoopSource);
        moveLoopSource = null;
    }

    public void PlayHit()
    {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        lastHitTime = Time.time;

        SoundManager.Instance?.PlaySoundAtPosition(
            hitKey,
            transform.position,
            hitVolumeMultiplier,
            minDistance,
            maxDistance
        );
    }

    public void PlayThrow()
    {
        if (Time.time < lastThrowTime + throwCooldown)
            return;

        lastThrowTime = Time.time;

        SoundManager.Instance?.PlaySoundAtPosition(
            throwKey,
            transform.position,
            throwVolumeMultiplier,
            minDistance,
            maxDistance
        );
    }

    private void HandleDeath()
    {
        if (hasPlayedDeath)
            return;

        hasPlayedDeath = true;

        StopMoveLoop();

        SoundManager.Instance?.PlaySoundAtPosition(
            deathKey,
            transform.position,
            deathVolumeMultiplier,
            minDistance,
            maxDistance
        );
    }
}