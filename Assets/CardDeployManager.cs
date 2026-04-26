using UnityEngine;

public class CardDeployManager : MonoBehaviour
{
    public static CardDeployManager Instance;

    [Header("部署区域")]
    public Collider2D deployArea;

    private CardData selectedCard;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (selectedCard != null && Input.GetMouseButtonDown(0))
        {
            DeployUnit();
        }
    }

    // 选中卡牌
    public void SelectCard(CardData data)
    {
        selectedCard = data;
    }

    // 部署
    void DeployUnit()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 判断是否在部署区内
        if (deployArea.OverlapPoint(worldPos))
        {
            if (BattleManager.Instance.PlayerCostUse(selectedCard.cost))
            {
                Instantiate(selectedCard.unitPrefab, worldPos, Quaternion.identity);
                selectedCard = null; // 部署完取消选中
            }
        }
    }
}