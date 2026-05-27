using UnityEngine;

/// <summary>
/// 单位AI大脑决策
/// 职责：只做行为判断，不实现具体移动、攻击细节
/// 行为优先级：
/// 1. 范围内有敌方单位 → 脱离驻扎 → 战斗追击攻击
/// 2. 资源点范围内 → 驻扎/抢占判定
/// 3. 无敌人且路径未走完 → 沿平滑路径前进
/// 4. 路径已走完 → 锁定并攻击对方防御塔
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
        if (combat == null || movement == null || attr == null) return;
        if (attr.currentHp <= 0) return;

        // 优先级1：发现敌人 → 离开驻扎 → 战斗模式
        if (combat.DetectEnemy())
        {
            LeaveGarrisonIfNeeded();
            combat.TryAttack();
        }
        // 优先级2：资源点驻扎/抢占判定
        else if (TryHandleResourcePoint())
        {
            // 驻扎逻辑在 TryHandleResourcePoint 内部处理
        }
        // 优先级3：无敌人 + 路径未走完 → 沿路径行走
        else if (!movement.IsPathCompleted())
        {
            movement.MoveAlongPath();
        }
        // 优先级4：路径终点 → 攻击防御塔
        else
        {
            combat.SetTargetToTower();
            combat.TryAttack();
        }
    }

    /// <summary>
    /// 驻扎单位检测到敌人时，先脱离驻扎再战斗
    /// </summary>
    void LeaveGarrisonIfNeeded()
    {
        if (!attr.isGarrisoned || attr.garrisonedPoint == null) return;
        attr.garrisonedPoint.RemoveGarrison(gameObject);
        movement.ResumeMovement();
    }

    /// <summary>
    /// 尝试处理资源点交互（驻扎或抢占），成功返回 true
    /// </summary>
    bool TryHandleResourcePoint()
    {
        if (movement.pathManager == null) return false;
        if (ResourcePointManager.Instance == null) return false;

        ResourcePoint rp = ResourcePointManager.Instance.GetResourcePointOnPath(
            movement.pathManager.pathId);

        if (rp == null) return false;
        if (!rp.IsUnitInRange(gameObject)) return false;

        // 情况A/B: 可驻扎 → 驻扎
        if (rp.CanGarrison(gameObject))
        {
            rp.AddGarrison(gameObject);
            return true;
        }

        // 情况C: 敌方占领 → 触发抢占战斗
        if (rp.occupyingCamp != null && rp.occupyingCamp != attr.camp && !rp.isContested)
        {
            rp.StartContest(gameObject);
            return true;
        }

        return false;
    }
}