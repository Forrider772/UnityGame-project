using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 驻扎点管理器（场景级单例）
/// 管理所有动态创建的驻扎点，提供创建/查询/删除接口
/// 其他脚本通过此管理器创建和查询驻扎点
/// </summary>
public class GarrisonPointManager : MonoBehaviour
{
    public static GarrisonPointManager Instance;

    [Header("预制体")]
    [Tooltip("驻扎点预制体（需包含 GarrisonPoint + GarrisonPointVisual + CircleCollider2D）")]
    public GameObject garrisonPointPrefab;

    [Header("吸附设置")]
    [Tooltip("创建驻扎点时，鼠标位置距离最近路径超过此值则不吸附（返回null）")]
    public float snapMaxDistance = 2f;

    private List<GarrisonPoint> _allGarrisonPoints = new List<GarrisonPoint>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    // ==================== 注册/注销 ====================

    public void Register(GarrisonPoint gp)
    {
        if (!_allGarrisonPoints.Contains(gp))
            _allGarrisonPoints.Add(gp);
    }

    public void Unregister(GarrisonPoint gp)
    {
        _allGarrisonPoints.Remove(gp);
    }

    // ==================== 工厂方法（脚本API入口）====================

    /// <summary>
    /// 在指定世界坐标创建驻扎点（自动吸附到最近路径路段）
    /// </summary>
    /// <param name="worldPosition">目标世界坐标</param>
    /// <param name="effectiveCamps">生效阵营列表</param>
    /// <returns>创建的 GarrisonPoint，若附近无路径则返回 null</returns>
    public GarrisonPoint CreateGarrisonPoint(Vector2 worldPosition, List<CampType> effectiveCamps)
    {
        if (garrisonPointPrefab == null)
        {
            Debug.LogError("GarrisonPointManager: garrisonPointPrefab 未设置！");
            return null;
        }

        if (LevelPathManager.Instance == null)
        {
            Debug.LogWarning("GarrisonPointManager: LevelPathManager.Instance 不存在，无法吸附路径");
            return null;
        }

        // 遍历所有阵营的所有路径，找最近路段
        PathManager bestPath = null;
        int bestSegIndex = 0;
        float bestT = 0f;
        Vector2 bestSnapPoint = worldPosition;
        float bestDist = snapMaxDistance;

        foreach (CampType camp in System.Enum.GetValues(typeof(CampType)))
        {
            List<PathManager> paths = LevelPathManager.Instance.GetAllPathsByCamp(camp);
            foreach (var path in paths)
            {
                if (path == null || path.pathPoints.Count < 2) continue;

                Vector2[] waypoints = path.GetWaypoints2D();
                float dist = Math2DHelper.MinDistancePointToPolyline(worldPosition, waypoints);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPath = path;
                    bestSnapPoint = Math2DHelper.ClosestPointOnPolyline(
                        worldPosition, waypoints, out bestSegIndex, out bestT);
                }
            }
        }

        if (bestPath == null)
        {
            Debug.LogWarning($"GarrisonPointManager: 坐标 {worldPosition} 附近 {snapMaxDistance} 范围内无路径，无法创建驻扎点");
            return null;
        }

        return CreateGarrisonPointOnPath(bestPath, bestSegIndex, bestT, effectiveCamps);
    }

    /// <summary>
    /// 在指定路径的指定路段上创建驻扎点
    /// </summary>
    /// <param name="path">目标路径</param>
    /// <param name="segmentIndex">路段索引 [0, pathPoints.Count-2]</param>
    /// <param name="t">线段上的插值参数 [0,1]</param>
    /// <param name="effectiveCamps">生效阵营列表</param>
    /// <returns>创建的 GarrisonPoint</returns>
    public GarrisonPoint CreateGarrisonPointOnPath(
        PathManager path, int segmentIndex, float t, List<CampType> effectiveCamps)
    {
        if (garrisonPointPrefab == null)
        {
            Debug.LogError("GarrisonPointManager: garrisonPointPrefab 未设置！");
            return null;
        }

        if (path == null || path.pathPoints.Count < 2)
        {
            Debug.LogWarning("GarrisonPointManager: 路径无效或路径点不足");
            return null;
        }

        // 限制索引范围
        segmentIndex = Mathf.Clamp(segmentIndex, 0, path.pathPoints.Count - 2);
        t = Mathf.Clamp01(t);

        // 计算路段两端点，插值得到世界坐标
        Vector2 a = path.pathPoints[segmentIndex].position;
        Vector2 b = path.pathPoints[segmentIndex + 1].position;
        Vector2 snapPos = Vector2.Lerp(a, b, t);

        // 实例化驻扎点
        GameObject go = Instantiate(garrisonPointPrefab, snapPos, Quaternion.identity);
        GarrisonPoint gp = go.GetComponent<GarrisonPoint>();
        if (gp == null)
        {
            Debug.LogError("GarrisonPointManager: garrisonPointPrefab 缺少 GarrisonPoint 组件！");
            Destroy(go);
            return null;
        }

        // 初始化驻扎点数据
        gp.worldPosition = snapPos;
        gp.boundPath = path;
        gp.boundSegmentIndex = segmentIndex;
        gp.boundSegmentT = t;
        gp.effectiveCamps = new List<CampType>(effectiveCamps);

        go.transform.position = snapPos;

        Debug.Log($"驻扎点创建成功: 坐标={snapPos}, 路线={path.pathId}, 路段={segmentIndex}, 生效阵营=[{string.Join(",", effectiveCamps)}]");

        return gp;
    }

    /// <summary>
    /// 移除驻扎点
    /// </summary>
    public void RemoveGarrisonPoint(GarrisonPoint gp)
    {
        if (gp == null) return;
        Unregister(gp);
        Destroy(gp.gameObject);
    }

    // ==================== 查询方法 ====================

    /// <summary>
    /// 获取对指定单位阵营有效的最近驻扎点
    /// </summary>
    /// <param name="position">查询位置（世界坐标）</param>
    /// <param name="unitCamp">单位所属阵营</param>
    /// <returns>最近的驻扎点，若无则返回 null</returns>
    public GarrisonPoint GetNearestGarrisonPoint(Vector2 position, CampType unitCamp)
    {
        GarrisonPoint nearest = null;
        float minDist = float.MaxValue;

        foreach (var gp in _allGarrisonPoints)
        {
            if (gp == null) continue;

            // 只返回对该阵营生效的驻扎点
            if (!gp.IsEffectiveFor(unitCamp)) continue;

            float dist = Vector2.Distance(position, gp.worldPosition);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = gp;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 获取某条路径上的所有驻扎点
    /// </summary>
    public List<GarrisonPoint> GetGarrisonPointsOnPath(PathID pathId)
    {
        List<GarrisonPoint> result = new List<GarrisonPoint>();
        foreach (var gp in _allGarrisonPoints)
        {
            if (gp != null && gp.boundPath != null && gp.boundPath.pathId == pathId)
                result.Add(gp);
        }
        return result;
    }

    /// <summary>
    /// 获取某阵营占领的所有活跃驻扎点
    /// </summary>
    public List<GarrisonPoint> GetActivePointsForCamp(CampType camp)
    {
        List<GarrisonPoint> result = new List<GarrisonPoint>();
        foreach (var gp in _allGarrisonPoints)
        {
            if (gp != null && gp.isActive && gp.occupyingCamp == camp)
                result.Add(gp);
        }
        return result;
    }

    /// <summary>
    /// 获取所有驻扎点
    /// </summary>
    public List<GarrisonPoint> GetAllGarrisonPoints()
    {
        _allGarrisonPoints.RemoveAll(gp => gp == null);
        return new List<GarrisonPoint>(_allGarrisonPoints);
    }
}
