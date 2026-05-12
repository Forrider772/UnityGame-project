using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("配置")]
    public Transform cardParent;       // 水平布局父物体
    public GameObject cardPrefab;      // 卡牌预制体
    public PlayerDeck playerDeck;      // 玩家携带的卡组

    private List<CardItemUI> spawnedCards = new List<CardItemUI>();

    // 记录当前选中的一张卡牌UI
    public CardItemUI curSelectedCard;
    void Start()
    {
        // 游戏启动 → 根据携带卡组动态生成卡牌
        GenerateCardsFromDeck();
    }

    #region 核心：动态生成卡牌UI
    public void GenerateCardsFromDeck()
    {
        ClearAllCards(); // 先清空

        foreach (var card in playerDeck.carryCards)
        {
            SpawnCard(card);
        }
    }

    void SpawnCard(CardData data)
    {
        var obj = Instantiate(cardPrefab, cardParent);
        var cardUI = obj.GetComponent<CardItemUI>();
        cardUI.Init(data);
        spawnedCards.Add(cardUI);
    }

    void ClearAllCards()
    {
        foreach (var card in spawnedCards)
            Destroy(card.gameObject);

        spawnedCards.Clear();
    }
    #endregion

    #region 【预留】编辑携带卡组功能（未来直接用）
    public void AddCardToDeck(CardData newCard)
    {
        if (!playerDeck.carryCards.Contains(newCard))
        {
            playerDeck.carryCards.Add(newCard);
            RefreshUI();
        }
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (playerDeck.carryCards.Contains(card))
        {
            playerDeck.carryCards.Remove(card);
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        GenerateCardsFromDeck();
    }
    #endregion
}