using UnityEngine;

/// <summary>
/// 移动策略抽象基类
/// 提取 GroundMoveStrategy 和 FlightMoveStrategy 的共同逻辑
///
/// 支持两阶段移动：
///   1. 寻路接近（Approach）：单位在路径外时，直线走向路径最近点
///   2. 路径跟随（Follow）：到达路径后，沿路径匀速推进
///
/// 支持追击后路径回归：
///   追击(MoveToward)脱离路径 → 战斗结束 → ReturnToPath 自动寻路回到路径
/// </summary>
public abstract class BaseMoveStrategy : MonoBehaviour, IMoveStrategy
{
    // ==================== 路径状态 ====================
    protected PathManager pathManager;
    protected float pathProgress;            // 归一化距离 [0, 1]
    protected float totalPathLength;         // 路径总弧长（缓存，避免每帧计算）
    protected bool isStopped;

    // ==================== 寻路接近阶段 ====================
    protected bool isApproachingPath;        // 是否处于"走向路径"阶段
    protected Vector3 pathEntryPoint;        // 路径上的目标进入点

    // ==================== 追击回归 ====================
    protected bool wasInterruptedByCombat;   // 是否因追击脱离了路径
    protected float preCombatPathProgress;   // 追击前的路径进度

    // ==================== IMoveStrategy 实现 ====================

    public virtual void SetPath(PathManager path)
    {
        pathManager = path;
        pathProgress = 0f;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
        isApproachingPath = false;
        wasInterruptedByCombat = false;
    }

    public virtual void SetPathAtClosestPoint(PathManager path, Vector3 worldPosition)
    {
        pathManager = path;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;

        if (path != null)
        {
            var (closestPoint, curveT, distance) = path.GetClosestPoint(worldPosition);
            pathProgress = curveT;

            // 单位离路径较远 → 进入寻路接近阶段
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

        wasInterruptedByCombat = false;
    }

    public virtual void Move(float deltaTime, float speed)
    {
        if (isStopped) return;
        if (pathManager == null) return;

        // ====== 追击回归：战斗结束后自动走回路径 ======
        if (wasInterruptedByCombat)
        {
            var (closestPt, curveT, _) = pathManager.GetClosestPoint(transform.position);
            pathEntryPoint = closestPt;
            pathProgress = curveT;
            isApproachingPath = true;
            wasInterruptedByCombat = false;
        }

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

        Vector2 curvePoint = pathManager.GetCurvePoint(pathProgress);
        OnPathFollow(curvePoint);
    }

    /// <summary>
    /// 路径跟随时的每帧回调。子类覆盖以定制行为（如飞行加高度偏移）。
    /// </summary>
    protected virtual void OnPathFollow(Vector2 curvePoint)
    {
        transform.position = curvePoint;
    }

    public virtual void Stop() => isStopped = true;

    public virtual void Resume() => isStopped = false;

    public virtual bool IsPathCompleted()
    {
        if (pathManager == null) return true;
        if (isApproachingPath) return false;     // 还在接近路径中
        if (pathManager.isLooping) return false;
        return pathProgress >= 1f;
    }

    /// <summary>
    /// 追击移动 —— 脱离路径向目标直线移动
    /// 调用此方法会自动标记 needsPathReturn，下次 Move() 时会先回归路径
    /// </summary>
    public virtual void MoveToward(Vector2 target, float speed)
    {
        if (isStopped) return;

        // 标记脱离路径，战后自动回归
        wasInterruptedByCombat = true;
        preCombatPathProgress = pathProgress;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }
}
