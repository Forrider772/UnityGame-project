using UnityEngine;

// 卡牌部署管理器：负责卡牌选中、点击部署、区域判定、生成单位
public class CardDeployManager : MonoBehaviour
{
    // 单例实例，全局唯一，方便其他脚本调用
    public static CardDeployManager Instance;

    [Header("部署区域")]
    public Collider2D deployArea;  // 玩家可部署单位的合法区域碰撞器

    private CardData selectedCard; // 当前选中的卡牌数据（待部署）

    // 脚本初始化时设置单例
    void Awake()
    {
        Instance = this;
    }

    // 每帧检测：如果已选中卡牌，且点击鼠标左键，则尝试部署
    void Update()
    {
        if (selectedCard != null && Input.GetMouseButtonDown(0))
        {
            DeployUnit();
        }
    }

    // 选中卡牌：外部调用，设置当前要部署的卡牌
    public void SelectCard(CardData data)
    {
        selectedCard = data;
    }

    // 部署单位：判定区域、费用，生成单位
    void DeployUnit()
    {
        // 将鼠标屏幕坐标转换为世界坐标
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 判断点击位置是否在合法部署区域内
        if (deployArea.OverlapPoint(worldPos))
        {
            // 检查费用是否足够
            if (BattleManager.Instance.PlayerCostUse(selectedCard.cost))
            {
                // 在点击位置生成卡牌对应的单位
                Instantiate(selectedCard.unitPrefab, worldPos, Quaternion.identity);
                // 部署完成，清空选中卡牌
                selectedCard = null;
            }
            else
            {
                // 费用不足，控制台打印提示
                Debug.Log("费用不足");
            }
        }
    }
}