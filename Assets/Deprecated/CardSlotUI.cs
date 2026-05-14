using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌槽UI脚本
/// 负责单张卡牌的图标、费用显示、点击选中卡牌逻辑
/// </summary>
public class CardSlotUI : MonoBehaviour
{
    [Header("绑定的数据")]
    public CardData cardData;       // 当前卡牌对应的配置数据

    [Header("UI引用")]
    public Image icon;              // 卡牌图标组件
    public Text costText;           // 卡牌费用文字组件
    public Button cardButton;       // 卡牌点击按钮组件

    /// <summary>
    /// 初始化卡牌UI，绑定点击事件
    /// </summary>
    void Start()
    {
        // 刷新卡牌图标与费用文字
        RefreshUI();
        // 绑定卡牌点击监听事件
        cardButton.onClick.AddListener(OnClickCard);
    }

    /// <summary>
    /// 刷新卡牌UI显示内容
    /// 根据CardData配置更新图标和费用文本
    /// </summary>
    void RefreshUI()
    {
        icon.sprite = cardData.icon;
        costText.text = cardData.cost.ToString();
    }

    /// <summary>
    /// 卡牌点击触发事件
    /// 校验费用，费用充足则选中当前卡牌，等待地面部署
    /// </summary>
    void OnClickCard()
    {
        // 选中当前卡牌，进入地面部署状态
         CardDeploy.Instance.SelectCard(cardData);
    }
}