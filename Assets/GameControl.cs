using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour
{
    public GameObject startUI;
    public GameObject pauseUI;

    // 游戏是否正在运行
    public bool isGameRun = false;


    void Start()
    {
        // 开局直接暂停
        Time.timeScale = 0;
        isGameRun = false;
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
