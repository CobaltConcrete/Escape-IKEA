using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RoomLightBuzz : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Clip")]
    [SerializeField] private AudioClip buzzClip;
    [SerializeField] private bool playOnStart = true;

    [Header("Distance")]
    [SerializeField] private float maxHearDistance = 7f;

    [Header("Volume")]
    [SerializeField][Range(0f, 1f)] private float maxVolume = 0.10f;
    [SerializeField] private float smoothTime = 0.35f;

    [Header("Volume Curve")]
    [SerializeField]
    private AnimationCurve volumeByDistance = new AnimationCurve(
        new Keyframe(0f, 0.55f),
        new Keyframe(0.15f, 0.52f),
        new Keyframe(0.35f, 0.42f),
        new Keyframe(0.60f, 0.24f),
        new Keyframe(0.82f, 0.08f),
        new Keyframe(1f, 0f)
    );

    private AudioSource audioSource;
    private float currentTargetVolume;
    private float currentVolumeVelocity;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        audioSource.spatialBlend = 0.75f;
        audioSource.volume = 0f;

        if (buzzClip != null)
        {
            audioSource.clip = buzzClip;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (playOnStart && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        UpdateTargetVolume();

        audioSource.volume = Mathf.SmoothDamp(
            audioSource.volume,
            currentTargetVolume,
            ref currentVolumeVelocity,
            smoothTime
        );
    }

    private void UpdateTargetVolume()
    {
        if (player == null)
        {
            currentTargetVolume = 0f;
            return;
        }

        float distance = Vector2.Distance(player.position, transform.position);

        if (distance >= maxHearDistance)
        {
            currentTargetVolume = 0f;
            return;
        }

        float normalizedDistance = Mathf.Clamp01(distance / maxHearDistance);

        float curveValue = volumeByDistance.Evaluate(normalizedDistance);

        currentTargetVolume = curveValue * maxVolume;
    }
    public void StopBuzz()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxHearDistance);
    }
#endif
}