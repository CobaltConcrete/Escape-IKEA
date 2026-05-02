using System.Collections;
using UnityEngine;

public class AudioWarmup : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    private IEnumerator Start()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        if (source == null || source.clip == null)
            yield break;

        float oldVolume = source.volume;

        source.volume = 0f;
        source.Play();

        yield return null;

        source.Stop();
        source.volume = oldVolume;
    }
}