using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRunTimer : MonoBehaviour
{
    [SerializeField] private Text timerText;

    [Tooltip("Run time in seconds. When greater than zero, the HUD shows time remaining and the player loses if it hits zero before winning. At zero, the clock counts up with no time limit.")]
    [HideInInspector] public float runTimeLimitSeconds = 300f;
    [HideInInspector] public int maxNumBlackout = 2;
    [SerializeField] private int runTimeMinutes = 5;
    [SerializeField] private int runTimeSeconds = 0;

    private float elapsed;
    private bool loseTriggered;

    private void Awake()
    {
        RecalculateTotalSeconds();
        RefreshDisplay();
    }
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

    private void RecalculateTotalSeconds()
    {
        runTimeLimitSeconds = Mathf.Max(0f, runTimeMinutes * 60f + runTimeSeconds);
    }

    public void SetTimerLimit(float limit)
    {
        runTimeLimitSeconds = limit;
        runTimeMinutes = (int)(runTimeLimitSeconds/60);
    }

    public void SetMaxBlackoutLimit(int limit)
    {
        maxNumBlackout = limit;
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
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (runTimeSeconds >= 60)
        {
            runTimeMinutes += runTimeSeconds / 60;
            runTimeSeconds %= 60;
        }

        if (runTimeSeconds < 0) runTimeSeconds = 0;
        if (runTimeMinutes < 0) runTimeMinutes = 0;

        RecalculateTotalSeconds();
    }
#endif
}
