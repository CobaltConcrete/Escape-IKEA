using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class UIButtonSoundBinder : MonoBehaviour
{
    private static UIButtonSoundBinder instance;

    [Header("Button Sound")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;

    private AudioSource uiAudioSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        uiAudioSource = GetComponent<AudioSource>();
        if (uiAudioSource == null)
            uiAudioSource = gameObject.AddComponent<AudioSource>();

        uiAudioSource.playOnAwake = false;
        uiAudioSource.loop = false;
        uiAudioSource.spatialBlend = 0f;
        uiAudioSource.ignoreListenerPause = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(DelayedBind());
    }

    private IEnumerator DelayedBind()
    {
        yield return null; // µ»“ª÷°
        BindAllButtonsInScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindAllButtonsInScene();
    }

    private void BindAllButtonsInScene()
    {
        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button button in buttons)
        {
            UIButtonClickSound clickSound = button.GetComponent<UIButtonClickSound>();
            if (clickSound == null)
                clickSound = button.gameObject.AddComponent<UIButtonClickSound>();

            clickSound.Initialize(this);
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickClip == null || uiAudioSource == null)
            return;

        uiAudioSource.PlayOneShot(buttonClickClip, volume);
    }
}