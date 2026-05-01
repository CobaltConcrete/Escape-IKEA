using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance = null;

    private GameObject[] gameUIObjects;

    [Header("Blackout UI")]
    [SerializeField] public GameObject blackOutScreen;
    [SerializeField] public GameObject blackOutText;
    [SerializeField] public GameObject gameTimer;

    [Header("Blackout Timing")]
    [SerializeField] private float minBlackoutInterval = 30f;
    [SerializeField] private float maxBlackoutInterval = 60f;

    [SerializeField] private float minFullDarkDuration = 10f;
    [SerializeField] private float maxFullDarkDuration = 20f;

    [SerializeField] private float minFlickerInterval = 0.5f;
    [SerializeField] private float maxFlickerInterval = 2f;
    [SerializeField] private float flickerDuration = 0.05f;
    [SerializeField] private float flickerRecoverTime = -1f;
    [SerializeField] private float minFlickerDarkIntensity = 0f;
    [SerializeField] private float maxFlickerDarkIntensity = 0.3f;
    [SerializeField] private float restoreLightBeforeFullDark = 1f;
    [SerializeField] private float fullBlackBeforePlayerLightDuration = 1f;

    [Header("Blackout Cooldown")]
    [SerializeField] private float blackoutCooldownAfterLightsOn = 20f;

    private bool isRecoveringFromBlackout = false;
    private bool isLightsOnTransition = false;
    private float nextAllowedBlackoutTime = 0f;

    [Header("Sound-to-Action Delay")]
    [SerializeField] private float lightsOffActionDelay = 9.7f;
    [SerializeField] private float lightsOnActionDelay = 3.1f;

    [Header("Blackout Visual")]
    [Range(0f, 1f)][SerializeField] private float fullDarkAlpha = 1f;

    [Header("2D Lights")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Light2D playerLight;
    [SerializeField] private float normalGlobalLightIntensity = 1f;
    [SerializeField] private float flickerLowGlobalLightIntensity = 0.5f;
    [SerializeField] private float blackoutGlobalLightIntensity = 0f;
    [SerializeField] private float playerLightBlackoutIntensity = 0.6f;

    [Header("Sound Keys")]
    [SerializeField] private string lightsOffKey = "LightsOff";
    [SerializeField] private string lightsOnKey = "LightsOn";
    [SerializeField] private string flashLightKey = "FlashLight";
    [SerializeField] private string lightBuzzingKey = "LightBuzzing";

    [Header("DEBUG Blackout Test")]
    [SerializeField] private bool enableDebugBlackoutToggle = true;
    [SerializeField] private KeyCode debugBlackoutKey = KeyCode.B;

    private enum BlackoutState
    {
        None,
        WarningFlicker,
        FullDark
    }

    private BlackoutState blackoutState = BlackoutState.None;

    private Image blackImage;

    private float runTimer = 0f;
    private float stateTimer = 0f;
    private float nextBlackoutTime = 0f;
    private float nextFlickerTime = 0f;
    private float currentFullDarkDuration = 10f;

    private bool isBlackout = false;
    private bool flickerLightLow = false;
    private bool lightBuzzingEnabled = true;
    private bool hasPlayedFlashlightSound = false;

    private Coroutine lightsOnRoutine;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "IKEAscene")
            return;

        globalLight = GameObject.Find("Global Light 2D")?.GetComponent<Light2D>();
        playerLight = GameObject.Find("Player")?.GetComponentInChildren<Light2D>();

        runTimer = 0f;
        stateTimer = 0f;
        blackoutState = BlackoutState.None;
        isBlackout = false;
        flickerLightLow = false;
        hasPlayedFlashlightSound = false;
        isRecoveringFromBlackout = false;
        isLightsOnTransition = false;

        if (blackOutScreen != null)
        {
            blackImage = blackOutScreen.GetComponent<Image>();
            SetBlackScreen(false, 0f);
        }

        if (blackOutText != null)
            blackOutText.SetActive(false);

        RestoreNormalLightingImmediate();
        SetLightBuzzing(true);
        ScheduleNextBlackout();
    }

    private void Update()
    {
        if (enableDebugBlackoutToggle && Input.GetKeyDown(debugBlackoutKey))
        {
            if (CanManualStartLightsOn())
            {
                EndBlackout();
            }
            else if (CanManualStartLightsOff())
            {
                StartBlackoutWarning();
            }
        }

        runTimer += Time.deltaTime;

        HandlePauseInput();

        if (!isBlackout &&
    !isRecoveringFromBlackout &&
    runTimer >= nextAllowedBlackoutTime &&
    runTimer >= nextBlackoutTime)
        {
            StartBlackoutWarning();
        }

        if (isBlackout)
        {
            UpdateBlackout();
        }
    }

    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.P) &&
            SceneManager.GetActiveScene().name == "IKEAscene" &&
            !SceneManager.GetSceneByName("Pause").isLoaded)
        {
            SetBlackScreen(false, 0f);

            if (blackOutText != null)
                blackOutText.SetActive(false);

            PauseGame();
        }

        if (Input.GetKeyDown(KeyCode.R) && SceneManager.GetSceneByName("Pause").isLoaded)
        {
            ResumeGame();
        }
    }

    private void ScheduleNextBlackout()
    {
        float min = Mathf.Min(minBlackoutInterval, maxBlackoutInterval);
        float max = Mathf.Max(minBlackoutInterval, maxBlackoutInterval);

        nextBlackoutTime = runTimer + Random.Range(min, max);
    }

    private void StartBlackoutWarning()
    {
        if (isBlackout)
            return;

        if (lightsOnRoutine != null)
        {
            StopCoroutine(lightsOnRoutine);
            lightsOnRoutine = null;
        }

        isBlackout = true;
        blackoutState = BlackoutState.WarningFlicker;
        stateTimer = 0f;
        nextFlickerTime = Time.time + GetRandomFlickerInterval();
        flickerLightLow = false;
        hasPlayedFlashlightSound = false;

        currentFullDarkDuration = Random.Range(
            Mathf.Min(minFullDarkDuration, maxFullDarkDuration),
            Mathf.Max(minFullDarkDuration, maxFullDarkDuration)
        );

        if (blackOutText != null)
            blackOutText.SetActive(true);

        SetBlackScreen(false, 0f);

        if (playerLight != null)
        {
            playerLight.intensity = 0f;
            playerLight.enabled = false;
        }

        SoundManager.Instance?.PlaySound(lightsOffKey, 1.2f);
    }

    private void UpdateBlackout()
    {
        stateTimer += Time.deltaTime;

        switch (blackoutState)
        {
            case BlackoutState.WarningFlicker:
                UpdateWarningFlicker();
                break;

            case BlackoutState.FullDark:
                UpdateFullDark();
                break;
        }
    }
    private float GetRandomFlickerInterval()
{
    return Random.Range(
        Mathf.Min(minFlickerInterval, maxFlickerInterval),
        Mathf.Max(minFlickerInterval, maxFlickerInterval)
    );
}
    private void UpdateWarningFlicker()
    {
        float timeUntilFullDark = lightsOffActionDelay - stateTimer;

        if (timeUntilFullDark <= restoreLightBeforeFullDark)
        {
            if (globalLight != null)
                globalLight.intensity = normalGlobalLightIntensity;

            SetLightBuzzing(true);
            flickerLightLow = false;
            flickerRecoverTime = -1f;

            if (stateTimer >= lightsOffActionDelay)
            {
                EnterFullDark();
            }

            return;
        }

        if (flickerLightLow && Time.time >= flickerRecoverTime)
        {
            flickerLightLow = false;
            flickerRecoverTime = -1f;

            if (globalLight != null)
                globalLight.intensity = normalGlobalLightIntensity;

            SetLightBuzzing(true);
        }

        if (!flickerLightLow && Time.time >= nextFlickerTime)
        {
            flickerLightLow = true;

            float darkIntensity = Random.Range(
                Mathf.Min(minFlickerDarkIntensity, maxFlickerDarkIntensity),
                Mathf.Max(minFlickerDarkIntensity, maxFlickerDarkIntensity)
            );

            if (globalLight != null)
                globalLight.intensity = darkIntensity;

            SetLightBuzzing(false);

            flickerRecoverTime = Time.time + flickerDuration;
            nextFlickerTime = Time.time + GetRandomFlickerInterval();
        }

        if (stateTimer >= lightsOffActionDelay)
        {
            EnterFullDark();
        }
    }

    private void EnterFullDark()
    {
        blackoutState = BlackoutState.FullDark;
        stateTimer = 0f;
        hasPlayedFlashlightSound = false;

        SetBlackScreen(true, fullDarkAlpha);
        EnterDarkNoPlayerLight();
    }

    private void UpdateFullDark()
    {
        if (stateTimer < fullBlackBeforePlayerLightDuration)
        {
            EnterDarkNoPlayerLight();
            return;
        }

        if (!hasPlayedFlashlightSound)
        {
            hasPlayedFlashlightSound = true;
            SoundManager.Instance?.PlaySound(flashLightKey, 1f);
        }

        SetBlackScreen(false, 0f);
        EnterDarkWithPlayerLight();

        if (stateTimer >= currentFullDarkDuration)
        {
            EndBlackout();
        }
    }

    private void EndBlackout()
    {
        if (!isBlackout)
            return;

        isBlackout = false;
        isRecoveringFromBlackout = true;
        isLightsOnTransition = true;
        blackoutState = BlackoutState.None;
        stateTimer = 0f;
        flickerLightLow = false;
        hasPlayedFlashlightSound = false;

        SoundManager.Instance?.PlaySound(lightsOnKey, 1f);

        if (lightsOnRoutine != null)
            StopCoroutine(lightsOnRoutine);

        lightsOnRoutine = StartCoroutine(CoEndBlackoutAfterLightsOnDelay());
    }

    private IEnumerator CoEndBlackoutAfterLightsOnDelay()
    {
        yield return new WaitForSeconds(lightsOnActionDelay);

        SetBlackScreen(false, 0f);

        if (blackOutText != null)
            blackOutText.SetActive(false);

        RestoreNormalLightingImmediate();

        nextAllowedBlackoutTime = runTimer + blackoutCooldownAfterLightsOn;

        float min = Mathf.Min(minBlackoutInterval, maxBlackoutInterval);
        float max = Mathf.Max(minBlackoutInterval, maxBlackoutInterval);

        nextBlackoutTime = nextAllowedBlackoutTime + Random.Range(min, max);

        isLightsOnTransition = false;
        lightsOnRoutine = null;
    }

    private void EnterDarkNoPlayerLight()
    {
        if (globalLight != null)
            globalLight.intensity = blackoutGlobalLightIntensity;

        if (playerLight != null)
        {
            playerLight.intensity = 0f;
            playerLight.enabled = false;
        }

        SetLightBuzzing(false);
    }

    private void EnterDarkWithPlayerLight()
    {
        if (globalLight != null)
            globalLight.intensity = blackoutGlobalLightIntensity;

        if (playerLight != null)
        {
            playerLight.enabled = true;
            playerLight.intensity = playerLightBlackoutIntensity;
        }

        SetLightBuzzing(false);
    }

    private void RestoreNormalLightingImmediate()
    {
        if (globalLight != null)
            globalLight.intensity = normalGlobalLightIntensity;

        if (playerLight != null)
        {
            playerLight.intensity = 0f;
            playerLight.enabled = false;
        }

        SetLightBuzzing(true);
    }

    private void SetBlackScreen(bool active, float alpha)
    {
        if (blackOutScreen == null)
            return;

        blackOutScreen.SetActive(active);

        if (blackImage != null)
        {
            Color c = blackImage.color;
            blackImage.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void SetLightBuzzing(bool enabled)
    {
        if (lightBuzzingEnabled == enabled)
            return;

        lightBuzzingEnabled = enabled;

        AudioClip buzzingClip = SoundManager.Instance != null
            ? SoundManager.Instance.GetClip(lightBuzzingKey)
            : null;

        AudioSource[] sources = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (AudioSource source in sources)
        {
            if (source == null)
                continue;

            bool isBuzzingSource =
                buzzingClip != null && source.clip == buzzingClip;

            if (!isBuzzingSource)
                continue;

            if (enabled)
            {
                source.mute = false;

                if (source.loop && !source.isPlaying)
                    source.Play();
            }
            else
            {
                source.mute = true;
            }
        }
    }

    #region Scene_transitions

    public void StartGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("IKEAscene");
    }

    public void SelectLevel()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("LevelSelection");
    }

    public void PauseGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        gameUIObjects = GameObject.FindGameObjectsWithTag("UI");
        foreach (GameObject obj in gameUIObjects)
            obj.SetActive(false);

        SceneManager.LoadSceneAsync("Pause", LoadSceneMode.Additive).completed += (op) =>
        {
            Time.timeScale = 0f;
            SoundManager.Instance?.PauseAllAudio();

            var resumeButton = GameObject.Find("ResumeButton");
            if (resumeButton != null)
            {
                var button = resumeButton.GetComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(ResumeGame);
            }

            var exitButton = GameObject.Find("ExitGameButton");
            if (exitButton != null)
            {
                exitButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(MainMenu);
            }
        };
    }

    public void ResumeGame()
    {
        if (this != Instance)
        {
            Instance.ResumeGame();
            return;
        }

        Time.timeScale = 1f;
        SoundManager.Instance?.ResumeAllAudio();
        SceneManager.UnloadSceneAsync("Pause");

        if (gameUIObjects != null)
        {
            foreach (GameObject obj in gameUIObjects)
                obj.SetActive(true);

            gameUIObjects = null;
        }
    }

    public static void LoseGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Lose");
    }

    public void WinGame()
    {
        PlayerPrefs.SetInt("LastRunValue", RunObjectiveManager.Instance.CurrentCollectedValue);
        PlayerPrefs.Save();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Win");
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        CancelBlackoutState();
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopMusic();
            SoundManager.Instance.StopAmbient();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
    private void CancelBlackoutState()
    {
        if (lightsOnRoutine != null)
        {
            StopCoroutine(lightsOnRoutine);
            lightsOnRoutine = null;
        }

        isBlackout = false;
        isRecoveringFromBlackout = false;
        isLightsOnTransition = false;
        blackoutState = BlackoutState.None;
        stateTimer = 0f;
        flickerLightLow = false;
        hasPlayedFlashlightSound = false;

        SetBlackScreen(false, 0f);

        if (blackOutText != null)
            blackOutText.SetActive(false);

        RestoreNormalLightingImmediate();
    }

    #endregion

    #region BlackoutQueries

    public bool IsBlackoutActive()
    {
        return isBlackout;
    }

    public bool IsPureBlackoutActive()
    {
        return isBlackout &&
               blackoutState == BlackoutState.FullDark &&
               stateTimer < fullBlackBeforePlayerLightDuration;
    }

    public bool IsPlayerVisionBlackoutActive()
    {
        return isBlackout &&
               blackoutState == BlackoutState.FullDark &&
               stateTimer >= fullBlackBeforePlayerLightDuration;
    }

    #endregion

    private bool CanManualStartLightsOff()
    {
        return !isBlackout && !isLightsOnTransition;
    }

    private bool CanManualStartLightsOn()
    {
        return isBlackout &&
               blackoutState == BlackoutState.FullDark &&
               stateTimer >= fullBlackBeforePlayerLightDuration &&
               !isLightsOnTransition;
    }

    private void ForceResetBlackoutState()
    {
        if (lightsOnRoutine != null)
        {
            StopCoroutine(lightsOnRoutine);
            lightsOnRoutine = null;
        }

        isBlackout = false;
        isRecoveringFromBlackout = false;
        isLightsOnTransition = false;
        blackoutState = BlackoutState.None;

        stateTimer = 0f;
        flickerLightLow = false;
        hasPlayedFlashlightSound = false;

        SetBlackScreen(false, 0f);

        if (blackOutText != null)
            blackOutText.SetActive(false);

        if (globalLight != null)
            globalLight.intensity = normalGlobalLightIntensity;

        if (playerLight != null)
        {
            playerLight.intensity = 0f;
            playerLight.enabled = false;
        }

        SetLightBuzzing(true);
    }
}