using UnityEngine;

/// <summary>
/// 单位移动模块
/// 职责：只处理移动相关逻辑
/// 1. 沿平滑路径自动行走
/// 2. 向目标点位追击移动
/// 3. 判断路径是否走完
/// 不参与索敌、攻击、UI、AI决策
/// </summary>
public class UnitMovement : MonoBehaviour
{
    [Header("路径绑定")]
    public PathManager pathManager; // 所属路径管理器

    private UnitAttr attr;       // 自身属性引用
    private int currentPathIndex;// 当前行走到的路径点索引

    /// <summary>
    /// 初始化：自动获取同物体上的属性组件
    /// </summary>
    void Awake()
    {
        attr = GetComponent<UnitAttr>();
    }

    /// <summary>
    /// 沿预设平滑路径匀速前进
    /// </summary>
    public void MoveAlongPath()
    {
        // 路径为空 或 已经走到终点，直接返回
        if (pathManager == null || currentPathIndex >= pathManager.pathPoints.Count)
            return;

        // 获取当前目标路径点
        Transform targetPoint = pathManager.pathPoints[currentPathIndex];

        // 向路径点匀速移动
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPoint.position,
            attr.moveSpeed * Time.deltaTime
        );

        // 距离足够近，判定到达，切换下一个路径点
        if (Vector2.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            currentPathIndex++;
        }
    }

    /// <summary>
    /// 向指定世界坐标追击移动
    /// </summary>
    /// <param name="targetPos">目标坐标</param>
    public void MoveToTarget(Vector2 targetPos)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            attr.moveSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 判断是否已经走完整条路径
    /// </summary>
    /// <returns>true=已走完 false=还在路径中</returns>
    public bool IsPathCompleted()
    {
        return pathManager == null || currentPathIndex >= pathManager.pathPoints.Count;
    }
}