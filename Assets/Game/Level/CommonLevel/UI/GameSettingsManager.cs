using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 关卡内设置管理器
/// 负责设置面板的开关、音量/全屏调整、返回主菜单、退出游戏
/// 打开设置时自动暂停游戏，关闭时恢复
/// </summary>
public class GameSettingsManager : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject settingsPanel;   // 设置菜单面板
    public Slider volumeSlider;        // 音量滑块
    public Toggle fullscreenToggle;    // 全屏开关
    public Button openSettingsBtn;     // 打开设置的按钮
    public Button closeSettingsBtn;    // 关闭设置的按钮
    public Button quitBtn;             // 退出游戏按钮
    public Button returnToMenuBtn;     // 返回主菜单按钮

    /// <summary>记录打开设置前的游戏速度，用于关闭设置时恢复</summary>
    private float previousTimeScale;

    private void Start()
    {
        // 初始化UI状态，与当前系统设置同步
        volumeSlider.value = AudioListener.volume;
        fullscreenToggle.isOn = Screen.fullScreen;

        // 绑定按钮点击事件
        openSettingsBtn.onClick.AddListener(OpenSettings);
        closeSettingsBtn.onClick.AddListener(CloseSettings);
        quitBtn.onClick.AddListener(QuitGame);
        returnToMenuBtn.onClick.AddListener(ReturnToMenu);

        // 绑定滑块和开关值变化事件
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 初始隐藏设置面板
        settingsPanel.SetActive(false);
    }

    /// <summary>
    /// 打开设置菜单，同时暂停游戏
    /// </summary>
    public void OpenSettings()
    {
        // 记录当前游戏速度后暂停，避免覆盖玩家手动暂停的状态
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// 关闭设置菜单，恢复之前的游戏速度
    /// </summary>
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        Time.timeScale = previousTimeScale;
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="volume">音量值（0~1）</param>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    /// <summary>
    /// 设置全屏/窗口模式
    /// </summary>
    /// <param name="isFullscreen">是否全屏</param>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    /// <summary>
    /// 返回主菜单，恢复时间流速后加载 MenuScene
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MenuScene");
    }

    /// <summary>
    /// 退出游戏
    /// 编辑器模式下停止播放，打包后关闭程序
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        // 在编辑器里运行时，退出游戏会停在编辑模式
        Debug.Log("退出游戏（编辑器模式）");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 打包后运行时，直接关闭程序
        Application.Quit();
#endif
    }
}
