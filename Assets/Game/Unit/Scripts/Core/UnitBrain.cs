using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 单位大脑 —— 纯状态机调度器
/// 职责：只做行为调度和状态切换，不包含任何具体的移动/战斗逻辑
/// 通过 GetComponent&lt;IMoveStrategy&gt;() 和 GetComponent&lt;ICombatStrategy&gt;() 注入策略
///
/// 行为优先级：
///   1. 死亡判定
///   2. 按状态执行：Moving / Advancing / Fighting / Garrisoned / AttackingTower
/// </summary>
[RequireComponent(typeof(UnitAttr))]
[RequireComponent(typeof(UnitUI))]
public class UnitBrain : MonoBehaviour
{
    [Header("=== 调试（只读） ===")]
    [Tooltip("当前状态。运行时查看，请勿手动修改")]
    [SerializeField] private UnitState _state = UnitState.Moving;

    public UnitState state
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            UnitState old = _state;
            _state = value;
            OnStateChanged?.Invoke(old, _state);
        }
    }

    private UnitAttr attr;
    private UnitUI ui;
    private IMoveStrategy moveStrategy;
    private ICombatStrategy combatStrategy;

    // ==================== 事件系统（供音效/特效/UI 挂载） ====================
    public event Action<UnitState, UnitState> OnStateChanged;  // (旧状态, 新状态)
    public event Action<UnitState> OnStateEnter;                // 进入新状态
    public event Action<UnitState> OnStateExit;                 // 离开旧状态
    public event Action<GameObject> OnDeath;                    // 死亡前触发
    public event Action<float, AttackType> OnDamageTaken;       // 受到伤害
    public event Action OnAttackHit;                            // 攻击命中
    public event Action OnMoveStart;                            // 开始移动
    public event Action OnPathComplete;                         // 路径走完

    // ==================== Unity 生命周期 ====================

    void Awake()
    {
        attr = GetComponent<UnitAttr>();
        ui = GetComponent<UnitUI>();
        moveStrategy = GetComponent<IMoveStrategy>();
        combatStrategy = GetComponent<ICombatStrategy>();

        if (moveStrategy == null)
            Debug.LogError($"UnitBrain: {gameObject.name} 缺少 IMoveStrategy 组件！", this);
        if (combatStrategy == null)
            Debug.LogError($"UnitBrain: {gameObject.name} 缺少 ICombatStrategy 组件！", this);

        attr.currentHp = attr.ModifiedMaxHp;
    }

    void Update()
    {
        // 终端状态不执行任何逻辑
        if (_state == UnitState.Dead) return;

        // 死亡判定
        if (attr.currentHp <= 0)
        {
            Die();
            return;
        }

        // ====== 状态机主循环 ======
        if (combatStrategy == null || moveStrategy == null) return;

        switch (_state)
        {
            case UnitState.Moving:
                UpdateMoving();
                break;

            case UnitState.Advancing:
                UpdateAdvancing();
                break;

            case UnitState.Fighting:
                UpdateFighting();
                break;

            case UnitState.Garrisoned:
                UpdateGarrisoned();
                break;

            case UnitState.AttackingTower:
                UpdateAttackingTower();
                break;
        }
    }

    // ==================== 统一状态切换 ====================

    /// <summary>
    /// 统一状态切换入口。检查合法性，触发进入/退出事件。
    /// </summary>
    void ChangeState(UnitState newState)
    {
        if (_state == UnitState.Dead) return;    // 终端状态不可切出
        if (_state == newState) return;

        OnStateExit?.Invoke(_state);
        state = newState;
        OnStateEnter?.Invoke(_state);
    }

    // ==================== 状态更新方法 ====================

    /// <summary>
    /// Moving 状态：沿路径行走，检测敌人/驻扎点
    /// </summary>
    void UpdateMoving()
    {
        // ★ 传送中跳过一切索敌/驻扎检测（单位处于无敌/不可交互状态）
        if (moveStrategy.IsTeleporting)
        {
            moveStrategy.Move(Time.deltaTime, attr.ModifiedMoveSpeed);
            return;
        }

        // 优先检测敌人
        Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
        if (enemy != null)
        {
            ChangeState(UnitState.Fighting);
            return;
        }

        // 检测驻扎点/资源点
        if (TryGarrisonInteraction()) return;

        // 路径未走完 → 继续移动
        if (!moveStrategy.IsPathCompleted())
        {
            moveStrategy.Move(Time.deltaTime, attr.ModifiedMoveSpeed);
            return;
        }

        // 路径已走完 → 进入 Advancing（向塔推进）
        OnPathComplete?.Invoke();
        ChangeState(UnitState.Advancing);
    }

    /// <summary>
    /// Advancing 状态：路径已走完，向敌方塔推进
    /// 优先级：敌人 > 塔
    /// </summary>
    void UpdateAdvancing()
    {
        // 1. 优先检测敌人
        Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
        if (enemy != null)
        {
            ChangeState(UnitState.Fighting);
            return;
        }

        // 2. 塔在攻击范围内 → 攻击塔
        if (TryTargetTower()) return;

        // 3. 塔存在但太远 → 向塔推进
        GameObject tower = attr.camp == CampType.Player
            ? BattleManager.Instance.enemyTower
            : BattleManager.Instance.playerTower;

        if (tower != null)
        {
            moveStrategy.MoveToward(tower.transform.position, attr.ModifiedMoveSpeed);
            return;
        }

        // 4. 塔不存在 → 待机
    }

    /// <summary>
    /// Fighting 状态：与敌方单位交战
    /// </summary>
    void UpdateFighting()
    {
        Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
        if (enemy == null)
        {
            // 敌人消失 → 路径未完则继续走，路径已完则继续推进
            ChangeState(moveStrategy.IsPathCompleted()
                ? UnitState.Advancing
                : UnitState.Moving);
            return;
        }

        combatStrategy.TryExecute(enemy, Time.deltaTime, attr, transform.position, moveStrategy);
    }

    /// <summary>
    /// Garrisoned 状态：驻扎中检测敌人
    /// </summary>
    void UpdateGarrisoned()
    {
        if (combatStrategy != null)
        {
            Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
            if (enemy != null)
                LeaveGarrison();
        }
    }

    /// <summary>
    /// AttackingTower 状态：攻击敌方防御塔
    /// </summary>
    void UpdateAttackingTower()
    {
        if (combatStrategy.CurrentTarget == null)
        {
            ChangeState(UnitState.Advancing);
            return;
        }

        float dist = Vector2.Distance(transform.position, combatStrategy.CurrentTarget.position);
        if (dist > attr.atkRange)
        {
            // 塔超出范围（保险）→ 回到推进
            ChangeState(UnitState.Advancing);
            return;
        }

        combatStrategy.TryExecute(combatStrategy.CurrentTarget,
            Time.deltaTime, attr, transform.position, moveStrategy);
    }

    // ==================== 塔检测 ====================

    /// <summary>
    /// 检测敌方塔是否在攻击范围内，在则锁定并切入 AttackingTower
    /// </summary>
    bool TryTargetTower()
    {
        GameObject tower = attr.camp == CampType.Player
            ? BattleManager.Instance.enemyTower
            : BattleManager.Instance.playerTower;

        if (tower == null) return false;

        float dist = Vector2.Distance(transform.position, tower.transform.position);
        if (dist <= attr.atkRange)
        {
            combatStrategy.SetTowerTarget(attr);
            if (combatStrategy.CurrentTarget != null)
            {
                ChangeState(UnitState.AttackingTower);
                return true;
            }
        }

        return false;
    }

    // ==================== 驻扎交互 ====================

    bool TryGarrisonInteraction()
    {
        // 驻扎点检测
        if (GarrisonPointManager.Instance != null)
        {
            GarrisonPoint gp = GarrisonPointManager.Instance.GetGarrisonPointInRange(
                transform.position, attr.camp);
            if (gp != null)
            {
                if (gp.CanGarrison(gameObject))
                {
                    gp.AddGarrison(gameObject);
                    return true;
                }
                if (gp.occupyingCamp != null && gp.occupyingCamp != attr.camp && !gp.isContested)
                {
                    gp.StartContest(gameObject);
                    return true;
                }
                return false;
            }
        }

        // 资源点检测
        if (ResourcePointManager.Instance != null)
        {
            ResourcePoint rp = ResourcePointManager.Instance.GetResourcePointInRange(transform.position);
            if (rp != null)
            {
                if (rp.CanGarrison(gameObject))
                {
                    rp.AddGarrison(gameObject);
                    return true;
                }
                if (rp.occupyingCamp != null && rp.occupyingCamp != attr.camp && !rp.isContested)
                {
                    rp.StartContest(gameObject);
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 脱离驻扎（发现敌人时自动调用）
    /// </summary>
    void LeaveGarrison()
    {
        if (!attr.isGarrisoned) return;

        if (attr.garrisonedAt != null)
            attr.garrisonedAt.RemoveGarrison(gameObject);

        moveStrategy?.Resume();

        // 先检测敌人是否存在，再决定目标状态
        Transform enemy = combatStrategy?.DetectTarget(attr, transform.position);
        ChangeState(enemy != null ? UnitState.Fighting : UnitState.Advancing);
    }

    // ==================== 外部公共 API ====================

    /// <summary>
    /// 受到伤害（供 TowerBase / Bullet / 其他单位调用）
    /// </summary>
    public void TakeDamage(float damage, AttackType type)
    {
        if (combatStrategy == null) return;

        // 传送中完全无敌：忽略一切伤害（含已锁定攻击和飞行中的子弹）
        if (moveStrategy != null && moveStrategy.IsTeleporting) return;

        combatStrategy.TakeDamage(damage, type, attr, Die);
        ui.RefreshHp(attr.currentHp);
        OnDamageTaken?.Invoke(damage, type);
    }

    /// <summary>
    /// 设置移动路径（供 WaveGenerator / CardDeploy 调用）
    /// </summary>
    public void SetPath(PathManager path)
    {
        moveStrategy?.SetPath(path);
    }

    /// <summary>
    /// 设置移动路径并定位到最近点（供 InitialUnitPlacer 调用）
    /// </summary>
    public void SetPathFromPosition(PathManager path, Vector3 worldPosition)
    {
        moveStrategy?.SetPathAtClosestPoint(path, worldPosition);
    }

    /// <summary>
    /// 暂停移动（供 ResourcePoint / GarrisonPoint 调用）
    /// </summary>
    public void StopMovement()
    {
        moveStrategy?.Stop();
    }

    /// <summary>
    /// 恢复移动（供 ResourcePoint / GarrisonPoint 调用）
    /// </summary>
    public void ResumeMovement()
    {
        moveStrategy?.Resume();
    }

    /// <summary>由 CombatStrategy 在 DealDamage 命中时调用</summary>
    public void NotifyAttackHit()
    {
        OnAttackHit?.Invoke();
    }

    /// <summary>
    /// 当前是否处于传送等待中（传送中单位完全无敌，Boss 关牵制检测应跳过）
    /// </summary>
    public bool IsTeleporting()
    {
        return moveStrategy != null && moveStrategy.IsTeleporting;
    }

    // ==================== 死亡序列 ====================

    /// <summary>
    /// 执行死亡流程：切到 Dead 状态 → 触发 OnDeath → 播放淡出动画 → 销毁。
    /// 公开供外部系统（如 Boss 关 TowerLeashZone 牵制死亡）直接触发。
    /// </summary>
    public void Die()
    {
        if (_state == UnitState.Dead) return;
        ChangeState(UnitState.Dead);

        OnDeath?.Invoke(gameObject);

        // 传送中死亡时恢复渲染，保证死亡淡出动画可见
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        ui.DestroyHpBar();

        var sr = GetComponent<SpriteRenderer>();
        if (sr)
        {
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                sr.color = new Color(1, 1, 1, 1 - t / 0.3f);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
