using UnityEngine;

/// <summary>
/// 飞行沿路径移动策略
/// 实现 IMoveStrategy，沿 Flight 类型 PathManager 路径匀速推进
/// 飞行路径通常配置为 2-点直线（起点=部署点，终点=敌方塔位置）
/// FlightMoveStrategy 与 GroundMoveStrategy 行为一致：沿路径匀速移动，战斗时追击目标
/// 飞行单位不做旋转，保持原始朝向
///
/// 支持两阶段移动：
///   1. 寻路接近（Approach）：单位在路径外时，直线走向路径最近点
///   2. 路径跟随（Follow）：到达路径后，沿路径匀速推进
/// </summary>
public class FlightMoveStrategy : MonoBehaviour, IMoveStrategy
{
    private PathManager pathManager;
    private float pathProgress;          // 归一化距离 [0, 1]
    private float totalPathLength;       // 路径总弧长（缓存）
    private bool isStopped;

    // 寻路接近阶段
    private bool isApproachingPath;      // 是否处于"走向路径"阶段
    private Vector3 pathEntryPoint;      // 路径上的目标进入点

    // ==================== IMoveStrategy 实现 ====================

    public void SetPath(PathManager path)
    {
        pathManager = path;
        pathProgress = 0f;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
        isApproachingPath = false;
    }

    public void SetPathAtClosestPoint(PathManager path, Vector3 worldPosition)
    {
        pathManager = path;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;

        if (path != null)
        {
            var (closestPoint, curveT, distance) = path.GetClosestPoint(worldPosition);
            pathProgress = curveT;

            // 单位离路径较远 → 进入寻路接近阶段，先直线走到进入点
            if (distance > 0.1f)
            {
                isApproachingPath = true;
                pathEntryPoint = closestPoint;
            }
            else
            {
                isApproachingPath = false;
            }
        }
        else
        {
            pathProgress = 0f;
            isApproachingPath = false;
        }
    }

    public void Move(float deltaTime, float speed)
    {
        if (isStopped) return;
        if (pathManager == null) return;

        // ====== 阶段 1：寻路接近 —— 直线走向路径进入点 ======
        if (isApproachingPath)
        {
            float step = speed * deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, pathEntryPoint, step);

            // 抵达进入点 → 吸附到路径，切换为跟随模式
            if (Vector2.Distance(transform.position, pathEntryPoint) < 0.05f)
            {
                transform.position = pathEntryPoint;
                isApproachingPath = false;
            }
            return;
        }

        // ====== 阶段 2：路径跟随 —— 沿路径匀速推进 ======
        if (!pathManager.isLooping && pathProgress >= 1f) return;

        pathProgress += (speed * deltaTime) / totalPathLength;

        if (pathManager.isLooping)
            pathProgress %= 1.0f;
        else
            pathProgress = Mathf.Min(pathProgress, 1f);

        // 更新位置（飞行单位不旋转朝向）
        transform.position = pathManager.GetCurvePoint(pathProgress);
    }

    public void Stop() => isStopped = true;

    public void Resume() => isStopped = false;

    public bool IsPathCompleted()
    {
        if (pathManager == null) return true;
        if (isApproachingPath) return false;
        if (pathManager.isLooping) return false;
        return pathProgress >= 1f;
    }

    public void MoveToward(Vector2 target, float speed)
    {
        if (isStopped) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
        // 飞行单位不旋转朝向
    }
}
