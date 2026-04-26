using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneAudioController : MonoBehaviour
{
    [Header("Win Music")]
    [SerializeField] private string playerWinKey = "PlayerWin";
    [SerializeField] private float winFadeDuration = 0.35f;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        HandleScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.name);
    }

    private void HandleScene(string sceneName)
    {
        if (SoundManager.Instance == null)
            return;

        // 先确保不是全局暂停状态
        SoundManager.Instance.ResumeAllAudio();

        if (sceneName == "Win")
        {
            SoundManager.Instance.StopAllAudio();
            SoundManager.Instance.PlayMusicSoundWithFade(playerWinKey, winFadeDuration, false);
            return;
        }

        if (sceneName == "Lose")
        {
            SoundManager.Instance.StopAllAudio();
            return;
        }

        // 如果你真的有独立 Pause scene，才这样
        if (sceneName == "Pause")
        {
            SoundManager.Instance.PauseAllAudio();
            return;
        }
    }
}