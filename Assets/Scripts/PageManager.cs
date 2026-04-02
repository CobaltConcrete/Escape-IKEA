
using UnityEngine;
using UnityEngine.SceneManagement;

public class PageManager : MonoBehaviour
{
    public static PageManager Instance = null;
    private GameObject[] gameUIObjects;

    #region Unity_functions
    private void Awake() {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            MainMenu();
        }
        else if(Instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.P) && SceneManager.GetActiveScene().name == "IKEAscene")
        {
            if (SceneManager.GetSceneByName("Pause").isLoaded)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    #endregion

    #region Scene_transitions
    public void StartGame()
    {
        SceneManager.LoadScene("IKEAscene");
    }


    public void PauseGame()
    {
        gameUIObjects = GameObject.FindGameObjectsWithTag("UI");
        foreach (GameObject obj in gameUIObjects)
            obj.SetActive(false);

        SceneManager.LoadSceneAsync("Pause", LoadSceneMode.Additive).completed += (op) =>
        {
            Time.timeScale = 0f;
        };
    }

    public void ResumeGame()
    {
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
        SceneManager.LoadScene("Lose");
    }

    public void WinGame()
    {
        SceneManager.LoadScene("Win");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    #endregion
}
