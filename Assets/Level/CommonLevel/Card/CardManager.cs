using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌管理器（单例）
/// 功能：
/// 1. 根据玩家卡组生成所有卡牌UI
/// 2. 管理卡牌选中、取消选中
/// 3. 与CardDeploy交互，传递选中卡牌
/// </summary>
public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    [Header("卡牌UI设置")]
    [Tooltip("卡牌父物体（布局容器）")]
    public Transform cardParent;

    [Tooltip("卡牌预制体（挂CardInteraction）")]
    public GameObject cardPrefab;

    [Tooltip("玩家卡组数据")]
    public PlayerDeck playerDeck;

    // 所有已生成的卡牌UI
    private List<CardInteraction> cardList = new List<CardInteraction>();

    /// <summary>
    /// 当前选中的卡牌
    /// </summary>
    public CardInteraction CurrentSelected { get; private set; }

    /// <summary>
    /// 初始化单例
    /// </summary>
    void Awake() => Instance = this;

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
    /// 卡牌点击回调
    /// </summary>
    /// <param name="clickedCard">被点击的卡牌</param>
    public void OnCardClicked(CardInteraction clickedCard)
    {
        // 取消旧选中
        if (CurrentSelected != null && CurrentSelected != clickedCard)
            CurrentSelected.Deselect();

        // 设置新选中
        CurrentSelected = clickedCard;
        CurrentSelected.Select();

        // 通知部署系统选中卡牌
        CardDeploy.Instance.SelectCard(clickedCard.GetCardData());
    }
}