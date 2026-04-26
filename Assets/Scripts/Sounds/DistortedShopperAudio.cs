using UnityEngine;

public class DistortedShopperAudio : MonoBehaviour
{
    [Header("Loop Sound")]
    [SerializeField] private string loopSoundKey = "DistortedShopper";
    [SerializeField] private string deathSoundKey = "DistortedShopperDeath";
    [SerializeField] private bool playOnStart = true;

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 15f;
    [Range(0f, 1f)][SerializeField] private float spatialBlend = 1f;
    [Range(0f, 1f)][SerializeField] private float baseVolumeMultiplier = 0.35f;

    [Header("Anti Noise Spam")]
    [SerializeField] private int maxLoopingShoppers = 4;

    [Header("Activation")]
    [SerializeField] private Transform listenerTarget;
    [SerializeField] private float startDistance = 10f;
    [SerializeField] private float stopDistance = 15f;
    [SerializeField] private float checkInterval = 0.25f;

    private float nextCheckTime;

    private static int activeLoopCount = 0;

    private AudioSource loopSource;
    private bool ownsLoopSlot = false;
    private EnemyCombat combat;
    private bool hasPlayedDeathSound = false;

    private void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.OnDeath += HandleDeath;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        if (listenerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                listenerTarget = player.transform;
        }

        if (listenerTarget == null)
            return;

        float distance = Vector2.Distance(transform.position, listenerTarget.position);

        if (distance <= startDistance)
        {
            StartLoop();
        }
        else if (distance >= stopDistance)
        {
            StopLoop();
        }
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnDeath -= HandleDeath;
        }

        StopLoop();
    }

    private void HandleDeath()
    {
        PlayDeathSound();
    }

    public void StartLoop()
    {
        if (loopSource != null)
            return;

        if (SoundManager.Instance == null)
            return;

        if (activeLoopCount >= maxLoopingShoppers)
            return;

        AudioClip clip = SoundManager.Instance.GetClip(loopSoundKey);
        if (clip == null)
            return;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = clip;
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = spatialBlend;
        loopSource.rolloffMode = AudioRolloffMode.Linear;
        loopSource.minDistance = minDistance;
        loopSource.maxDistance = maxDistance;

        float libraryVolume = SoundManager.Instance.GetSoundVolume(loopSoundKey);
        loopSource.volume = libraryVolume * baseVolumeMultiplier;

        loopSource.Play();

        ownsLoopSlot = true;
        activeLoopCount++;
    }

    public void StopLoop()
    {
        if (loopSource != null)
        {
            loopSource.Stop();
            Destroy(loopSource);
            loopSource = null;
        }

        if (ownsLoopSlot)
        {
            activeLoopCount = Mathf.Max(0, activeLoopCount - 1);
            ownsLoopSlot = false;
        }
    }

    public void PlayDeathSound()
    {
        if (hasPlayedDeathSound)
            return;

        hasPlayedDeathSound = true;

        Vector3 deathPosition = transform.position;

        StopLoop();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundAtPosition(
            deathSoundKey,
            deathPosition,
            1.2f,
            minDistance,
            maxDistance
 );
        }
    }
}