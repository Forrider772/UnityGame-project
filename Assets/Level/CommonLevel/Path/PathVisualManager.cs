using UnityEngine;

/// <summary>
/// 路线视觉效果管理器（简化版）
/// 负责管理路线的 LineRenderer 显示、高亮效果、动画
/// 支持几种内置效果，通过枚举切换，无需额外组件
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PathVisualManager : MonoBehaviour
{
    /// <summary>
    /// 支持的效果类型
    /// </summary>
    public enum EffectType
    {
        None,               // 无任何视觉效果
        ColorChange,        // 悬停时只改变线条颜色
        WidthPulse,         // 悬停时线条变粗并脉动，同时改变颜色
        ColorAndPulse       // 悬停时同时改变颜色和脉动宽度
    }

    [Header("效果选择")]
    public EffectType effect = EffectType.ColorAndPulse;   // 默认改为 ColorAndPulse

    [Header("颜色配置")]
    public Color normalColor = new Color(0.5f, 1f, 0.5f, 1f);  // 浅绿色
    public Color highlightColor = new Color(0.2f, 0.8f, 0.6f, 1f); // 蓝绿色

    [Header("宽度配置")]
    public float normalWidth = 0.2f;
    public float highlightWidth = 0.6f;
    public float pulseSpeed = 3f;

    private LineRenderer lineRenderer;   // 线条渲染器组件
    private bool isHighlighted = false;  // 当前是否处于高亮状态
    private float pulseTimer = 0f;       // 脉冲动画计时器

    private void Awake()
    {
        // 获取或添加 LineRenderer 组件
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // 强制使用支持顶点颜色的材质
        Material spriteMat = new Material(Shader.Find("Sprites/Default"));
        if (spriteMat != null)
            lineRenderer.material = spriteMat;
        else
            Debug.LogError("无法找到 Sprites/Default 着色器，颜色将不会生效");
    }

    /// <summary>
    /// 初始化路线，设置路径点并应用默认样式
    /// 必须在设置完路径点数组后调用
    /// </summary>
    /// <param name="waypoints">路径点的世界坐标数组（Vector2）</param>
    public void Initialize(Vector2[] waypoints)
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("路径点不足，无法绘制线条", this);
            return;
        }

        // 设置 LineRenderer 的顶点数量及位置
        lineRenderer.positionCount = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
            lineRenderer.SetPosition(i, waypoints[i]);

        // 应用默认样式（普通状态）
        lineRenderer.startWidth = normalWidth;
        lineRenderer.endWidth = normalWidth;
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;

        // 初始时隐藏路线（等待进入部署模式后再显示）
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// 设置路线是否可见
    /// </summary>
    /// <param name="visible">true 显示，false 隐藏</param>
    public void SetVisible(bool visible)
    {
        lineRenderer.enabled = visible;
    }

    /// <summary>
    /// 设置高亮状态，根据当前选中的特效类型改变视觉表现
    /// </summary>
    /// <param name="highlight">true 进入高亮，false 恢复正常</param>
    public void SetHighlight(bool highlight)
    {
        if (isHighlighted == highlight) return; // 避免重复设置
        isHighlighted = highlight;

        if (highlight)
            ApplyHighlightStyle();   // 应用高亮样式
        else
            ApplyNormalStyle();      // 恢复普通样式
    }

    /// <summary>
    /// 应用普通样式（未高亮状态）
    /// 恢复颜色和宽度，重置脉冲计时器
    /// </summary>
    private void ApplyNormalStyle()
    {
        lineRenderer.startColor = normalColor;
        lineRenderer.endColor = normalColor;
        lineRenderer.startWidth = normalWidth;
        lineRenderer.endWidth = normalWidth;
        pulseTimer = 0f;
    }

    /// <summary>
    /// 根据 EffectType 应用高亮样式
    /// </summary>
    private void ApplyHighlightStyle()
    {
        switch (effect)
        {
            case EffectType.ColorChange:
                // 只改变颜色
                lineRenderer.startColor = highlightColor;
                lineRenderer.endColor = highlightColor;
                break;
            case EffectType.WidthPulse:
                // 改变颜色并将宽度临时设为最大，后续由 Update 动画控制脉冲
                lineRenderer.startColor = highlightColor;
                lineRenderer.endColor = highlightColor;
                lineRenderer.startWidth = highlightWidth;
                lineRenderer.endWidth = highlightWidth;
                pulseTimer = 0f;
                break;
            case EffectType.ColorAndPulse:
                // 改变颜色，宽度动画由 Update 控制
                lineRenderer.startColor = highlightColor;
                lineRenderer.endColor = highlightColor;
                break;
            case EffectType.None:
            default:
                break;
        }
    }

    private void Update()
    {
        // 只有高亮状态且需要脉冲效果时，才执行宽度动画
        if (!isHighlighted) return;

        if (effect == EffectType.WidthPulse || effect == EffectType.ColorAndPulse)
        {
            // 脉冲计时器递增，根据正弦曲线在 normalWidth 和 highlightWidth 之间平滑插值
            pulseTimer += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(pulseTimer) + 1f) / 2f;  // 将正弦波从 [-1,1] 映射到 [0,1]
            float currentWidth = Mathf.Lerp(normalWidth, highlightWidth, t);
            lineRenderer.startWidth = currentWidth;
            lineRenderer.endWidth = currentWidth;
        }
    }
}