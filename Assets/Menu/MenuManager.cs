using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void LoadTestLevel()
    {
        SceneManager.LoadScene("TestLevelScene");
    }
    // 退出游戏
    public void QuitGame()
    {
        Application.Quit();
    }
}