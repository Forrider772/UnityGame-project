using UnityEngine;

/// <summary>
/// 卡牌部署系统（单例）
/// 流程：
/// 1. 选中卡牌
/// 2. 右键选择路线
/// 3. 左键在路线起点部署单位
/// </summary>
public class CardDeploy : MonoBehaviour
{
    public static CardDeploy Instance;

    [Header("路线设置")]
    [Tooltip("所有可部署路线数组")]
    public DeployRoute[] routes;

    [Tooltip("路线点击判定范围")]
    public float selectRange = 0.6f;

    // 当前待部署卡牌
    private CardData selectedCard;

    // 当前选中路线
    private DeployRoute selectedRoute;

    /// <summary>
    /// 初始化单例
    /// </summary>
    void Awake() => Instance = this;

    /// <summary>
    /// 每帧检测输入：右键选路线、左键部署
    /// </summary>
    void Update()
    {
        if (selectedCard == null) return;

        // 右键选择路线
        if (Input.GetMouseButtonDown(1))
            SelectRouteByMouse();

        // 左键确认部署
        if (Input.GetMouseButtonDown(0) && selectedRoute != null)
            TryDeploy();
    }

    /// <summary>
    /// 设置当前待部署卡牌
    /// </summary>
    public void SelectCard(CardData card)
    {
        selectedCard = card;
        selectedRoute = null;
    }

    /// <summary>
    /// 根据鼠标位置选择最近路线
    /// </summary>
    private void SelectRouteByMouse()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        DeployRoute closest = null;
        float minDist = Mathf.Infinity;

        foreach (var route in routes)
        {
            float dist = Vector2.Distance(worldPos, route.startPos);
            if (dist < selectRange && dist < minDist)
            {
                minDist = dist;
                closest = route;
            }
        }

        selectedRoute = closest;
    }

    /// <summary>
    /// 尝试部署：费用检查 → 生成单位 → 冷却
    /// </summary>
    private void TryDeploy()
    {
        if (selectedCard == null || selectedRoute == null) return;

        // 费用不足
        if (!BattleManager.Instance.UseCost(selectedCard.cost))
        {
            Debug.Log("费用不足，无法部署");
            return;
        }

        // 在路线起点生成单位
        Instantiate(selectedCard.unitPrefab, selectedRoute.startPos, Quaternion.identity);

        // 卡牌进入冷却
        CardManager.Instance.CurrentSelected.StartCooldown();

        // 清空状态
        selectedCard = null;
        selectedRoute = null;
    }
}

/// <summary>
/// 部署路线数据
/// </summary>
[System.Serializable]
public class DeployRoute
{
    [Tooltip("路线名称（调试用）")]
    public string routeName;

    [Tooltip("路线起点（部署点）")]
    public Vector2 startPos;
}