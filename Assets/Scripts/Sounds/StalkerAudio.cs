using UnityEngine;

public class StalkerAudio : MonoBehaviour
{
    [Header("Sound Keys")]
    [SerializeField] private string walkerLoopKey = "StalkerWalker";
    [SerializeField] private string transformKey = "StalkerTurnToSpider";
    [SerializeField] private string spiderLoopKey = "SpiderCrawling";
    [SerializeField] private string deathKey = "SpiderDeath";

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 15f;
    [Range(0f, 1f)][SerializeField] private float spatialBlend = 1f;
    [Range(0f, 1f)][SerializeField] private float loopVolumeMultiplier = 0.45f;

    private AudioSource loopSource;
    private EnemyCombat combat;
    private bool hasPlayedDeath;

    private void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        if (combat != null)
            combat.OnDeath += HandleDeath;
    }

    private void Start()
    {
        StartWalkerLoop();
    }

    private void OnDestroy()
    {
        if (combat != null)
            combat.OnDeath -= HandleDeath;

        StopLoop();
    }

    public void StartWalkerLoop()
    {
        StartLoop(walkerLoopKey);
    }

    public void StartSpiderLoop()
    {
        StartLoop(spiderLoopKey);
    }

    public void PlayTransformSound()
    {
        PlayOneShot(transformKey, 1f);
    }

    private void HandleDeath()
    {
        if (hasPlayedDeath)
            return;

        hasPlayedDeath = true;
        StopLoop();
        PlayOneShot(deathKey, 1.2f);
    }

    private void StartLoop(string key)
    {
        StopLoop();

        if (SoundManager.Instance == null)
            return;

        AudioClip clip = SoundManager.Instance.GetClip(key);
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

        float libraryVolume = SoundManager.Instance.GetSoundVolume(key);
        loopSource.volume = libraryVolume * loopVolumeMultiplier;

        loopSource.Play();
    }

    private void StopLoop()
    {
        if (loopSource != null)
        {
            loopSource.Stop();
            Destroy(loopSource);
            loopSource = null;
        }
    }

    private void PlayOneShot(string key, float volumeMultiplier)
    {
        if (SoundManager.Instance == null)
            return;

        SoundManager.Instance.PlaySoundAtPosition(
            key,
            transform.position,
            volumeMultiplier,
            minDistance,
            maxDistance
        );
    }
}