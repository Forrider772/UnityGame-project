using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Battle/Card Data")]
public class CardData : ScriptableObject
{
    [Header("部署配置")]
    public CampType camp;       // 卡牌所属阵营

    [Header("卡牌信息")]
    public string cardID;        // 卡牌唯一ID
    public string cardName;       // 卡牌名字
    public int cost;              // 费用
    public float cooldown;       // 冷却时间
    public Sprite icon;          // 卡牌图片

    [Header("单位设置")]
    public GameObject unitPrefab; // 要召唤的单位预制体
}