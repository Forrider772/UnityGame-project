using UnityEngine;

/// <summary>
/// 空心圆范围显示组件 — 使用 LineRenderer 绘制圆环轮廓（非填充）。
/// 使用世界坐标定位，半径精确为世界单位，不受父级缩放影响。
/// 挂载到目标 GameObject 上，自动创建子对象管理 LineRenderer。
///
/// 使用方式：
///   1. 挂载到需要显示范围的 GameObject 上
///   2. 每帧调用 UpdateDisplay(radius, color) 同步状态
/// </summary>
public class LeashCircleDisplay : MonoBehaviour
{
    [Header("=== 圆圈外观 ===")]
    [Tooltip("圆环分段数（越多越平滑）")]
    [SerializeField] private int segments = 64;

    [Tooltip("圆环线宽（世界单位）")]
    [SerializeField] private float lineWidth = 0.15f;

    [Tooltip("渲染所在的 Sorting Layer 名称")]
    [SerializeField] private string sortingLayer = "Building";

    [Tooltip("同 Sorting Layer 内的排序优先级")]
    [SerializeField] private int sortingOrder = 10;

    private LineRenderer _lineRenderer;
    private Vector3[] _points;
    private float _displayRadius = -1f;
    private Vector3 _lastCenter;
    private bool _initialized;

    void Awake()
    {
        CreateLineChild();
        _initialized = true;
    }

    /// <summary>
    /// 更新圆环的半径和颜色。
    /// 仅在半径或中心位置变化时重算顶点，避免每帧不必要的开销。
    /// </summary>
    /// <param name="radius">圆环半径（世界单位）</param>
    /// <param name="color">圆环颜色</param>
    public void UpdateDisplay(float radius, Color color)
    {
        if (!_initialized || _lineRenderer == null) return;

        bool radiusChanged = Mathf.Abs(radius - _displayRadius) > 0.001f;
        bool centerMoved = (transform.position - _lastCenter).sqrMagnitude > 0.0001f;

        if (radiusChanged || centerMoved)
        {
            _displayRadius = radius;
            _lastCenter = transform.position;
            RebuildPoints(radius);
        }

        // 颜色设置开销极低，直接应用
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }

    /// <summary>设置圆环可见性</summary>
    public void SetVisible(bool visible)
    {
        if (_lineRenderer != null)
            _lineRenderer.enabled = visible;
    }

    // ==================== 内部实现 ====================

    /// <summary>
    /// 创建子 GameObject 并挂载 LineRenderer（世界坐标模式）
    /// </summary>
    private void CreateLineChild()
    {
        var child = new GameObject("LeashCircle");
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;

        _lineRenderer = child.AddComponent<LineRenderer>();
        // 世界坐标定位 → 半径精确为 leashRange，不受父级（塔）缩放影响
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = segments;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        // 使用支持顶点颜色的材质（参照 PathVisualManager）
        Material mat = new Material(Shader.Find("Sprites/Default"));
        if (mat != null)
            _lineRenderer.material = mat;
        else
            Debug.LogError("LeashCircleDisplay: 无法找到 Sprites/Default 着色器，颜色将不会生效");

        _lineRenderer.sortingLayerName = sortingLayer;
        _lineRenderer.sortingOrder = sortingOrder;

        _points = new Vector3[segments];
    }

    /// <summary>以世界坐标围绕当前中心点重建圆环顶点</summary>
    private void RebuildPoints(float radius)
    {
        Vector2 center = transform.position;
        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            _points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        _lineRenderer.SetPositions(_points);
    }

    void OnDestroy()
    {
        if (_lineRenderer != null)
            Destroy(_lineRenderer.gameObject);
    }
}
