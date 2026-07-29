using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 驻扎点组件 — 挂载在动态实例化的驻扎点 GameObject 上。
/// 负责路径绑定、生效阵营过滤和曲线位置同步。
/// 驻扎/占领/争夺逻辑委托给 Garrison 组件。
///
/// 架构：GarrisonPoint = Garrison（驻扎） + GarrisonPoint（路径绑定+阵营过滤）
/// 驻扎核心逻辑统一由 Garrison 组件处理，本组件关注路径吸附和阵营权限。
/// </summary>
[RequireComponent(typeof(Garrison))]
public class GarrisonPoint : MonoBehaviour
{
    [Header("=== 生效阵营 ===")]
    [Tooltip("对哪些阵营的单位生效（可多选）")]
    public List<CampType> effectiveCamps = new List<CampType>();

    // ==================== 路径绑定 ====================

    [HideInInspector]
    public Vector2 worldPosition;

    [HideInInspector]
    public PathManager boundPath;

    [HideInInspector]
    public int boundSegmentIndex;

    [HideInInspector]
    public float boundSegmentT;

    [HideInInspector]
    [Tooltip("归一化曲线距离 [0,1]，用于曲线模式下的精确定位")]
    public float boundCurveT;

    // ==================== 委托给 Garrison 的属性 ====================

    /// <summary>驻扎范围（委托给 Garrison 组件）</summary>
    public float garrisonRange
    {
        get => _garrison != null ? _garrison.garrisonRange : _cachedGarrisonRange;
        set
        {
            _cachedGarrisonRange = value;
            if (_garrison != null) _garrison.garrisonRange = value;
        }
    }
    [SerializeField, FormerlySerializedAs("garrisonRange")]
    private float _cachedGarrisonRange = 1.5f;

    /// <summary>最大驻扎人数（委托给 Garrison 组件）</summary>
    public int maxGarrison
    {
        get => _garrison != null ? _garrison.maxGarrison : _cachedMaxGarrison;
        set
        {
            _cachedMaxGarrison = value;
            if (_garrison != null) _garrison.maxGarrison = value;
        }
    }
    [SerializeField, FormerlySerializedAs("maxGarrison")]
    private int _cachedMaxGarrison = 3;

    /// <summary>当前占领阵营（委托）</summary>
    public CampType? occupyingCamp => _garrison != null ? _garrison.occupyingCamp : null;

    /// <summary>是否处于争夺中（委托）</summary>
    public bool isContested => _garrison != null && _garrison.isContested;

    /// <summary>是否处于活跃占领状态（委托）</summary>
    public bool isActive => _garrison != null && _garrison.isActive;

    /// <summary>已驻扎单位数量（委托）</summary>
    public int garrisonedCount => _garrison != null ? _garrison.garrisonedCount : 0;

    private Garrison _garrison;

    // ==================== 生命周期 ====================

    void Awake()
    {
        _garrison = GetComponent<Garrison>();
        if (_garrison == null)
            _garrison = gameObject.AddComponent<Garrison>();

        // Awake 阶段同步一次（此时 _garrison 刚初始化完）
        SyncConfigToGarrison();
    }

    void Start()
    {
        if (GarrisonPointManager.Instance != null)
            GarrisonPointManager.Instance.Register(this);

        // 同步 GameObject 位置
        transform.position = worldPosition;
    }

    /// <summary>
    /// Inspector 中任何值变化时自动调用（Edit Mode 和 Play Mode 均生效）。
    /// 同时也作为运行时从缓存同步到 Garrison 的入口。
    /// </summary>
    void OnValidate()
    {
        SyncConfigToGarrison();
    }

    /// <summary>
    /// 将序列化缓存值推送到 Garrison 组件（真正的运行时数据源）
    /// </summary>
    private void SyncConfigToGarrison()
    {
        if (_garrison == null)
            _garrison = GetComponent<Garrison>();
        if (_garrison == null) return;

        _garrison.garrisonRange = _cachedGarrisonRange;
        _garrison.maxGarrison = _cachedMaxGarrison;
    }

    void Update()
    {
        // 曲线模式下同步位置到路径
        if (boundPath != null && boundCurveT >= 0f)
        {
            Vector2 curvePos = boundPath.GetCurvePoint(boundCurveT);
            transform.position = curvePos;
            worldPosition = curvePos;
        }

        // 验证绑定的路径仍然有效
        if (boundPath == null || boundPath.pathPoints == null
            || boundSegmentIndex >= boundPath.pathPoints.Count - 1
            || boundPath.pathPoints.Count < 2)
        {
            if (GarrisonPointManager.Instance != null)
                GarrisonPointManager.Instance.Unregister(this);
            Destroy(gameObject);
        }
    }

    // ==================== 阵营过滤 ====================

    /// <summary>
    /// 检查该驻扎点对指定阵营是否生效
    /// </summary>
    public bool IsEffectiveFor(CampType camp)
    {
        return effectiveCamps.Contains(camp);
    }

    // ==================== 委托给 Garrison 的方法（带阵营过滤） ====================

    /// <summary>
    /// 检查单位是否在驻扎范围内
    /// </summary>
    public bool IsUnitInRange(GameObject unit)
        => _garrison != null && _garrison.IsUnitInRange(unit);

    /// <summary>
    /// 判定单位是否可以驻扎（含生效阵营过滤）
    /// </summary>
    public bool CanGarrison(GameObject unit)
    {
        UnitAttr attr = unit.GetComponent<UnitAttr>();
        if (attr == null) return false;

        // 检查生效阵营（GarrisonPoint 专属逻辑）
        if (!IsEffectiveFor(attr.camp)) return false;

        return _garrison != null && _garrison.CanGarrison(unit);
    }

    /// <summary>单位加入驻扎</summary>
    public void AddGarrison(GameObject unit)
        => _garrison?.AddGarrison(unit);

    /// <summary>单位离开驻扎</summary>
    public void RemoveGarrison(GameObject unit)
        => _garrison?.RemoveGarrison(unit);

    /// <summary>触发抢占战斗</summary>
    public void StartContest(GameObject attacker)
        => _garrison?.StartContest(attacker);

    /// <summary>是否已满员</summary>
    public bool IsFull()
        => _garrison != null && _garrison.IsFull();

    /// <summary>指定单位是否已驻扎在此</summary>
    public bool IsGarrisonedUnit(GameObject unit)
        => _garrison != null && _garrison.IsGarrisonedUnit(unit);
}
