using UnityEngine;
using UnityEngine.UI;

public class CountdownBGMController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text runTimerText;

    [Header("Music Keys")]
    [SerializeField] private string countdown1Key = "Countdown1";
    [SerializeField] private string countdown2Key = "Countdown2";
    [SerializeField] private string countdown3Key = "Countdown3";

    [Header("Trigger Times")]
    [SerializeField] private float countdown1Time = 216f; // 1:26 + 1:05 + 1:05
    [SerializeField] private float countdown2Time = 151f; // 1:26 + 1:05
    [SerializeField] private float countdown3Time = 86f;  // 1:26

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.35f;

    private bool playedCountdown1;
    private bool playedCountdown2;
    private bool playedCountdown3;

    private void Update()
    {
        if (runTimerText == null)
            return;

        if (!TryParseTimer(runTimerText.text, out float remainingSeconds))
            return;

        if (!playedCountdown1 && remainingSeconds <= countdown1Time)
        {
            SoundManager.Instance?.PlayMusicSoundWithFade(countdown1Key, fadeDuration, true);
            playedCountdown1 = true;
        }

        if (!playedCountdown2 && remainingSeconds <= countdown2Time)
        {
            SoundManager.Instance?.PlayMusicSoundWithFade(countdown2Key, fadeDuration, true);
            playedCountdown2 = true;
        }

        if (!playedCountdown3 && remainingSeconds <= countdown3Time)
        {
            SoundManager.Instance?.PlayMusicSoundWithFade(countdown3Key, fadeDuration, true);
            playedCountdown3 = true;
        }
    }

    private bool TryParseTimer(string timerText, out float seconds)
    {
        seconds = 0f;

        if (string.IsNullOrWhiteSpace(timerText))
            return false;

        string[] parts = timerText.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out int minutes))
            return false;

        if (!int.TryParse(parts[1], out int secs))
            return false;

        seconds = minutes * 60 + secs;
        return true;
    }
}