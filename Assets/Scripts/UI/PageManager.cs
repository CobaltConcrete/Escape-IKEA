using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance = null;
    private GameObject[] gameUIObjects;

    private bool blackOut = false;
    private float blackOutTimer = 0;
    [SerializeField] public GameObject blackOutScreen;
    [SerializeField] public GameObject blackOutText;
    private Image image;
    private float increment = -0.005f;

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
            image = blackOutScreen.GetComponent<Image>();
            image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
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

    private void Start() { }

    private void Update() {
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
            blackOutScreen.SetActive(true);
            blackOutText.SetActive(true);
            if (image.color.a < 0.75 || image.color.a > 1){
                increment *= -1;
            }
            image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a+increment);
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
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
    #endregion
}
