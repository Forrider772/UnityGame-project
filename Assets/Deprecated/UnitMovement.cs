using UnityEngine;

/// <summary>
/// 单位移动模块
/// 职责：只处理移动相关逻辑
/// 1. 沿平滑路径自动行走（支持曲线/直线统一 API）
/// 2. 向目标点位追击移动
/// 3. 判断路径是否走完
/// 不参与索敌、攻击、UI、AI决策
/// </summary>
public class UnitMovement : MonoBehaviour
{
    [Header("路径绑定")]
    public PathManager pathManager; // 所属路径管理器

    private UnitAttr attr;         // 自身属性引用
    private float pathProgress;    // 归一化距离 [0, 1]，沿路径的完成进度
    private float totalPathLength; // 路径总弧长（缓存，避免每帧计算）
    private bool isStopped;        // 驻扎等情况下暂停移动

    /// <summary>
    /// 初始化：自动获取同物体上的属性组件
    /// </summary>
    void Awake()
    {
        attr = GetComponent<UnitAttr>();
    }

    /// <summary>
    /// 外部生成器调用：给单位设置行走路径
    /// </summary>
    public void SetPath(PathManager path)
    {
        pathManager = path;
        pathProgress = 0f;
        totalPathLength = path != null ? path.GetTotalArcLength() : 1f;
    }

    /// <summary>
    /// 暂停移动（驻扎时调用）
    /// </summary>
    public void StopMovement()
    {
        isStopped = true;
    }

    /// <summary>
    /// 恢复移动（离开驻扎时调用）
    /// </summary>
    public void ResumeMovement()
    {
        isStopped = false;
    }

    /// <summary>
    /// 沿路径匀速前进（支持曲线/直线，统一使用归一化距离）
    /// 循环模式：到达终点后自动回到起点继续前进
    /// </summary>
    public void MoveAlongPath()
    {
        if (isStopped) return;
        if (pathManager == null) return;

        // 非循环模式：路径已走完则停止
        if (!pathManager.isLooping && pathProgress >= 1f) return;

        // 按弧长参数化推进，保证沿曲线匀速运动
        pathProgress += (attr.moveSpeed * Time.deltaTime) / totalPathLength;

        // 循环模式：模运算回到起点继续前进
        if (pathManager.isLooping)
        {
            pathProgress %= 1.0f;
        }
        else
        {
            pathProgress = Mathf.Min(pathProgress, 1f);
        }

        // 移动到曲线上的位置
        transform.position = pathManager.GetCurvePoint(pathProgress);
    }

    /// <summary>
    /// 向指定世界坐标追击移动
    /// </summary>
    /// <param name="targetPos">目标坐标</param>
    public void MoveToTarget(Vector2 targetPos)
    {
        if (isStopped) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            attr.moveSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 飞行单位专用：直线飞向目标点
    /// </summary>
    public void FlyToTarget(Vector3 targetPos)
    {
        if (isStopped) return;

        Vector3 moveDir = (targetPos - transform.position).normalized;
        transform.Translate(moveDir * attr.moveSpeed * Time.deltaTime, Space.World);
        transform.right = moveDir;
    }

    /// <summary>
    /// 判断是否已经走完整条路径
    /// 循环路径永远不会"走完"，始终返回 false
    /// </summary>
    /// <returns>true=已走完 false=还在路径中</returns>
    public bool IsPathCompleted()
    {
        if (pathManager == null) return true;
        if (pathManager.isLooping) return false;
        return pathProgress >= 1f;
    }

    /// <summary>
    /// 判断飞行单位是否到达目标
    /// </summary>
    public bool IsFlyingTargetReached(Vector3 targetPos, float threshold = 0.5f)
    {
        return Vector3.Distance(transform.position, targetPos) < threshold;
    }
}
