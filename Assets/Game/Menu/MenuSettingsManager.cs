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
    public Slider volumeSlider;           // BGM 音量滑块
    public Slider sfxVolumeSlider;        // SFX 音效音量滑块
    public Toggle fullscreenToggle;       // 全屏开关

    private void Start()
    {
        // 从 AudioManager 读取已保存的双通道音量（无实例时回退 PlayerPrefs）
        volumeSlider.value = AudioManager.Instance?.GetBGMVolume() ?? AudioManager.GetPersistedBGMVolume();
        sfxVolumeSlider.value = AudioManager.Instance?.GetSFXVolume() ?? AudioManager.GetPersistedSFXVolume();
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        // 应用已保存的设置
        fullscreenToggle.isOn = savedFullscreen;
        Screen.fullScreen = savedFullscreen;

        // 绑定按钮点击事件
        openSettingsBtn.onClick.AddListener(OpenSettings);
        closeSettingsBtn.onClick.AddListener(CloseSettings);
        AudioManager.BindClick(openSettingsBtn);
        AudioManager.BindClick(closeSettingsBtn);

        // 绑定滑块和开关值变化事件
        volumeSlider.onValueChanged.AddListener(SetVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
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
    /// 设置 BGM 音量（持久化由 AudioManager 处理）
    /// </summary>
    /// <param name="volume">音量值（0~1）</param>
    public void SetVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(volume);
    }

    /// <summary>
    /// 设置 SFX 音效音量（持久化由 AudioManager 处理）
    /// </summary>
    /// <param name="volume">音量值（0~1）</param>
    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
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
