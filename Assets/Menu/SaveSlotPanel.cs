using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档栏位选择面板（独立组件）
/// 支持新游戏（选择空栏位/覆盖已有存档）和加载存档（选择已有存档）两种模式
/// 从每个栏位按钮子节点自动获取 Text 显示存档信息，无需手动赋值标签
/// </summary>
public class SaveSlotPanel : MonoBehaviour
{
    /// <summary>栏位选择模式</summary>
    public enum Mode { NewGame, Load }

    [Header("面板根节点")]
    public GameObject panelRoot;               // 整个面板的根 GameObject
    public Text titleText;                     // 面板标题

    [Header("栏位按钮")]
    public Button[] slotButtons;               // 3 个栏位按钮，各需包含一个子 Text 用于显示标签
    public Button cancelButton;                // 取消/返回按钮

    [Header("覆盖确认子面板")]
    public GameObject overwritePanel;          // 覆盖确认弹窗根节点
    public Text overwriteWarningText;          // 覆盖警告文本
    public Button overwriteConfirmButton;      // 确认覆盖按钮
    public Button overwriteCancelButton;       // 取消覆盖按钮

    /// <summary>用户确认选择栏位后触发，参数为栏位索引</summary>
    public System.Action<int> OnSlotConfirmed;

    /// <summary>用户取消选择时触发</summary>
    public System.Action OnCancelled;

    /// <summary>当前模式</summary>
    public Mode CurrentMode { get; private set; }

    /// <summary>从按钮子节点自动获取的标签文本</summary>
    private Text[] slotLabelTexts;

    /// <summary>待确认覆盖的栏位索引</summary>
    private int pendingSlotIndex;

    private void Start()
    {
        // 从按钮子节点自动获取标签文本
        slotLabelTexts = new Text[SaveManager.SlotCount];
        for (int i = 0; i < SaveManager.SlotCount && i < slotButtons.Length; i++)
        {
            slotLabelTexts[i] = slotButtons[i].GetComponentInChildren<Text>();
            int index = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
        }

        cancelButton.onClick.AddListener(() =>
        {
            Hide();
            OnCancelled?.Invoke();
        });

        overwriteConfirmButton.onClick.AddListener(OnOverwriteConfirm);
        overwriteCancelButton.onClick.AddListener(OnOverwriteCancel);

        Hide();
    }

    /// <summary>
    /// 显示面板并刷新所有栏位信息
    /// </summary>
    /// <param name="mode">选择模式</param>
    public void Show(Mode mode)
    {
        CurrentMode = mode;
        titleText.text = mode == Mode.NewGame
            ? "选择存档栏位 — 开始新游戏"
            : "选择存档 — 加载游戏";

        RefreshSlots();
        panelRoot.SetActive(true);
    }

    /// <summary>
    /// 隐藏面板及子面板
    /// </summary>
    public void Hide()
    {
        panelRoot.SetActive(false);
        overwritePanel.SetActive(false);
    }

    /// <summary>
    /// 刷新栏位标签，显示存档信息或空位提示
    /// </summary>
    private void RefreshSlots()
    {
        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            var save = SaveManager.LoadSave(i);
            if (save != null)
            {
                slotLabelTexts[i].text = $"栏位 {i + 1} — {save.currentLevelScene}\n{save.saveTime}";
                slotButtons[i].interactable = true;
            }
            else
            {
                slotLabelTexts[i].text = $"栏位 {i + 1}\n— 空 —";
                slotButtons[i].interactable = (CurrentMode == Mode.NewGame);
            }
        }
    }

    private void OnSlotClicked(int index)
    {
        if (CurrentMode == Mode.NewGame)
        {
            if (SaveManager.HasSave(index))
            {
                // 已占用：弹出覆盖确认
                var save = SaveManager.LoadSave(index);
                pendingSlotIndex = index;
                overwriteWarningText.text =
                    $"栏位 {index + 1} 已有存档（{save.currentLevelScene}）\n覆盖后将无法恢复，确定继续？";
                overwritePanel.SetActive(true);
            }
            else
            {
                // 空栏位：直接确认
                OnSlotConfirmed?.Invoke(index);
            }
        }
        else // Load 模式
        {
            if (SaveManager.HasSave(index))
                OnSlotConfirmed?.Invoke(index);
        }
    }

    private void OnOverwriteConfirm()
    {
        overwritePanel.SetActive(false);
        OnSlotConfirmed?.Invoke(pendingSlotIndex);
    }

    private void OnOverwriteCancel()
    {
        overwritePanel.SetActive(false);
    }
}
