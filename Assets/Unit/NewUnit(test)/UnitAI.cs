using UnityEngine;

/// <summary>
/// 单位AI大脑决策
/// 职责：只做行为判断，不实现具体移动、攻击细节
/// 行为优先级：
/// 1. 范围内有敌方单位 → 进入战斗追击攻击
/// 2. 无敌人且路径未走完 → 沿平滑路径前进
/// 3. 路径已走完 → 锁定并攻击对方防御塔
/// </summary>
public class UnitAI : MonoBehaviour
{
    private UnitCombat combat;     // 战斗模块引用
    private UnitMovement movement; // 移动模块引用
    private UnitAttr attr;         // 属性引用

    /// <summary>
    /// 自动获取同物体依赖组件
    /// </summary>
    void Awake()
    {
        combat = GetComponent<UnitCombat>();
        movement = GetComponent<UnitMovement>();
        attr = GetComponent<UnitAttr>();
    }

    void Update()
    {
        // 组件缺失 直接容错返回
        if (combat == null || movement == null || attr == null) return;
        // 已经死亡不再执行AI
        if (attr.currentHp <= 0) return;

        // 优先级1：发现敌人 → 战斗模式
        if (combat.DetectEnemy())
        {
            combat.TryAttack();
        }
        // 优先级2：无敌人 + 路径未走完 → 沿路径行走
        else if (!movement.IsPathCompleted())
        {
            movement.MoveAlongPath();
        }
        // 优先级3：路径终点 → 攻击防御塔
        else
        {
            combat.SetTargetToTower();
            combat.TryAttack();
        }
    }
}