using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 卡牌部署系统
/// 流程：
/// 1. 玩家选中卡牌 -> 自动进入部署模式，显示所有同阵营路线
/// 2. 鼠标悬停到路线任意线段上 -> 该路线高亮
/// 3. 左键点击高亮路线 -> 在路线起点生成单位，扣费，触发卡牌冷却，退出部署模式
/// 4. 右键点击 -> 退出部署模式并取消卡牌选中
/// </summary>
public class CardDeploy : MonoBehaviour
{
    [Header("部署设置")]
    [Tooltip("鼠标悬停路线的检测阈值（世界单位），小于此距离视为悬停")]
    public float hoverThreshold = 0.5f;

    private CardData selectedCard;                  // 当前选中的卡牌数据
    private bool isDeploying = false;              // 是否处于部署模式
    private PathManager currentHighlightedPath;    // 当前高亮的路线
    private List<PathManager> availablePaths = new List<PathManager>(); // 当前阵营的所有可用路线缓存
    private Camera mainCamera;                     // 主摄像机引用

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 如果没有选中的卡牌，则强制退出部署模式（如有）
        if (CardManager.Instance.CurrentSelected == null)
        {
            if (isDeploying)
                ExitDeployMode();
            return;
        }

        // 获取当前选中卡牌的数据
        CardData card = CardManager.Instance.CurrentSelected.GetCardData();
        if (card == null) return;

        // 如果尚未进入部署模式，或者选中的卡牌发生了改变（如切换卡牌），则重新进入部署模式
        if (!isDeploying || selectedCard != card)
        {
            selectedCard = card;
            EnterDeployMode();
        }

        if (!isDeploying) return;

        // 1. 鼠标悬停检测：获取鼠标下方的路线
        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        PathManager hitPath = GetPathUnderMouse(mouseWorldPos);

        // 如果悬停的路线发生变化，更新高亮状态
        if (hitPath != currentHighlightedPath)
        {
            if (currentHighlightedPath != null)
                currentHighlightedPath.SetHighlight(false); // 取消旧高亮
            currentHighlightedPath = hitPath;
            if (currentHighlightedPath != null)
                currentHighlightedPath.SetHighlight(true);   // 高亮新路线
        }

        // 2. 左键点击：如果当前有高亮路线，则部署单位
        if (Input.GetMouseButtonDown(0) && currentHighlightedPath != null)
        {
            DeployUnit(currentHighlightedPath);
        }

        // 3. 右键点击：退出部署模式并取消卡牌选中（提供取消操作）
        if (Input.GetMouseButtonDown(1))
        {
            ExitDeployMode();
            CardManager.Instance.DeselectCurrentCard();
        }
    }

    /// <summary>
    /// 进入部署模式
    /// - 根据卡牌阵营获取所有可用路线
    /// - 显示所有路线的视觉表现
    /// - 重置高亮状态
    /// </summary>
    private void EnterDeployMode()
    {
        if (isDeploying) return;

        // 从场景管理器中获取当前卡牌阵营的所有路线
        availablePaths = LevelPathManager.Instance.GetAllPathsByCamp(selectedCard.camp);
        if (availablePaths.Count == 0)
        {
            Debug.LogWarning($"阵营 [{selectedCard.camp}] 没有可用路线，无法部署");
            return;
        }

        // 显示所有路线
        foreach (var path in availablePaths)
            path.SetVisible(true);

        isDeploying = true;
        currentHighlightedPath = null;
        Debug.Log("进入部署模式，鼠标悬停路线可高亮，左键部署单位");
    }

    /// <summary>
    /// 退出部署模式
    /// - 隐藏所有路线视觉
    /// - 清除高亮
    /// - 清空内部状态
    /// </summary>
    private void ExitDeployMode()
    {
        if (!isDeploying) return;

        // 隐藏并取消高亮所有路线
        foreach (var path in availablePaths)
        {
            path.SetVisible(false);
            path.SetHighlight(false);
        }

        isDeploying = false;
        currentHighlightedPath = null;
        selectedCard = null;
        Debug.Log("退出部署模式");
    }

    /// <summary>
    /// 获取鼠标下方最近的路线（距离小于阈值）
    /// 遍历所有可用路线，计算鼠标到每条路线折线的最短距离
    /// </summary>
    /// <param name="mousePos">鼠标世界坐标</param>
    /// <returns>最近的路线，若所有路线距离均大于阈值则返回 null</returns>
    private PathManager GetPathUnderMouse(Vector2 mousePos)
    {
        PathManager closest = null;
        float minDist = hoverThreshold;

        foreach (var path in availablePaths)
        {
            if (path == null) continue;
            Vector2[] waypoints = path.GetWaypoints2D();
            if (waypoints.Length < 2) continue;

            float dist = Math2DHelper.MinDistancePointToPolyline(mousePos, waypoints);
            if (dist < minDist)
            {
                minDist = dist;
                closest = path;
            }
        }
        return closest;
    }

    /// <summary>
    /// 在指定路线的起点部署单位
    /// 流程：扣费 → 实例化单位 → 设置路径 → 触发卡牌冷却 → 退出部署模式并清空选中状态
    /// </summary>
    /// <param name="targetPath">目标路线</param>
    private void DeployUnit(PathManager targetPath)
    {
        if (selectedCard == null || targetPath == null) return;

        // 1. 扣费，若费用不足则提示并返回（不退出部署模式）
        if (!BattleManager.Instance.UseCost(selectedCard.cost))
        {
            Debug.Log("费用不足，无法部署");
            return;
        }

        // 2. 在路线起点生成单位
        Vector3 spawnPos = targetPath.GetStartPoint();
        GameObject unit = Instantiate(selectedCard.unitPrefab, spawnPos, Quaternion.identity);

        // 3. 为单位的移动组件设置路径
        UnitMovement move = unit.GetComponent<UnitMovement>();
        if (move != null)
            move.SetPath(targetPath);

        // 4. 启动当前卡牌的冷却（如果有）
        if (CardManager.Instance.CurrentSelected != null)
            CardManager.Instance.CurrentSelected.StartCooldown();

        // 5. 退出部署模式并清空卡牌选中状态
        ExitDeployMode();
        CardManager.Instance.DeselectCurrentCard();

        Debug.Log($"单位部署成功，位置：{spawnPos}");
    }
}