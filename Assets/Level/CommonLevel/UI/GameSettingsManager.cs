using UnityEngine;
using UnityEngine.UI;

public class GameSettingsManager : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject settingsPanel; // 设置菜单面板
    public Slider volumeSlider;     // 音量滑块
    public Toggle fullscreenToggle; // 全屏开关
    public Button openSettingsBtn; // 打开设置的按钮
    public Button closeSettingsBtn; // 关闭设置的按钮
    public Button quitBtn;          // 退出游戏按钮

    private void Start()
    {
        // 初始化UI和设置
        volumeSlider.value = AudioListener.volume;
        fullscreenToggle.isOn = Screen.fullScreen;

        // 绑定按钮事件
        openSettingsBtn.onClick.AddListener(OpenSettings);
        closeSettingsBtn.onClick.AddListener(CloseSettings);
        quitBtn.onClick.AddListener(QuitGame);

        // 绑定滑块和开关事件
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 初始隐藏设置面板
        settingsPanel.SetActive(false);
    }

    // 打开设置菜单
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // 关闭设置菜单
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    // 设置全屏/窗口
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    // 退出游戏（加了个日志提示）
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
