using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌交互UI脚本
/// 功能：
/// 1. 显示卡牌图标、费用、名称、冷却
/// 2. 通过 Toggle 组件实现选中/取消选中交互
/// 3. 选中遮罩直接使用 Toggle.graphic（预制体中已绑定 SelectMask），代码通过 SetActive 控制
/// 4. 管理冷却计时与 CdMask 遮罩动画
/// </summary>
[RequireComponent(typeof(Toggle))]
public class CardInteraction : MonoBehaviour
{
    [Header("UI绑定")]
    [Tooltip("卡牌图标")]
    public Image icon;

    [Tooltip("费用文本")]
    public Text costText;

    [Tooltip("冷却数字文本")]
    public Text cdText;

    [Tooltip("名称文本")]
    public Text nameText;

    [Tooltip("冷却遮罩（从上往下遮）")]
    public Image cdMask;

    [Tooltip("费用不足遮罩")]
    public Image costMask;

    [Tooltip("卡牌 Toggle 组件")]
    public Toggle toggle;

    // 当前卡牌数据
    private CardData cardData;

    // 剩余冷却时间
    private float currentCd;

    /// <summary>
    /// 是否处于选中状态（直接映射 Toggle.isOn）
    /// </summary>
    public bool IsSelected => toggle.isOn;

    /// <summary>
    /// 选中遮罩快捷引用（即 Toggle.graphic）
    /// </summary>
    private Graphic SelectMask => toggle.graphic;

    /// <summary>
    /// 初始化卡牌数据和UI
    /// </summary>
    public void Init(CardData data)
    {
        cardData = data;
        icon.sprite = data.icon;
        costText.text = data.cost.ToString();
        nameText.text = data.cardName;
        ResetCdUI();

        // 初始状态：未选中、可交互、遮罩隐藏
        toggle.SetIsOnWithoutNotify(false);
        toggle.interactable = true;
        if (SelectMask != null)
            SelectMask.gameObject.SetActive(false);

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    /// <summary>
    /// Toggle 值变化回调
    /// </summary>
    private void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            if (currentCd > 0)
            {
                toggle.SetIsOnWithoutNotify(false);
                return;
            }
            if (SelectMask != null)
                SelectMask.gameObject.SetActive(true);
            CardManager.Instance.OnCardSelected(this);
        }
        else
        {
            if (SelectMask != null)
                SelectMask.gameObject.SetActive(false);
            CardManager.Instance.OnCardDeselected(this);
        }
    }

    /// <summary>
    /// 开始冷却
    /// </summary>
    public void StartCooldown()
    {
        if (cardData == null) return;
        currentCd = cardData.cooldown;
        toggle.isOn = false;            // 取消选中 → 触发 OnToggleChanged → 隐藏遮罩
        toggle.interactable = false;    // 冷却期间禁止点击
    }

    /// <summary>
    /// 程序化强制取消选中（不触发 onValueChanged 事件）
    /// </summary>
    public void ForceDeselect()
    {
        toggle.SetIsOnWithoutNotify(false);
        if (SelectMask != null)
            SelectMask.gameObject.SetActive(false);
    }

    /// <summary>
    /// 重置冷却UI为初始状态
    /// </summary>
    private void ResetCdUI()
    {
        cdMask.fillAmount = 0;
        cdText.text = "";
        currentCd = 0;
    }

    /// <summary>
    /// 每帧更新冷却计时和费用遮罩
    /// </summary>
    private void Update()
    {
        // 冷却计时
        if (currentCd > 0)
        {
            currentCd -= Time.deltaTime;
            cdMask.fillAmount = currentCd / cardData.cooldown;
            cdText.text = currentCd.ToString("F2");

            if (currentCd <= 0)
            {
                cdText.text = "";
                cdMask.fillAmount = 0;
            }
        }

        // 费用不足检测
        UpdateAffordability();
    }

    /// <summary>
    /// 根据当前资源判断是否可支付，控制费用遮罩和可交互状态
    /// </summary>
    private void UpdateAffordability()
    {
        if (cardData == null || BattleManager.Instance == null) return;

        // 冷却期间保持锁定（StartCooldown 已设 interactable=false）
        if (currentCd > 0)
        {
            if (costMask != null) costMask.gameObject.SetActive(false);
            return;
        }

        bool canAfford = BattleManager.Instance.nowCost >= cardData.cost;

        if (costMask != null)
            costMask.gameObject.SetActive(!canAfford);

        if (canAfford)
        {
            toggle.interactable = true;
        }
        else
        {
            toggle.interactable = false;
            // 资源突然不足时强制取消选中
            if (toggle.isOn)
                ForceDeselect();
        }
    }

    public CardData GetCardData() => cardData;
}
