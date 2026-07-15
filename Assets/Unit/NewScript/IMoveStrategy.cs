using UnityEngine;

/// <summary>
/// 移动策略接口
/// 所有移动策略（地面沿路径、飞行沿路径等）必须实现此接口
/// UnitBrain 通过 GetComponent&lt;IMoveStrategy&gt;() 找到移动策略并调用
/// </summary>
public interface IMoveStrategy
{
    /// <summary>绑定路径（从起点开始），由 WaveGenerator/CardDeploy 在生成时调用</summary>
    void SetPath(PathManager path);

    /// <summary>绑定路径并定位到路径上距 worldPosition 最近的点（初始布阵用）</summary>
    void SetPathAtClosestPoint(PathManager path, Vector3 worldPosition);

    /// <summary>每帧沿路径移动，speed 来自 UnitAttr.moveSpeed</summary>
    void Move(float deltaTime, float speed);

    /// <summary>驻扎时暂停移动</summary>
    void Stop();

    /// <summary>离开驻扎时恢复移动</summary>
    void Resume();

    /// <summary>路径是否已走完（循环路径永远不完成）</summary>
    bool IsPathCompleted();

    /// <summary>向目标点追击移动（战斗中使用）。地面和飞行各自实现不同的追击方式</summary>
    void MoveToward(Vector2 target, float speed);
}
