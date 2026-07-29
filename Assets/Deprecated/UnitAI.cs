using UnityEngine;

/// <summary>
/// 单位AI大脑决策
/// 职责：只做行为判断，不实现具体移动、攻击细节
/// 行为优先级：
/// 1. 范围内有敌方单位 → 脱离驻扎 → 战斗追击攻击
/// 2. 驻扎点范围内 → 驻扎/抢占判定
/// 3. 资源点范围内 → 驻扎/抢占判定
/// 4. 无敌人且路径未走完 → 沿平滑路径前进
/// 5. 路径已走完 → 锁定并攻击对方防御塔
/// </summary>
public class UnitAI : MonoBehaviour
{
    private UnitCombat combat;   // 战斗模块引用
    private UnitMovement movement; // 移动模块引用
    private UnitAttr attr;       // 属性引用

    // 敌方基地/防御塔坐标（在Inspector中设置）
    [Header("目标配置")]
    public Vector3 enemyBasePosition;

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
        // 优先级2：驻扎点驻扎/抢占判定 [新增]
        else if (TryHandleGarrisonPoint())
        {
            // 驻扎逻辑在 TryHandleGarrisonPoint 内部处理
        }
        // 优先级3：资源点驻扎/抢占判定
        else if (TryHandleResourcePoint())
        {
            // 驻扎逻辑在TryHandleResourcePoint内部处理
        }
        // 优先级3：无敌人 + 路径未走完 → 移动（新增分支）
        else
        {
            if (attr.moveType == MoveType.Ground)
            {
                // 地面单位：沿预设路径行走（原有逻辑不变）
                if (!movement.IsPathCompleted())
                {
                    movement.MoveAlongPath();
                }
                else
                {
                    // 路径走完攻击防御塔
                    combat.SetTargetToTower();
                    combat.TryAttack();
                }
            }
            else if (attr.moveType == MoveType.Flying)
            {
                // 飞行单位：直接直线飞向敌方基地
                if (!movement.IsFlyingTargetReached(enemyBasePosition))
                {
                    movement.FlyToTarget(enemyBasePosition);
                }
                else
                {
                    // 到达基地后攻击基地
                    combat.SetTargetToTower();
                    combat.TryAttack();
                }
            }
        }
    }

    /// <summary>
    /// 驻扎单位检测到敌人时，先脱离驻扎再战斗
    /// 同时处理资源点和驻扎点
    /// </summary>
    void LeaveGarrisonIfNeeded()
    {
        if (!attr.isGarrisoned) return;

        if (attr.garrisonedAt != null)
        {
            attr.garrisonedAt.RemoveGarrison(gameObject);
            movement.ResumeMovement();
        }
    }

    /// <summary>
    /// 尝试处理驻扎点交互（驻扎或抢占），成功返回 true
    /// 单位走到哪就检测脚下有没有已进入范围的驻扎点
    /// </summary>
    bool TryHandleGarrisonPoint()
    {
        if (GarrisonPointManager.Instance == null) return false;

        // 检测单位是否已进入某个驻扎点的交互范围（不再找最近，只看是否已进入）
        GarrisonPoint gp = GarrisonPointManager.Instance.GetGarrisonPointInRange(
            transform.position, attr.camp);

        if (gp == null) return false;

        // 可驻扎 → 驻扎
        if (gp.CanGarrison(gameObject))
        {
            gp.AddGarrison(gameObject);
            return true;
        }

        // 敌方占领 → 触发抢占战斗
        if (gp.occupyingCamp != null && gp.occupyingCamp != attr.camp && !gp.isContested)
        {
            gp.StartContest(gameObject);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试处理资源点交互（驻扎或抢占），成功返回true
    /// 单位沿路径行走，进入资源点范围即触发交互，不依赖路径系统
    /// </summary>
    bool TryHandleResourcePoint()
    {
        if (ResourcePointManager.Instance == null) return false;

        // 单位走到哪就检查是否进入了某个资源点的范围
        ResourcePoint rp = ResourcePointManager.Instance.GetResourcePointInRange(transform.position);
        if (rp == null) return false;

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