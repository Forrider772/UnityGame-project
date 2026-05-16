using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌交互UI脚本
/// 功能：
/// 1. 显示卡牌图标、费用、冷却
/// 2. 响应点击、选中、取消选中
/// 3. 管理冷却计时与遮罩动画
/// </summary>
[RequireComponent(typeof(Button))]
public class CardInteraction : MonoBehaviour
{
    [Header("UI绑定")]
    [Tooltip("卡牌图标")]
    public Image icon;

    [Tooltip("费用文本")]
    public Text costText;

    [Tooltip("冷却数字文本")]
    public Text cdText;

    [Tooltip("冷却遮罩（从上往下遮）")]
    public Image cdMask;

    [Tooltip("卡牌按钮")]
    public Button button;

    // 当前卡牌数据
    private CardData cardData;

    // 剩余冷却时间
    private float currentCd;

    /// <summary>
    /// 是否处于选中状态
    /// </summary>
    public bool IsSelected;

    /// <summary>
    /// 初始化卡牌数据和UI
    /// </summary>
    /// <param name="data">卡牌配置数据</param>
    public void Init(CardData data)
    {
        cardData = data;
        icon.sprite = data.icon;
        costText.text = data.cost.ToString();
        ResetCdUI();
        IsSelected = false;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    /// <summary>
    /// 卡牌点击事件
    /// </summary>
    private void OnClick()
    {
        // 冷却中不可点击
        if (currentCd > 0) return;

        // 通知管理器处理选中
        CardManager.Instance.OnCardClicked(this);
    }

    /// <summary>
    /// 开始冷却
    /// </summary>
    public void StartCooldown()
    {
        if (cardData == null) return;
        currentCd = cardData.cooldown;
        Deselect();
    }

    /// <summary>
    /// 设置为选中状态
    /// </summary>
    public void Select()
    {
        IsSelected = true;
        icon.color = new Color(0.8f, 0.8f, 0.8f, 1);
    }

    /// <summary>
    /// 取消选中状态
    /// </summary>
    public void Deselect()
    {
        IsSelected = false;
        icon.color = Color.white;
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
    /// 每帧更新冷却
    /// </summary>
    private void Update()
    {
        if (currentCd <= 0) return;

        // 递减冷却
        currentCd -= Time.deltaTime;

        // 更新遮罩比例
        cdMask.fillAmount = currentCd / cardData.cooldown;

        // 更新数字
        cdText.text = Mathf.Ceil(currentCd).ToString();

        // 冷却结束清空
        if (currentCd <= 0)
        {
            cdText.text = "";
            cdMask.fillAmount = 0;
        }
    }

    /// <summary>
    /// 获取卡牌数据
    /// </summary>
    public CardData GetCardData() => cardData;
}