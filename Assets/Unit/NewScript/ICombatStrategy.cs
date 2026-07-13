using System;
using UnityEngine;

/// <summary>
/// 战斗策略接口
/// 所有战斗策略（近战、远程投射、治疗等）必须实现此接口
/// UnitBrain 通过 GetComponent&lt;ICombatStrategy&gt;() 找到战斗策略并调用
/// </summary>
public interface ICombatStrategy
{
    /// <summary>当前锁定目标（供 UnitBrain 读取）</summary>
    Transform CurrentTarget { get; }

    /// <summary>
    /// 在指定范围内搜寻敌方目标
    /// 返回最近的有效目标，近战自动跳过飞行单位，无目标返回 null
    /// </summary>
    Transform DetectTarget(UnitAttr attr, Vector2 position);

    /// <summary>
    /// 执行一次战斗行动（攻击或治疗）
    /// 返回 true 表示执行了行动，false 表示本帧无行动
    /// </summary>
    bool TryExecute(Transform target, float deltaTime, UnitAttr attr,
                    Vector2 position, IMoveStrategy movement);

    /// <summary>路径走完后锁定敌方防御塔</summary>
    void SetTowerTarget(UnitAttr attr);

    /// <summary>
    /// 受到伤害，内部结算防御减伤、扣血、触发死亡回调
    /// </summary>
    void TakeDamage(float damage, AttackType type, UnitAttr attr, Action onDie);
}
