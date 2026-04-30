using UnityEngine;

public class LoseSceneBGM : MonoBehaviour
{
    [SerializeField] private string loseSoundKey = "PlayerDeath";
    [SerializeField] private float fadeDuration = 0.6f;

    private void Start()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogError("[LoseSceneBGM] No SoundManager found.");
            return;
        }

        SoundManager.Instance.StopAllAudio();
        SoundManager.Instance.PlayMusicSoundWithFade(loseSoundKey, fadeDuration, false);
    }
}