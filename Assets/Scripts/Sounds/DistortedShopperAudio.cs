using UnityEngine;

public class DistortedShopperAudio : MonoBehaviour
{
    [Header("Loop Sound")]
    [SerializeField] private string loopSoundKey = "DistortedShopper";
    [SerializeField] private string deathSoundKey = "DistortedShopperDeath";
    [SerializeField] private bool playOnStart = true;

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 8f;
    [Range(0f, 1f)][SerializeField] private float spatialBlend = 1f;
    [Range(0f, 1f)][SerializeField] private float baseVolumeMultiplier = 0.35f;

    [Header("Anti Noise Spam")]
    [SerializeField] private int maxLoopingShoppers = 4;

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

    private void Start()
    {
        if (playOnStart)
        {
            StartLoop();
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
                0.8f
            );
        }
    }
}