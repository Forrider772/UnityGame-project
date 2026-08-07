using UnityEngine;

/// <summary>
/// 地面沿路径移动策略
/// 继承 BaseMoveStrategy，无额外特殊行为。
/// 路径跟随位置由基类 OnPathFollow 默认实现直接设置。
/// </summary>
public class GroundMoveStrategy : BaseMoveStrategy
{
    // 地面单位无特殊行为，全部逻辑在 BaseMoveStrategy 中
}
