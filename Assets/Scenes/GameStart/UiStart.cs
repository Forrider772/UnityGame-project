using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiStart : MonoBehaviour
{
    public void Start()
    {
        Debug.Log("游戏已跳转");
        SceneManager.LoadScene("Test");
    }
    public void Exit()
    {
        Debug.Log("游戏已退出");
        Application.Quit();
    }
}
