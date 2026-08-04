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

    // ==================== 传送段状态 ====================
    protected bool isTeleporting;            // 是否处于传送等待中（期间单位隐藏+无敌）
    protected float teleportWaitTimer;       // 传送等待计时器（固定时间，不受速度影响）
    protected int currentTeleportSegment;    // 当前传送的路径段索引（-1 = 不在传送）

    // ==================== 可见性控制缓存 ====================
    protected SpriteRenderer spriteRenderer; // 单位精灵渲染器缓存
    protected Collider2D unitCollider;       // 单位碰撞体缓存

    // ==================== IMoveStrategy 实现 ====================

    public virtual void SetPath(PathManager path)
    {
        pathManager = path;
        pathProgress = 0f;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
        isApproachingPath = false;
        wasInterruptedByCombat = false;
        ResetTeleportState();
        SetUnitVisible(true);
    }

    public virtual void SetPathAtClosestPoint(PathManager path, Vector3 worldPosition)
    {
        pathManager = path;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
        ResetTeleportState();
        SetUnitVisible(true);

        if (path != null)
        {
            var (closestPoint, curveT, distance) = path.GetClosestPoint(worldPosition);
            pathProgress = curveT;

            // 若最近点落在传送段内部（传送段无"可行走"内部），吸附到段起点
            int segIndex = path.GetSegmentAtProgress(pathProgress);
            if (path.GetConnectionType(segIndex) == ConnectionType.Teleport)
            {
                pathProgress = path.GetSegmentStartProgress(segIndex);
                closestPoint = path.GetCurvePoint(pathProgress);
            }

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

            // 若回退点落在传送段内部，吸附到段起点
            int segIdx = pathManager.GetSegmentAtProgress(pathProgress);
            if (pathManager.GetConnectionType(segIdx) == ConnectionType.Teleport)
            {
                pathProgress = pathManager.GetSegmentStartProgress(segIdx);
                pathEntryPoint = pathManager.GetCurvePoint(pathProgress);
            }

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

                // 若到达点在传送段起点 → 直接进入传送等待
                int segIdx = pathManager.GetSegmentAtProgress(pathProgress);
                if (pathManager.GetConnectionType(segIdx) == ConnectionType.Teleport)
                    BeginTeleport(segIdx);
            }
            return;
        }

        // ====== 阶段 2：路径跟随 ======
        if (!pathManager.isLooping && pathProgress >= 1f) return;

        // ★ 传送等待状态：位置锁定在段起点，等待结束后瞬移到段终点
        if (isTeleporting)
        {
            UpdateTeleport(deltaTime);
            return;
        }

        // ★ 检测进入传送段
        int currentSeg = pathManager.GetSegmentAtProgress(pathProgress);
        if (pathManager.GetConnectionType(currentSeg) == ConnectionType.Teleport)
        {
            BeginTeleport(currentSeg);
            return;
        }

        // ====== 正常行走 —— 沿路径匀速推进 ======
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

    // ==================== 传送段逻辑 ====================

    /// <summary>是否正在传送等待中（UnitBrain 借此跳过索敌/驻扎检测）</summary>
    public bool IsTeleporting => isTeleporting;

    /// <summary>重置传送状态（设置新路径或取消传送时调用）</summary>
    protected void ResetTeleportState()
    {
        isTeleporting = false;
        teleportWaitTimer = 0f;
        currentTeleportSegment = -1;
    }

    /// <summary>
    /// 控制单位可见性和可交互性
    /// 传送期间：视觉消失 + 碰撞关闭（无敌）+ HP 血条隐藏
    /// </summary>
    protected virtual void SetUnitVisible(bool visible)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (unitCollider == null)
            unitCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        if (unitCollider != null)
            unitCollider.enabled = visible;

        var ui = GetComponent<UnitUI>();
        if (ui != null)
            ui.SetHpBarVisible(visible);
    }

    /// <summary>进入传送等待：单位消失，等待 teleportTime 后瞬移到段终点</summary>
    protected void BeginTeleport(int segmentIndex)
    {
        isTeleporting = true;
        teleportWaitTimer = 0f;
        currentTeleportSegment = segmentIndex;

        // 吸附到段起点（防止浮点漂移导致不在精确起点）
        pathProgress = pathManager.GetSegmentStartProgress(segmentIndex);

        // 单位隐藏 + 无敌
        SetUnitVisible(false);
        OnTeleportStart();
    }

    /// <summary>传送等待帧更新：锁定段起点，计时结束后瞬移</summary>
    protected void UpdateTeleport(float deltaTime)
    {
        // 位置锁定在段起点（隐藏期间保持物理稳定）
        float segStart = pathManager.GetSegmentStartProgress(currentTeleportSegment);
        transform.position = pathManager.GetCurvePoint(segStart);

        teleportWaitTimer += deltaTime;

        if (teleportWaitTimer >= pathManager.teleportTime)
        {
            // 传送完成 → 瞬移到段终点并恢复显示
            // 微推进进入下一段，避免 pathProgress 恰好落在段边界时
            // GetSegmentAtProgress 判定仍在传送段导致重复传送（卡在传送点）
            const float boundaryEpsilon = 0.0001f;
            float segEnd = pathManager.GetSegmentEndProgress(currentTeleportSegment);
            pathProgress = pathManager.isLooping
                ? (segEnd + boundaryEpsilon) % 1.0f
                : Mathf.Min(segEnd + boundaryEpsilon, 1f);
            transform.position = pathManager.GetCurvePoint(pathProgress);
            ResetTeleportState();
            SetUnitVisible(true);
            OnTeleportComplete();
        }
    }

    /// <summary>传送开始回调（子类可覆盖播放消失特效）</summary>
    protected virtual void OnTeleportStart() { }

    /// <summary>传送完成回调（子类可覆盖播放出现特效）</summary>
    protected virtual void OnTeleportComplete() { }

    public virtual void Stop() => isStopped = true;

    public virtual void Resume() => isStopped = false;

    public virtual bool IsPathCompleted()
    {
        if (pathManager == null) return true;
        if (isApproachingPath) return false;     // 还在接近路径中
        if (isTeleporting) return false;         // 还在传送等待中
        if (pathManager.isLooping) return false;
        return pathProgress >= 1f;
    }

    /// <summary>
    /// 追击移动 —— 脱离路径向目标直线移动
    /// 调用此方法会自动标记 needsPathReturn，下次 Move() 时会先回归路径
    /// 若传送等待中调用，会取消传送并恢复可见
    /// </summary>
    public virtual void MoveToward(Vector2 target, float speed)
    {
        if (isStopped) return;

        // 传送等待中进入战斗 → 取消传送并恢复可见（无敌状态结束）
        if (isTeleporting)
        {
            ResetTeleportState();
            SetUnitVisible(true);
        }

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
