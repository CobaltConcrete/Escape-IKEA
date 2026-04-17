using System.Collections;
using TMPro;
using UnityEngine;

public class BossRoomNoticeUI : MonoBehaviour
{
    public static BossRoomNoticeUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private float defaultShowDuration = 2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (noticeText == null)
        {
            noticeText = GetComponent<TextMeshProUGUI>();
        }

        HideImmediate();
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultShowDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (noticeText == null) return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        noticeText.gameObject.SetActive(false);
        currentRoutine = null;
    }

    public void HideImmediate()
    {
        if (noticeText != null)
        {
            noticeText.text = "";
            noticeText.gameObject.SetActive(false);
        }
    }
}