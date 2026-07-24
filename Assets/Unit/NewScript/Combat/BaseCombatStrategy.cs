using System;
using UnityEngine;

/// <summary>
/// 战斗策略抽象基类
/// 提取 Melee/Ranged/Healer 三者的重复代码，并实现攻击间隔系统
///
/// 攻击间隔模型：
///   Windup(前摇) → Recovery(后摇) → IdleWait(空闲) → Idle(就绪) → 下一轮
///   攻速系数等比缩放全部三段时间
///
/// 子类只需实现：PerformDetection + ExecuteAttack
/// </summary>
public abstract class BaseCombatStrategy : MonoBehaviour, ICombatStrategy
{
    // ==================== 攻击间隔配置 ====================
    [Header("=== 攻击动画时长（攻速×1.0 时） ===")]
    [Tooltip("前摇时长（秒），伤害发生前的准备时间，前摇期间目标丢失则取消攻击")]
    [SerializeField] protected float baseWindup = 0.15f;

    [Tooltip("后摇时长（秒），伤害后的收招时间，不可中断。暂为 0 方便调试攻击间隔")]
    [SerializeField] protected float baseRecovery = 0f;

    // ==================== 索敌配置 ====================
    [Header("=== 索敌 ===")]
    [Tooltip("索敌扫描间隔（秒），越大性能越好但反应越慢")]
    [SerializeField] protected float detectInterval = 0.2f;

    // ==================== 运行时状态 ====================
    private float _attackInterval = 1f;               // 缓存自 UnitAttr.atkCD

    // ==================== 攻速调试 ====================
    [Header("=== 攻速调试 ===")]
    [Tooltip("攻速系数：1.0=正常速度，2.0=双倍速。修改即生效")]
    [SerializeField] protected float attackSpeedMul = 1f;

    [Tooltip("当前有效间隔 = atkCD / 攻速系数（只读）")]
    [SerializeField] protected float effectiveIntervalDebug;
    protected AttackPhase phase = AttackPhase.Idle;
    protected float phaseTimer;
    protected float detectTimer;
    protected Transform cachedTarget;
    protected float lastDetectTime = -999f;

    /// <summary>攻击阶段枚举</summary>
    protected enum AttackPhase
    {
        Idle,       // 就绪，可开始新攻击
        Windup,     // 前摇，可被中断
        Recovery,   // 后摇，不可中断
        IdleWait    // 空闲等待，填充剩余间隔
    }

    // ==================== 动态属性（攻速缩放） ====================
    protected float EffectiveInterval => _attackInterval / attackSpeedMul;
    protected float WindupDuration    => baseWindup    / attackSpeedMul;
    protected float RecoveryDuration  => baseRecovery  / attackSpeedMul;
    protected float IdleWaitDuration  => Mathf.Max(0f, EffectiveInterval - WindupDuration - RecoveryDuration);

    // ==================== 接口属性 ====================
    public Transform CurrentTarget { get; protected set; }

    // ==================== Unity 生命周期 ====================

    protected virtual void Awake()
    {
        var selfAttr = GetComponent<UnitAttr>();
        if (selfAttr != null)
            _attackInterval = selfAttr.atkCD;
    }

    protected virtual void Update()
    {
        // 同步 Inspector 调试显示
        effectiveIntervalDebug = EffectiveInterval;

        // 推进攻击阶段机
        if (phase != AttackPhase.Idle)
            UpdateAttackPhase();
    }

    // ==================== 攻击阶段机 ====================

    protected virtual void UpdateAttackPhase()
    {
        phaseTimer -= Time.deltaTime;

        switch (phase)
        {
            case AttackPhase.Windup:
                if (!IsTargetValid(CurrentTarget))
                {
                    CancelAttack();
                    return;
                }
                if (phaseTimer <= 0)
                {
                    ExecuteAttack(CurrentTarget);           // ★ 伤害点
                    phase = AttackPhase.Recovery;
                    phaseTimer = RecoveryDuration;
                }
                break;

            case AttackPhase.Recovery:
                if (phaseTimer <= 0)
                {
                    if (IdleWaitDuration > 0)
                    {
                        phase = AttackPhase.IdleWait;
                        phaseTimer = IdleWaitDuration;
                    }
                    else
                    {
                        phase = AttackPhase.Idle;
                    }
                }
                break;

            case AttackPhase.IdleWait:
                if (phaseTimer <= 0)
                    phase = AttackPhase.Idle;
                break;
        }
    }

    /// <summary>
    /// 子类实现：前摇完成后执行的攻击行为
    /// 近战→DealDamage  /  远程→SpawnBullet  /  治疗→HealTarget
    /// </summary>
    protected abstract void ExecuteAttack(Transform target);

    /// <summary>
    /// 取消当前攻击（仅前摇阶段可中断）
    /// </summary>
    public virtual void CancelAttack()
    {
        if (phase == AttackPhase.Windup)
        {
            phase = AttackPhase.Idle;
            phaseTimer = 0f;
        }
    }

    // ==================== 索敌（模板方法） ====================

    /// <summary>
    /// 索敌入口。带缓存：在 detectInterval 内直接返回上次结果（轻量验证）
    /// </summary>
    public virtual Transform DetectTarget(UnitAttr attr, Vector2 position)
    {
        if (Time.time - lastDetectTime < detectInterval && IsTargetValid(cachedTarget))
            return cachedTarget;

        lastDetectTime = Time.time;
        cachedTarget = PerformDetection(attr, position);
        CurrentTarget = cachedTarget;
        return CurrentTarget;
    }

    /// <summary>
    /// 子类实现实际的索敌逻辑
    /// </summary>
    protected abstract Transform PerformDetection(UnitAttr attr, Vector2 position);

    /// <summary>
    /// 轻量验证目标是否仍有效（单位或防御塔）
    /// </summary>
    protected bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        // 检查是否为敌方单位
        UnitAttr ta = target.GetComponent<UnitAttr>();
        if (ta != null) return ta.currentHp > 0;

        // 检查是否为防御塔
        TowerBase tower = target.GetComponent<TowerBase>();
        if (tower != null) return tower.hp > 0;

        // 目标既不是单位也不是塔 → 无效
        return false;
    }

    // ==================== TryExecute（模板方法） ====================

    /// <summary>
    /// 战斗执行。间隔未就绪则跳过，距离不够则追击
    /// </summary>
    public virtual bool TryExecute(Transform target, float deltaTime, UnitAttr attr,
                                   Vector2 position, IMoveStrategy movement)
    {
        if (target == null) return false;
        if (phase != AttackPhase.Idle) return false;

        float distance = Vector2.Distance(position, target.position);

        if (distance > attr.atkRange)
        {
            movement.MoveToward(target.position, attr.moveSpeed);
            return true;
        }

        phase = AttackPhase.Windup;
        phaseTimer = WindupDuration;
        return true;
    }

    // ==================== 通用方法（消除重复） ====================

    public virtual void TakeDamage(float damage, AttackType type, UnitAttr attr, Action onDie)
    {
        float finalDmg = damage;

        if (type == AttackType.Physical)
            finalDmg = Mathf.Max(1f, damage - attr.physicalDefense);
        else if (type == AttackType.Magic)
            finalDmg = Mathf.Max(1f, damage - attr.magicDefense);

        attr.currentHp -= finalDmg;

        if (attr.currentHp <= 0)
            onDie?.Invoke();
    }

    public virtual void SetTowerTarget(UnitAttr attr)
    {
        if (attr.camp == CampType.Player)
        {
            CurrentTarget = BattleManager.Instance.enemyTower != null
                ? BattleManager.Instance.enemyTower.transform
                : null;
        }
        else
        {
            CurrentTarget = BattleManager.Instance.playerTower != null
                ? BattleManager.Instance.playerTower.transform
                : null;
        }
    }

    /// <summary>
    /// 统一伤害结算：对单位 → UnitBrain.TakeDamage，对塔 → TowerBase.TakeDamage
    /// </summary>
    protected void DealDamage(Transform target, float damage, AttackType attackType)
    {
        if (target.TryGetComponent<UnitBrain>(out var unitBrain))
        {
            unitBrain.TakeDamage(damage, attackType);
            GetComponent<UnitBrain>()?.NotifyAttackHit();
            return;
        }
        if (target.TryGetComponent<TowerBase>(out var tower))
        {
            tower.TakeDamage(damage, attackType);
            GetComponent<UnitBrain>()?.NotifyAttackHit();
            return;
        }
        Debug.LogWarning(
            $"BaseCombatStrategy: {gameObject.name} 攻击 {target.name}，" +
            "目标缺少 UnitBrain 和 TowerBase 组件，伤害无法结算！", target);
    }

    // ==================== 攻速 buff 接口 ====================

    public void AddSpeedBonus(float multiplier) => attackSpeedMul *= multiplier;
    public void RemoveSpeedBonus(float multiplier) => attackSpeedMul /= multiplier;
    public void ResetAttackSpeed() => attackSpeedMul = 1f;

    // ==================== 编辑器范围可视化 ====================

    private void OnDrawGizmos()
    {
        var attr = GetComponent<UnitAttr>();
        if (attr == null) return;

        // 攻击范围 — 红色
        Gizmos.color = new Color(1f, 0.25f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, attr.atkRange);

        // 索敌范围 — 蓝色
        Gizmos.color = new Color(0.15f, 0.45f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, attr.detectRange);
    }
}
