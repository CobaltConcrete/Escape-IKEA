using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRunTimer : MonoBehaviour
{
    [SerializeField] private Text timerText;

    [Tooltip("Run time in seconds. When greater than zero, the HUD shows time remaining and the player loses if it hits zero before winning. At zero, the clock counts up with no time limit.")]
    [SerializeField] private float runTimeLimitSeconds = 300f;

    private float elapsed;
    private bool loseTriggered;

    private void Update()
    {
        if (loseTriggered) return;

        elapsed += Time.deltaTime;
        RefreshDisplay();

        if (runTimeLimitSeconds > 0f && elapsed >= runTimeLimitSeconds)
            TryLoseFromTimeout();
    }

    private void RefreshDisplay()
    {
        if (timerText == null) return;

        float displaySeconds = runTimeLimitSeconds > 0f
            ? Mathf.Max(0f, runTimeLimitSeconds - elapsed)
            : elapsed;

        int minutes = (int)(displaySeconds / 60f);
        int seconds = Mathf.FloorToInt(displaySeconds % 60f);
        timerText.text = $"{minutes}:{seconds:00}";
    }

    private void TryLoseFromTimeout()
    {
        if (loseTriggered) return;
        if (SceneManager.GetActiveScene().name != "IKEAscene") return;

        loseTriggered = true;
        PageManager.LoseGame();
    }

    public void BindTimerText(Text text)
    {
        timerText = text;
        elapsed = 0f;
        loseTriggered = false;
        RefreshDisplay();
    }
}
