using UnityEngine;
using UnityEngine.UI;

public class GameControl : MonoBehaviour
{
    public GameObject startUI;
    public GameObject pauseUI;

    public Button startButton;
    public Button pauseButton;
    public Button resumeButton;
    // 游戏是否正在运行
    public bool isGameRun = false;


    void Start()
    {
        // 开局直接暂停
        Time.timeScale = 0;
        isGameRun = false;
        pauseUI.SetActive(false);
        startButton.onClick.AddListener(GameStart);
        pauseButton.onClick.AddListener(GamePause);
        resumeButton.onClick.AddListener(GameResume);
    }

    // ========== 按钮绑定的方法 ==========
    public void GameStart()
    {
        Time.timeScale = 1;
        isGameRun = true;

        startUI.SetActive(false);
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
