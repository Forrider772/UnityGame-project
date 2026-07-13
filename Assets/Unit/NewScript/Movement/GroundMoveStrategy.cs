using UnityEngine;

/// <summary>
/// 地面沿路径移动策略
/// 实现 IMoveStrategy，沿 PathManager 路径匀速推进（支持曲线/直线/循环）
/// </summary>
public class GroundMoveStrategy : MonoBehaviour, IMoveStrategy
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

        // 非循环路径已走完则停止
        if (!pathManager.isLooping && pathProgress >= 1f) return;

        // 弧长参数化推进，保证沿曲线匀速
        pathProgress += (speed * deltaTime) / totalPathLength;

        // 循环模式：到达终点后回到起点
        if (pathManager.isLooping)
            pathProgress %= 1.0f;
        else
            pathProgress = Mathf.Min(pathProgress, 1f);

        // 更新 Transform 位置
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
    }
}
