using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌管理器（单例）
/// 功能：
/// 1. 根据玩家卡组生成所有卡牌UI
/// 2. 通过 ToggleGroup 管理卡牌互斥选中/取消选中
/// </summary>
public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    [Header("卡牌UI设置")]
    [Tooltip("卡牌父物体（布局容器）")]
    public Transform cardParent;

    [Tooltip("卡牌预制体")]
    public GameObject cardPrefab;

    [Tooltip("玩家卡组数据")]
    public PlayerDeck playerDeck;

    // 所有已生成的卡牌UI
    private List<CardInteraction> cardList = new List<CardInteraction>();

    /// <summary>
    /// 当前选中的卡牌
    /// </summary>
    public CardInteraction CurrentSelected;

    // ToggleGroup 实现互斥单选
    private ToggleGroup toggleGroup;

    /// <summary>
    /// 初始化单例和 ToggleGroup
    /// </summary>
    void Awake()
    {
        Instance = this;

        // 确保 cardParent 上有 ToggleGroup
        toggleGroup = cardParent.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
            toggleGroup = cardParent.gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // 允许点击已选中卡牌来取消选中
    }

    /// <summary>
    /// 开局生成卡牌UI
    /// </summary>
    void Start() => RefreshAllCards();

    /// <summary>
    /// 刷新所有卡牌UI
    /// </summary>
    public void RefreshAllCards()
    {
        ClearAll();
        foreach (var data in playerDeck.carryCards)
            SpawnCard(data);
    }

    /// <summary>
    /// 生成单张卡牌UI
    /// </summary>
    private void SpawnCard(CardData data)
    {
        var go = Instantiate(cardPrefab, cardParent);
        var card = go.GetComponent<CardInteraction>();
        // 将 Toggle 注册到 ToggleGroup 实现互斥单选
        go.GetComponent<Toggle>().group = toggleGroup;
        card.Init(data);
        cardList.Add(card);
    }

    /// <summary>
    /// 清空所有卡牌UI
    /// </summary>
    private void ClearAll()
    {
        foreach (var card in cardList) Destroy(card.gameObject);
        cardList.Clear();
        CurrentSelected = null;
    }

    /// <summary>
    /// 卡牌被选中回调（由 CardInteraction.OnToggleChanged 调用）
    /// </summary>
    public void OnCardSelected(CardInteraction card)
    {
        CurrentSelected = card;
    }

    /// <summary>
    /// 卡牌取消选中回调（由 CardInteraction.OnToggleChanged 调用）
    /// </summary>
    public void OnCardDeselected(CardInteraction card)
    {
        if (CurrentSelected == card)
            CurrentSelected = null;
    }

    /// <summary>
    /// 取消选中当前卡牌（由 CardDeploy 调用）
    /// </summary>
    public void DeselectCurrentCard()
    {
        if (CurrentSelected != null)
        {
            CurrentSelected.ForceDeselect();
            CurrentSelected = null;
        }
    }
}
