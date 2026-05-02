using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPointerDownSound : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource source;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickClip != null)
            source.PlayOneShot(clickClip, volume);
    }
}