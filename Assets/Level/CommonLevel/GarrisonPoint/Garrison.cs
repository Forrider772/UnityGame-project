using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 驻扎核心组件 — 可复用的驻扎/占领/争夺逻辑。
/// 挂载到任意 GameObject 上即可赋予驻扎交互能力。
///
/// 使用方：ResourcePoint（场景放置）、GarrisonPoint（动态创建）
///         各自在 Start 中注册到对应的 Manager
///
/// 设计原则：
///   - 使用 transform.position 作为范围检测中心
///   - 不包含 effectiveCamps 过滤（由 GarrisonPoint 层处理）
///   - 驻扎单位通过 UnitAttr.garrisonedAt 反向引用
/// </summary>
public class Garrison : MonoBehaviour
{
    [Header("=== 驻扎参数 ===")]
    [Tooltip("驻扎交互的圆形范围半径")]
    public float garrisonRange = 1.5f;

    [Tooltip("最大同时驻扎单位数量")]
    public int maxGarrison = 3;

    [Header("=== 运行时状态 ===")]
    [SerializeField] private CampType? _occupyingCamp;
    [SerializeField] private List<GameObject> _garrisonedUnits = new List<GameObject>();
    [SerializeField] private bool _isContested;

    // ==================== 公开属性 ====================

    public CampType? occupyingCamp => _occupyingCamp;
    public int garrisonedCount => _garrisonedUnits.Count;
    public bool isContested => _isContested;
    /// <summary>是否处于活跃占领状态（有阵营占领且未在争夺中）</summary>
    public bool isActive => _occupyingCamp != null && !_isContested;

    private RangeCircleDisplay _rangeCircle;

    // ==================== 生命周期 ====================

    void Awake()
    {
        // 自动添加范围圈显示组件
        _rangeCircle = GetComponent<RangeCircleDisplay>();
        if (_rangeCircle == null)
            _rangeCircle = gameObject.AddComponent<RangeCircleDisplay>();
    }

    void OnDestroy()
    {
        // 清理所有驻扎单位的引用，避免悬挂指针
        foreach (var unit in _garrisonedUnits)
        {
            if (unit == null) continue;
            UnitAttr attr = unit.GetComponent<UnitAttr>();
            if (attr != null)
            {
                attr.isGarrisoned = false;
                attr.garrisonedAt = null;
            }
            UnitBrain brain = unit.GetComponent<UnitBrain>();
            if (brain != null)
                brain.ResumeMovement();
        }
        _garrisonedUnits.Clear();
    }

    void Update()
    {
        CleanDeadUnits();
        if (_isContested)
            ResolveContest();

        // 同步范围圈
        _rangeCircle?.UpdateDisplay(garrisonRange, _occupyingCamp, _isContested);
    }

    // ==================== 范围检测 ====================

    /// <summary>
    /// 检查单位是否在驻扎范围内（使用 transform.position）
    /// </summary>
    public bool IsUnitInRange(GameObject unit)
    {
        return Vector2.Distance(transform.position, unit.transform.position) <= garrisonRange;
    }

    /// <summary>
    /// 判定单位是否可以驻扎。
    /// 不含 effectiveCamps 检查（由 GarrisonPoint 层处理）。
    /// </summary>
    public bool CanGarrison(GameObject unit)
    {
        UnitAttr attr = unit.GetComponent<UnitAttr>();
        if (attr == null) return false;

        if (!IsUnitInRange(unit)) return false;
        if (_isContested) return false;

        // 无人占领 → 可驻扎
        if (_occupyingCamp == null) return true;

        // 同阵营且未满员 → 可驻扎
        if (_occupyingCamp == attr.camp && _garrisonedUnits.Count < maxGarrison)
            return true;

        // 同阵营满员 / 敌方占领 → 不可驻扎
        return false;
    }

    // ==================== 驻扎管理 ====================

    /// <summary>
    /// 单位加入驻扎
    /// </summary>
    public void AddGarrison(GameObject unit)
    {
        if (_garrisonedUnits.Contains(unit)) return;

        UnitAttr attr = unit.GetComponent<UnitAttr>();
        if (attr == null) return;

        _garrisonedUnits.Add(unit);
        attr.isGarrisoned = true;
        attr.garrisonedAt = this;

        UnitBrain brain = unit.GetComponent<UnitBrain>();
        if (brain != null)
            brain.StopMovement();

        if (_occupyingCamp == null)
            _occupyingCamp = attr.camp;
    }

    /// <summary>
    /// 单位离开驻扎（死亡/出战/回收时调用）
    /// </summary>
    public void RemoveGarrison(GameObject unit)
    {
        _garrisonedUnits.Remove(unit);

        UnitAttr attr = unit.GetComponent<UnitAttr>();
        if (attr != null)
        {
            attr.isGarrisoned = false;
            attr.garrisonedAt = null;
        }

        if (_garrisonedUnits.Count == 0 && !_isContested)
            _occupyingCamp = null;
    }

    // ==================== 争夺战斗 ====================

    /// <summary>
    /// 触发抢占战斗：唤醒所有守方单位投入战斗
    /// </summary>
    public void StartContest(GameObject attacker)
    {
        if (!_isContested)
        {
            _isContested = true;
        }

        // 唤醒所有守方单位
        foreach (var defender in _garrisonedUnits)
        {
            if (defender == null) continue;
            UnitAttr attr = defender.GetComponent<UnitAttr>();
            if (attr != null)
            {
                attr.isGarrisoned = false;
                attr.garrisonedAt = null;
            }
            UnitBrain brain = defender.GetComponent<UnitBrain>();
            if (brain != null)
                brain.ResumeMovement();
        }
    }

    /// <summary>
    /// 每帧检查争夺胜负
    /// </summary>
    void ResolveContest()
    {
        CleanDeadUnits();

        bool anyDefenderAlive = _garrisonedUnits.Exists(u => u != null);
        bool anyAttackerAlive = CheckAttackersAlive();

        if (!anyDefenderAlive && anyAttackerAlive)
        {
            // 守方全灭 → 攻方胜，攻方存活单位自动驻扎
            _isContested = false;
            AutoGarrisonWinners();
        }
        else if (anyDefenderAlive && !anyAttackerAlive)
        {
            // 攻方全灭 → 守方胜，重新驻扎
            _isContested = false;
            ReGarrisonSurvivors();
        }
        else if (!anyDefenderAlive && !anyAttackerAlive)
        {
            // 同归于尽 → 无人占领
            _isContested = false;
            _occupyingCamp = null;
            _garrisonedUnits.Clear();
        }
    }

    /// <summary>
    /// 攻方获胜时，存活攻方单位自动驻扎（最多 maxGarrison 人）
    /// </summary>
    void AutoGarrisonWinners()
    {
        _garrisonedUnits.Clear();

        CampType? winnerCamp = GetWinnerCampFromUnitsInRange();
        if (winnerCamp == null)
        {
            _occupyingCamp = null;
            return;
        }

        _occupyingCamp = winnerCamp;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, garrisonRange);
        int added = 0;
        foreach (var hit in hits)
        {
            if (added >= maxGarrison) break;
            UnitAttr attr = hit.GetComponent<UnitAttr>();
            if (attr == null || attr.camp != winnerCamp || attr.currentHp <= 0) continue;
            if (attr.isGarrisoned) continue;

            _garrisonedUnits.Add(hit.gameObject);
            attr.isGarrisoned = true;
            attr.garrisonedAt = this;

            UnitBrain brain = hit.GetComponent<UnitBrain>();
            if (brain != null)
                brain.StopMovement();
            added++;
        }
    }

    /// <summary>
    /// 守方获胜时，守方存活单位重新驻扎
    /// </summary>
    void ReGarrisonSurvivors()
    {
        int added = 0;
        foreach (var unit in _garrisonedUnits)
        {
            if (unit == null) continue;
            if (added >= maxGarrison) break;

            UnitAttr attr = unit.GetComponent<UnitAttr>();
            if (attr == null || attr.currentHp <= 0 || attr.isGarrisoned) continue;

            attr.isGarrisoned = true;
            attr.garrisonedAt = this;

            UnitBrain brain = unit.GetComponent<UnitBrain>();
            if (brain != null)
                brain.StopMovement();
            added++;
        }

        if (_garrisonedUnits.Count == 0 || !_garrisonedUnits.Exists(u => u != null))
            _occupyingCamp = null;
    }

    /// <summary>
    /// 检查攻方是否还有存活单位在范围内
    /// </summary>
    bool CheckAttackersAlive()
    {
        if (_occupyingCamp == null) return false;
        CampType attackerCamp = _occupyingCamp == CampType.Player ? CampType.Enemy : CampType.Player;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, garrisonRange);
        foreach (var hit in hits)
        {
            UnitAttr attr = hit.GetComponent<UnitAttr>();
            if (attr != null && attr.camp == attackerCamp && attr.currentHp > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 根据范围内存活单位推断胜方阵营
    /// </summary>
    CampType? GetWinnerCampFromUnitsInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, garrisonRange);
        foreach (var hit in hits)
        {
            UnitAttr attr = hit.GetComponent<UnitAttr>();
            if (attr != null && attr.currentHp > 0)
                return attr.camp;
        }
        return null;
    }

    /// <summary>
    /// 清理列表中已销毁的单位引用
    /// </summary>
    void CleanDeadUnits()
    {
        _garrisonedUnits.RemoveAll(u => u == null);
    }

    // ==================== 便捷查询 ====================

    public bool IsFull()
    {
        return _garrisonedUnits.Count >= maxGarrison;
    }

    public bool IsGarrisonedUnit(GameObject unit)
    {
        return _garrisonedUnits.Contains(unit);
    }

    // ==================== 编辑器可视化 ====================

    void OnDrawGizmos()
    {
        Gizmos.color = _occupyingCamp == null ? Color.gray
            : _occupyingCamp == CampType.Player ? Color.green : Color.red;
        if (_isContested) Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, garrisonRange);
    }
}
