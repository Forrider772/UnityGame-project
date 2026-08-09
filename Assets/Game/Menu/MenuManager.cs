using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单管理器
/// 负责新游戏、继续游戏、加载存档、退出游戏的顶层逻辑
/// 存档栏位选择 UI 由独立的 SaveSlotPanel 组件管理
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("主菜单按钮")]
    public Button newGameButton;       // 新游戏按钮
    public Button continueButton;      // 继续游戏按钮
    public Button loadGameButton;      // 加载存档按钮
    public Button quitButton;          // 退出游戏按钮

    [Header("存档栏位选择面板")]
    public SaveSlotPanel saveSlotPanel;  // 独立的栏位选择面板组件

    /// <summary>防止重复加载场景的标记</summary>
    private bool isLoading;

    private void Start()
    {
        // 播放主菜单 BGM
        if (AudioManager.Instance != null && AudioManager.Instance.config != null)
            AudioManager.Instance.PlayBGM(AudioManager.Instance.config.bgmMenu);

        // 绑定主菜单按钮
        newGameButton.onClick.AddListener(OnNewGameClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        loadGameButton.onClick.AddListener(OnLoadClicked);
        quitButton.onClick.AddListener(QuitGame);

        // 按钮点击音效
        AudioManager.BindClick(newGameButton);
        AudioManager.BindClick(continueButton);
        AudioManager.BindClick(loadGameButton);
        AudioManager.BindClick(quitButton);

        // 绑定栏位选择面板回调
        saveSlotPanel.OnSlotConfirmed += OnSlotConfirmed;
        saveSlotPanel.OnCancelled += OnSlotSelectCancelled;

        UpdateButtonStates();
    }

    /// <summary>
    /// 根据是否存在存档控制继续/加载按钮的可交互状态
    /// </summary>
    private void UpdateButtonStates()
    {
        bool hasSave = SaveManager.HasAnySave();
        continueButton.interactable = hasSave;
        loadGameButton.interactable = hasSave;
    }

    #region 主菜单按钮回调

    /// <summary>
    /// 新游戏：打开栏位选择面板（NewGame 模式）
    /// </summary>
    private void OnNewGameClicked()
    {
        saveSlotPanel.Show(SaveSlotPanel.Mode.NewGame);
    }

    /// <summary>
    /// 继续游戏：自动找最新存档并加载
    /// </summary>
    private void OnContinueClicked()
    {
        if (isLoading) return;
        int latestSlot = SaveManager.GetLatestSaveSlot();
        if (latestSlot < 0)
        {
            UpdateButtonStates();
            return;
        }

        var save = SaveManager.LoadSave(latestSlot);
        LoadScene(save.currentLevelScene, latestSlot);
    }

    /// <summary>
    /// 加载存档：打开栏位选择面板（Load 模式）
    /// </summary>
    private void OnLoadClicked()
    {
        saveSlotPanel.Show(SaveSlotPanel.Mode.Load);
    }

    #endregion

    #region 栏位面板回调

    /// <summary>
    /// 用户选择了某个栏位
    /// </summary>
    /// <param name="slotIndex">栏位索引</param>
    private void OnSlotConfirmed(int slotIndex)
    {
        if (saveSlotPanel.CurrentMode == SaveSlotPanel.Mode.NewGame)
        {
            // 在指定栏位开始新游戏
            StartNewGame(slotIndex);
        }
        else
        {
            // 加载指定栏位的存档
            var save = SaveManager.LoadSave(slotIndex);
            LoadScene(save.currentLevelScene, slotIndex);
        }
    }

    /// <summary>
    /// 用户取消了栏位选择
    /// </summary>
    private void OnSlotSelectCancelled()
    {
        UpdateButtonStates();
    }

    #endregion

    #region 核心流程

    /// <summary>
    /// 在指定栏位开始新游戏：删除旧档 → 创建新档 → 保存 → 先播第一章剧情再进入第一关
    /// </summary>
    private void StartNewGame(int slotIndex)
    {
        if (isLoading) return;
        saveSlotPanel.Hide();
        SaveManager.DeleteSave(slotIndex);
        var data = SaveManager.CreateNewGame();
        SaveManager.SaveGame(data, slotIndex);
        // 先播第一章剧情（StoryScene_Ch1 播完自动进入 Level_1）
        // LoadScene 内部会设置 isLoading 并执行场景切换，这里不能提前置位
        LoadScene("StoryScene_Ch1", slotIndex);
    }

    /// <summary>
    /// 加载场景并记录当前存档栏位
    /// </summary>
    private void LoadScene(string sceneName, int slotIndex)
    {
        if (isLoading) return;
        isLoading = true;
        SaveManager.CurrentSlotIndex = slotIndex;
        SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region 退出

    /// <summary>
    /// 退出游戏，编辑器模式下停止播放，打包后关闭程序
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
