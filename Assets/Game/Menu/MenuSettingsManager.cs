using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单设置管理器
/// 负责设置面板的开关、音量/全屏调整，设置通过 PlayerPrefs 持久化
/// </summary>
public class MenuSettingsManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject settingsPanel;      // 设置菜单面板

    [Header("按钮")]
    public Button openSettingsBtn;        // 打开设置按钮
    public Button closeSettingsBtn;       // 关闭设置按钮

    [Header("设置控件")]
    public Slider volumeSlider;           // 音量滑块
    public Toggle fullscreenToggle;       // 全屏开关

    private void Start()
    {
        // 从 PlayerPrefs 读取已保存的设置，无则使用系统默认值
        float savedVolume = PlayerPrefs.GetFloat("Volume", AudioListener.volume);
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        // 应用已保存的设置
        volumeSlider.value = savedVolume;
        fullscreenToggle.isOn = savedFullscreen;
        AudioListener.volume = savedVolume;
        Screen.fullScreen = savedFullscreen;

        // 绑定按钮点击事件
        openSettingsBtn.onClick.AddListener(OpenSettings);
        closeSettingsBtn.onClick.AddListener(CloseSettings);

        // 绑定滑块和开关值变化事件
        volumeSlider.onValueChanged.AddListener(SetVolume);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 初始隐藏设置面板
        settingsPanel.SetActive(false);
    }

    /// <summary>
    /// 打开设置面板，覆盖在主菜单之上
    /// </summary>
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// 关闭设置面板
    /// </summary>
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    /// <summary>
    /// 设置音量并持久化到 PlayerPrefs
    /// </summary>
    /// <param name="volume">音量值（0~1）</param>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 设置全屏/窗口模式并持久化到 PlayerPrefs
    /// </summary>
    /// <param name="isFullscreen">是否全屏</param>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}
