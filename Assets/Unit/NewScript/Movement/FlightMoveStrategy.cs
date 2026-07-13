using UnityEngine;

/// <summary>
/// 飞行沿路径移动策略
/// 实现 IMoveStrategy，沿 Flight 类型 PathManager 路径匀速推进
/// 飞行路径通常配置为 2-点直线（起点=部署点，终点=敌方塔位置）
/// FlightMoveStrategy 与 GroundMoveStrategy 行为一致：沿路径匀速移动，战斗时追击目标
/// 飞行单位不做旋转，保持原始朝向
/// </summary>
public class FlightMoveStrategy : MonoBehaviour, IMoveStrategy
{
    private PathManager pathManager;
    private float pathProgress;      // 归一化距离 [0, 1]
    private float totalPathLength;   // 路径总弧长（缓存）
    private bool isStopped;

    // ==================== IMoveStrategy 实现 ====================

    public void SetPath(PathManager path)
    {
        pathManager = path;
        pathProgress = 0f;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
    }

    public void Move(float deltaTime, float speed)
    {
        if (isStopped) return;
        if (pathManager == null) return;

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
