using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 驻扎点组件，挂载在动态实例化的驻扎点 GameObject 上。
/// 管理驻扎/争夺状态，支持自由配置对任意阵营生效。
/// 与 ResourcePoint 并行但支持运行时动态放置和灵活的阵营过滤。
/// </summary>
public class GarrisonPoint : MonoBehaviour
{
    [Header("生效阵营")]
    [Tooltip("对哪些阵营的单位生效（可多选）")]
    public List<CampType> effectiveCamps = new List<CampType>();

    [Header("驻扎参数")]
    public float garrisonRange = 1.5f;
    public int maxGarrison = 3;

    [Header("运行时状态")]
    [SerializeField] private CampType? _occupyingCamp;
    [SerializeField] private List<GameObject> _garrisonedUnits = new List<GameObject>();
    [SerializeField] private bool _isContested;

    [HideInInspector]
    public Vector2 worldPosition;

    [HideInInspector]
    public PathManager boundPath;

    [HideInInspector]
    public int boundSegmentIndex;

    [HideInInspector]
    public float boundSegmentT;

    public CampType? occupyingCamp => _occupyingCamp;
    public int garrisonedCount => _garrisonedUnits.Count;
    public bool isContested => _isContested;
    public bool isActive => _occupyingCamp != null && !_isContested;

    void Start()
    {
        if (GarrisonPointManager.Instance != null)
            GarrisonPointManager.Instance.Register(this);

        // 同步 GameObject 位置
        transform.position = worldPosition;
    }

    void Update()
    {
        CleanDeadUnits();
        if (_isContested)
            ResolveContest();

        // 验证绑定的路径仍然有效
        if (boundPath == null || boundPath.pathPoints == null
            || boundSegmentIndex >= boundPath.pathPoints.Count - 1)
        {
            // 路径已失效，销毁自身
            if (GarrisonPointManager.Instance != null)
                GarrisonPointManager.Instance.Unregister(this);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // 清理所有驻扎单位的引用
        foreach (var unit in _garrisonedUnits)
        {
            if (unit == null) continue;
            UnitAttr attr = unit.GetComponent<UnitAttr>();
            if (attr != null)
            {
                attr.isGarrisoned = false;
                attr.garrisonedGarrisonPoint = null;
            }
            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
                movement.ResumeMovement();
        }
        _garrisonedUnits.Clear();
    }

    /// <summary>
    /// 检查该驻扎点对指定阵营是否生效
    /// </summary>
    public bool IsEffectiveFor(CampType camp)
    {
        return effectiveCamps.Contains(camp);
    }

    /// <summary>
    /// 检查单位是否在驻扎范围内
    /// </summary>
    public bool IsUnitInRange(GameObject unit)
    {
        return Vector2.Distance(worldPosition, unit.transform.position) <= garrisonRange;
    }

    /// <summary>
    /// 判定单位是否可以驻扎此驻扎点
    /// </summary>
    public bool CanGarrison(GameObject unit)
    {
        UnitAttr attr = unit.GetComponent<UnitAttr>();
        if (attr == null) return false;

        if (!IsUnitInRange(unit)) return false;
        if (_isContested) return false;

        // 检查生效阵营
        if (!IsEffectiveFor(attr.camp)) return false;

        // 情况A: 无人占领 → 可驻扎
        if (_occupyingCamp == null) return true;

        // 情况B: 同阵营且未满员 → 可驻扎
        if (_occupyingCamp == attr.camp && _garrisonedUnits.Count < maxGarrison)
            return true;

        // 情况B-2: 同阵营满员 → 不可驻扎
        // 情况C: 敌方占领 → 不可驻扎（由 UnitAI 触发抢占战斗）
        return false;
    }

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
        attr.garrisonedGarrisonPoint = this;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null)
            movement.StopMovement();

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
            attr.garrisonedGarrisonPoint = null;
        }

        if (_garrisonedUnits.Count == 0 && !_isContested)
            _occupyingCamp = null;
    }

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
                attr.garrisonedGarrisonPoint = null;
            }
            UnitMovement movement = defender.GetComponent<UnitMovement>();
            if (movement != null)
                movement.ResumeMovement();
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, garrisonRange);
        int added = 0;
        foreach (var hit in hits)
        {
            if (added >= maxGarrison) break;
            UnitAttr attr = hit.GetComponent<UnitAttr>();
            if (attr == null || attr.camp != winnerCamp || attr.currentHp <= 0) continue;
            if (attr.isGarrisoned) continue;

            _garrisonedUnits.Add(hit.gameObject);
            attr.isGarrisoned = true;
            attr.garrisonedGarrisonPoint = this;

            UnitMovement movement = hit.GetComponent<UnitMovement>();
            if (movement != null)
                movement.StopMovement();
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
            attr.garrisonedGarrisonPoint = this;

            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
                movement.StopMovement();
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, garrisonRange);
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
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, garrisonRange);
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

    public bool IsFull()
    {
        return _garrisonedUnits.Count >= maxGarrison;
    }

    public bool IsGarrisonedUnit(GameObject unit)
    {
        return _garrisonedUnits.Contains(unit);
    }

    void OnDrawGizmosSelected()
    {
        // 编辑器下使用 transform.position（运行时用 worldPosition）
        Vector3 pos = Application.isPlaying ? (Vector3)worldPosition : transform.position;

        Gizmos.color = _occupyingCamp == null ? Color.gray
            : _occupyingCamp == CampType.Player ? Color.green : Color.red;
        if (_isContested) Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, garrisonRange);
    }
}
