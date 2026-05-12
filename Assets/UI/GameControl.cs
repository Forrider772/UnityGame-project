using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{
    
    public GameObject pauseUI;

    public Button reStartButton;
    public Button pauseButton;
    public Button resumeButton;
    // 游戏是否正在运行
    public bool isGameRun = true;


    void Start()
    {
        Time.timeScale = 1;
        isGameRun = true;
        pauseUI.SetActive(false);
        reStartButton.onClick.AddListener(GameReStart);
        pauseButton.onClick.AddListener(GamePause);
        resumeButton.onClick.AddListener(GameResume);
    }

    // ========== 按钮绑定的方法 ==========
    public void GameReStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GamePause()
    {
        Time.timeScale = 0;
        pauseUI.SetActive(true);
    }

    public void GameResume()
    {
        Time.timeScale = 1;
        pauseUI.SetActive(false);
    }
}
