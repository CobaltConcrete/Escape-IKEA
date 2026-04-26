using UnityEngine;

public class WinSceneBGM : MonoBehaviour
{
    [SerializeField] private string winMusicKey = "PlayerWin";
    [SerializeField] private float fadeDuration = 0.6f;

    private void Start()
    {
        SoundManager.Instance?.PlayMusicSoundWithFade(winMusicKey, fadeDuration, false);
    }
}