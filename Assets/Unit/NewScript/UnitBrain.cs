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
///   2. 驻扎中检测敌人 → 脱离驻扎进入战斗
///   3. 按状态执行：Moving / Fighting / Garrisoned / AttackingTower
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

        // ====== 驻扎中：检测敌人 → 立刻离开驻扎投入战斗 ======
        if (_state == UnitState.Garrisoned)
        {
            if (combatStrategy != null)
            {
                Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
                if (enemy != null)
                    LeaveGarrison();
            }
            return;
        }

        // ====== 状态机主循环 ======
        if (combatStrategy == null || moveStrategy == null) return;

        switch (_state)
        {
            case UnitState.Moving:
                UpdateMoving();
                break;

            case UnitState.Fighting:
                UpdateFighting();
                break;

            case UnitState.AttackingTower:
                UpdateAttackingTower();
                break;

            case UnitState.Garrisoned:
                break;
        }
    }

    // ==================== 状态更新方法 ====================

    /// <summary>
    /// Moving 状态：沿路径行走，检测敌人/驻扎点/塔
    /// </summary>
    void UpdateMoving()
    {
        // 优先检测敌人
        Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
        if (enemy != null)
        {
            state = UnitState.Fighting;
            return;
        }

        // 检测驻扎点/资源点
        if (TryGarrisonInteraction()) return;

        // 检测敌方塔是否在攻击范围内
        if (TryTargetTower()) return;

        // 路径未走完 → 继续移动
        if (!moveStrategy.IsPathCompleted())
        {
            moveStrategy.Move(Time.deltaTime, attr.moveSpeed);
            return;
        }

        // 路径已走完 → 以敌方塔为目标点直接走过去
        if (TryWalkToTower()) return;

        // 路径已走完 → 触发事件，停在终点
        OnPathComplete?.Invoke();
    }

    /// <summary>
    /// Fighting 状态：与敌方单位交战
    /// </summary>
    void UpdateFighting()
    {
        Transform enemy = combatStrategy.DetectTarget(attr, transform.position);
        if (enemy == null)
        {
            state = UnitState.Moving;
            return;
        }

        combatStrategy.TryExecute(enemy, Time.deltaTime, attr, transform.position, moveStrategy);
    }

    /// <summary>
    /// AttackingTower 状态：攻击敌方防御塔
    /// 塔被毁或超出攻击范围且路径未走完 → 回到 Moving
    /// </summary>
    void UpdateAttackingTower()
    {
        if (combatStrategy.CurrentTarget == null)
        {
            state = UnitState.Moving;
            return;
        }

        float dist = Vector2.Distance(transform.position, combatStrategy.CurrentTarget.position);
        if (dist > attr.atkRange)
        {
            state = UnitState.Moving;
            return;
        }

        combatStrategy.TryExecute(combatStrategy.CurrentTarget,
            Time.deltaTime, attr, transform.position, moveStrategy);
    }

    // ==================== 塔检测 ====================

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
                state = UnitState.AttackingTower;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 路径走完后，直接以塔为目标点行走, 是目前的冗余保险
    /// </summary>
    bool TryWalkToTower()
    {
        GameObject tower = attr.camp == CampType.Player
            ? BattleManager.Instance.enemyTower
            : BattleManager.Instance.playerTower;

        if (tower == null) return false;

        transform.position = Vector2.MoveTowards(
            transform.position,
            tower.transform.position,
            attr.moveSpeed * Time.deltaTime);
        return true;
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
    /// 先检测敌人是否存在再决定跳转状态，避免 Fighting→Moving 无意义切换
    /// </summary>
    void LeaveGarrison()
    {
        if (!attr.isGarrisoned) return;

        if (attr.garrisonedAt != null)
            attr.garrisonedAt.RemoveGarrison(gameObject);

        moveStrategy?.Resume();

        // 先检测敌人是否存在，再决定状态
        Transform enemy = combatStrategy?.DetectTarget(attr, transform.position);
        state = enemy != null ? UnitState.Fighting : UnitState.Moving;
    }

    // ==================== 事件触发方法 ====================

    /// <summary>由 CombatStrategy 在 DealDamage 命中时调用</summary>
    public void NotifyAttackHit()
    {
        OnAttackHit?.Invoke();
    }

    // ==================== 外部公共 API ====================

    /// <summary>
    /// 受到伤害（供 TowerBase / Bullet / 其他单位调用）
    /// 委托给 CombatStrategy 处理防御减伤和扣血
    /// </summary>
    public void TakeDamage(float damage, AttackType type)
    {
        if (combatStrategy == null) return;
        combatStrategy.TakeDamage(damage, type, attr, Die);
        ui.RefreshHp(attr.currentHp);

        // 受击反馈
        OnDamageTaken?.Invoke(damage, type);
        GetComponent<UnitVisual>()?.FlashRed();

        // 攻击者事件（当此单位被作为"攻击命中"时由 DealDamage 侧触发 OnAttackHit）
    }

    /// <summary>
    /// 设置移动路径（供 WaveGenerator / CardDeploy 调用）
    /// </summary>
    public void SetPath(PathManager path)
    {
        moveStrategy?.SetPath(path);
    }

    /// <summary>
    /// 设置移动路径，并将单位定位到路径上距 worldPosition 最近的点
    /// （供 InitialUnitPlacer 等初始布阵系统调用）
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

    // ==================== 死亡序列 ====================

    void Die()
    {
        if (_state == UnitState.Dead) return;
        state = UnitState.Dead;

        // 事件通知（音效、任务系统等监听）
        OnDeath?.Invoke(gameObject);

        // 立即禁用碰撞（停止物理交互）
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        // 启动死亡序列协程
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        ui.DestroyHpBar();

        // 0.3s 淡出
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
