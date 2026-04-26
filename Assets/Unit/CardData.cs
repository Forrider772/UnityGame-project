using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Battle/Card Data")]
public class CardData : ScriptableObject
{
    [Header("卡牌信息")]
    public string cardName;       // 卡牌名字
    public int cost;              // 费用
    public Sprite icon;          // 卡牌图片

    [Header("单位设置")]
    public GameObject unitPrefab; // 要召唤的单位预制体
}