using UnityEngine;

public class PewPewGuyAudio : MonoBehaviour
{
    [Header("Sound Keys")]
    [SerializeField] private string aimSoundKey = "PewPewAim";
    [SerializeField] private string fireSoundKey = "PewPewFire";
    [SerializeField] private string deathSoundKey = "PewPewDeath";

    [Header("3D Audio Settings")]
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Volume Multipliers")]
    [SerializeField] private float aimVolumeMultiplier = 0.8f;
    [SerializeField] private float fireVolumeMultiplier = 1.1f;
    [SerializeField] private float deathVolumeMultiplier = 1.2f;

    [Header("Anti Spam")]
    [SerializeField] private float aimSoundCooldown = 0.4f;
    [SerializeField] private float fireSoundCooldown = 0.08f;

    private EnemyCombat combat;
    private bool hasPlayedDeathSound = false;
    private float lastAimSoundTime = -999f;
    private float lastFireSoundTime = -999f;

    private void Awake()
    {
        combat = GetComponent<EnemyCombat>();
        if (combat != null)
        {
            combat.OnDeath += HandleDeath;
        }
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        PlayDeathSound();
    }

    public void PlayAimSound()
    {
        if (Time.time < lastAimSoundTime + aimSoundCooldown)
            return;

        lastAimSoundTime = Time.time;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundAtPosition(
                aimSoundKey,
                transform.position,
                aimVolumeMultiplier,
                minDistance,
                maxDistance
            );
        }
    }

    public void PlayFireSound()
    {
        if (Time.time < lastFireSoundTime + fireSoundCooldown)
            return;

        lastFireSoundTime = Time.time;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundAtPosition(
                fireSoundKey,
                transform.position,
                fireVolumeMultiplier,
                minDistance,
                maxDistance
            );
        }
    }

    public void PlayDeathSound()
    {
        if (hasPlayedDeathSound)
            return;

        hasPlayedDeathSound = true;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundAtPosition(
                deathSoundKey,
                transform.position,
                deathVolumeMultiplier,
                minDistance,
                maxDistance
            );
        }
    }
}