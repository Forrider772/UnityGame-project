using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 资源点组件 — 挂载在场景中的资源点 GameObject 上。
/// 负责资源加成逻辑，驻扎功能委托给 Garrison 组件。
///
/// 架构：ResourcePoint = Garrison（驻扎） + ResourcePoint（资源加成）
/// 驻扎/占领/争夺逻辑统一由 Garrison 组件处理，本组件仅关注资源产出。
/// </summary>
[RequireComponent(typeof(Garrison))]
public class ResourcePoint : MonoBehaviour
{
    [Header("=== 资源参数 ===")]
    [Tooltip("占领后每秒提供的额外费用加成")]
    public float resourceBonus = 5f;

    // ==================== 委托给 Garrison 的属性 ====================

    /// <summary>驻扎范围（委托给 Garrison 组件，保留兼容 Inspector）</summary>
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

        // Awake 阶段同步一次
        SyncConfigToGarrison();
    }

    void Start()
    {
        if (ResourcePointManager.Instance != null)
            ResourcePointManager.Instance.Register(this);
    }

    /// <summary>
    /// Inspector 中任何值变化时自动调用（Edit Mode 和 Play Mode 均生效）
    /// </summary>
    void OnValidate()
    {
        SyncConfigToGarrison();
    }

    /// <summary>
    /// 将序列化缓存值推送到 Garrison 组件
    /// </summary>
    private void SyncConfigToGarrison()
    {
        if (_garrison == null)
            _garrison = GetComponent<Garrison>();
        if (_garrison == null) return;

        _garrison.garrisonRange = _cachedGarrisonRange;
        _garrison.maxGarrison = _cachedMaxGarrison;
    }

    // ==================== 委托给 Garrison 的方法 ====================

    /// <summary>检查单位是否在驻扎范围内</summary>
    public bool IsUnitInRange(GameObject unit)
        => _garrison != null && _garrison.IsUnitInRange(unit);

    /// <summary>判定单位是否可以驻扎</summary>
    public bool CanGarrison(GameObject unit)
        => _garrison != null && _garrison.CanGarrison(unit);

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
