using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    [Header("绑定的数据")]
    public CardData cardData;

    [Header("UI引用")]
    public Image icon;
    public Text costText;
    public Button cardButton;

    void Start()
    {
        RefreshUI();
        cardButton.onClick.AddListener(OnClickCard);
    }

    // 刷新UI显示
    void RefreshUI()
    {
        icon.sprite = cardData.icon;
        costText.text = cardData.cost.ToString();
    }

    // 点击卡牌
    void OnClickCard()
    {
        // 检查费用够不够
        if (BattleManager.Instance.PlayerCostUse(cardData.cost))
        {
            // 部署单位
            CardDeployManager.Instance.SelectCard(cardData);
        }
        else
        {
            Debug.Log("费用不足");
        }
    }

    
}