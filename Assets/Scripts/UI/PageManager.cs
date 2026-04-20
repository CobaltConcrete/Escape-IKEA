using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance = null;
    private GameObject[] gameUIObjects;

    private bool blackOut = false;
    private float blackOutTimer = 0;
    [SerializeField] public GameObject blackOutScreen;
    [SerializeField] public GameObject blackOutText;

    [Header("Instructions")]
    [SerializeField] private KeyCode helpKey = KeyCode.H;
    [SerializeField] private string instructionsSceneName = "Instructions";

    private float timeScaleBeforeInstructions = 1f;
    private bool instructionsOpen;
    private bool instructionsOpenedFromGame;

    #region Unity_functions
    
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
        if (scene.name == "IKEAscene")
        {
            blackOutTimer = 0f;
            blackOutScreen = GameObject.Find("BlackoutScreen");
            blackOutText = GameObject.Find("BlackoutText");
        }
        else if (scene.name == "MainMenu")
        {
            EnsureMainMenuInstructionsButton();
        }
    }

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

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
            EnsureMainMenuInstructionsButton();
    }

    private void Update() {
        if (Input.GetKeyDown(helpKey) &&
            SceneManager.GetActiveScene().name == "IKEAscene" &&
            !SceneManager.GetSceneByName("Pause").isLoaded)
        {
            if (SceneManager.GetSceneByName(instructionsSceneName).isLoaded)
                CloseInstructions();
            else
                OpenInstructions();
        }

        if (instructionsOpen && instructionsOpenedFromGame)
            return;

        blackOutTimer += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.P) && SceneManager.GetActiveScene().name == "IKEAscene" && !SceneManager.GetSceneByName("Pause").isLoaded)
        {
            if (blackOutScreen != null)
                blackOutScreen.SetActive(false);
            if (blackOutText != null)
                blackOutText.SetActive(false);
            PauseGame();
        }

        if (Input.GetKeyDown(KeyCode.R) && SceneManager.GetSceneByName("Pause").isLoaded)
        {
            ResumeGame();
        }

        if ((blackOutTimer > 75.0f && blackOutTimer < 85.0f) || (blackOutTimer > 150.0f && blackOutTimer < 160.0f)){
            if (blackOutScreen != null)
                blackOutScreen.SetActive(true);
            if (blackOutText != null)
                blackOutText.SetActive(true);
        }
        else{
            if (blackOutScreen != null)
                blackOutScreen.SetActive(false);
            if (blackOutText != null)
                blackOutText.SetActive(false);
        }
    }
    #endregion

    #region Scene_transitions
    public void StartGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("IKEAscene");
    }


    public void PauseGame()
    {
        if (instructionsOpen)
            CloseInstructions();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        gameUIObjects = GameObject.FindGameObjectsWithTag("UI");
        foreach (GameObject obj in gameUIObjects)
            obj.SetActive(false);

        SceneManager.LoadSceneAsync("Pause", LoadSceneMode.Additive).completed += (op) =>
        {
            Time.timeScale = 0f;

            var resumeButton = GameObject.Find("ResumeButton");
            Debug.Log("ResumeButton found: " + (resumeButton != null));
            if (resumeButton != null)
            {
                var button = resumeButton.GetComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => Debug.Log("CLICKED"));
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
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        if (this != Instance) { Instance.ResumeGame(); return; }

        if (instructionsOpen)
            CloseInstructions();

        Time.timeScale = 1f;
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
        if (instructionsOpen)
            CloseInstructions();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenInstructions()
    {
        if (SceneManager.GetSceneByName(instructionsSceneName).isLoaded)
            return;

        instructionsOpenedFromGame = SceneManager.GetActiveScene().name == "IKEAscene";
        timeScaleBeforeInstructions = Time.timeScale;
        instructionsOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (instructionsOpenedFromGame)
            Time.timeScale = 0f;

        SceneManager.LoadSceneAsync(instructionsSceneName, LoadSceneMode.Additive).completed += (op) =>
        {
            var closeButton = GameObject.Find("CloseInstructionsButton");
            if (closeButton != null)
            {
                Button button = closeButton.GetComponent<Button>();
                if (button != null)
                    button.onClick.AddListener(CloseInstructions);
            }
        };
    }

    private void EnsureMainMenuInstructionsButton()
    {
        if (GameObject.Find("InstructionsButton") != null)
            return;

        GameObject startButton = GameObject.Find("StartButton");
        if (startButton == null)
            return;

        RectTransform startRect = startButton.GetComponent<RectTransform>();
        Transform parent = startRect != null ? startRect.parent : null;
        if (parent == null)
            return;

        GameObject buttonObject = new GameObject("InstructionsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = startRect.anchorMin;
        rect.anchorMax = startRect.anchorMax;
        rect.sizeDelta = startRect.sizeDelta;
        rect.pivot = startRect.pivot;
        rect.anchoredPosition = startRect.anchoredPosition + new Vector2(0f, -42f);

        Image image = buttonObject.GetComponent<Image>();
        Image startImage = startButton.GetComponent<Image>();
        if (startImage != null)
        {
            image.sprite = startImage.sprite;
            image.type = startImage.type;
            image.color = startImage.color;
        }

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(OpenInstructions);

        GameObject textObject = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI startText = startButton.GetComponentInChildren<TextMeshProUGUI>();
        if (startText != null)
        {
            text.font = startText.font;
            text.fontSharedMaterial = startText.fontSharedMaterial;
            text.color = startText.color;
            text.fontSize = startText.fontSize;
            text.alignment = startText.alignment;
        }
        else
        {
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }

        text.text = "Instructions";
        text.raycastTarget = false;
    }

    public void CloseInstructions()
    {
        if (!SceneManager.GetSceneByName(instructionsSceneName).isLoaded)
        {
            instructionsOpen = false;
            return;
        }

        if (instructionsOpenedFromGame)
        {
            Time.timeScale = Mathf.Approximately(timeScaleBeforeInstructions, 0f) ? 1f : timeScaleBeforeInstructions;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        instructionsOpen = false;
        instructionsOpenedFromGame = false;
        SceneManager.UnloadSceneAsync(instructionsSceneName);
    }
    #endregion
}
