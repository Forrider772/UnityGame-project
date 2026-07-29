using UnityEngine;

/// <summary>
/// 运行时范围圈显示组件 — 实心填充圆。
/// 使用 SpriteRenderer + 程序化生成的圆形纹理，渲染在指定 Sorting Layer 上。
/// 挂载到目标 GameObject 上，自动创建子对象管理 SpriteRenderer。
///
/// 使用方式：
///   1. 挂载到需要显示范围的 GameObject 上
///   2. 每帧调用 UpdateDisplay(radius, occupyingCamp, isContested) 同步状态
/// </summary>
public class RangeCircleDisplay : MonoBehaviour
{
    [Header("=== 圆圈外观 ===")]
    [Tooltip("纹理分辨率（像素，越大边缘越平滑）")]
    [SerializeField] private int textureResolution = 256;

    [Tooltip("渲染所在的 Sorting Layer 名称")]
    [SerializeField] private string sortingLayer = "Building";

    [Tooltip("同 Sorting Layer 内的排序优先级")]
    [SerializeField] private int sortingOrder = 0;

    [Header("=== 状态颜色 ===")]
    [Tooltip("无人占领时的颜色")]
    [SerializeField] private Color unoccupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    [Tooltip("Player 占领时的颜色")]
    [SerializeField] private Color playerColor = new Color(0.2f, 0.9f, 0.2f, 0.4f);

    [Tooltip("Enemy 占领时的颜色")]
    [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.2f, 0.4f);

    [Tooltip("争夺中时的颜色")]
    [SerializeField] private Color contestedColor = new Color(1f, 1f, 0.2f, 0.5f);

    private GameObject _circleChild;
    private SpriteRenderer _spriteRenderer;
    private float _displayRadius = -1f;
    private CampType? _displayCamp;
    private bool _displayContested;
    private bool _initialized;

    void Awake()
    {
        CreateCircleChild();
        _initialized = true;
    }

    /// <summary>
    /// 更新范围圈的半径和颜色。
    /// 仅在值发生变化时才更新，避免每帧不必要的开销。
    /// </summary>
    /// <param name="radius">圆圈半径（世界单位）</param>
    /// <param name="occupyingCamp">当前占领阵营（null = 无人）</param>
    /// <param name="isContested">是否处于争夺状态</param>
    public void UpdateDisplay(float radius, CampType? occupyingCamp, bool isContested)
    {
        if (!_initialized || _spriteRenderer == null) return;

        bool radiusChanged = Mathf.Abs(radius - _displayRadius) > 0.001f;
        bool stateChanged = _displayCamp != occupyingCamp || _displayContested != isContested;

        if (radiusChanged)
        {
            _displayRadius = radius;
            // 圆形纹理直径为 1 世界单位，缩放到直径 = radius * 2
            float scale = radius * 2f;
            _circleChild.transform.localScale = new Vector3(scale, scale, 1f);
        }

        if (stateChanged || radiusChanged)
        {
            _displayCamp = occupyingCamp;
            _displayContested = isContested;
            ApplyColor();
        }
    }

    /// <summary>
    /// 设置圆圈可见性
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_circleChild != null)
            _circleChild.SetActive(visible);
    }

    // ==================== 内部实现 ====================

    /// <summary>
    /// 创建子 GameObject，生成圆形纹理和 SpriteRenderer
    /// </summary>
    private void CreateCircleChild()
    {
        _circleChild = new GameObject("RangeCircle");
        _circleChild.transform.SetParent(transform);
        _circleChild.transform.localPosition = Vector3.zero;
        _circleChild.transform.localScale = Vector3.one;

        _spriteRenderer = _circleChild.AddComponent<SpriteRenderer>();

        // 程序化生成圆形纹理
        Texture2D tex = GenerateCircleTexture(textureResolution);
        // pixelsPerUnit = textureResolution → 纹理直径 = 1 世界单位
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, textureResolution, textureResolution),
            new Vector2(0.5f, 0.5f), textureResolution);
        _spriteRenderer.sprite = sprite;

        // 设置 Sorting Layer
        _spriteRenderer.sortingLayerName = sortingLayer;
        _spriteRenderer.sortingOrder = sortingOrder;

        // 初始颜色
        _spriteRenderer.color = unoccupiedColor;
    }

    /// <summary>
    /// 程序化生成一张带有抗锯齿边缘的白色圆形纹理
    /// </summary>
    /// <param name="size">纹理宽高（像素）</param>
    /// <returns>RGBA32 格式的圆形纹理</returns>
    private static Texture2D GenerateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;
        float radius = size / 2f;
        // 抗锯齿过渡带宽度（像素），让边缘平滑
        float aaWidth = 1.5f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                // 使用 smoothstep 做抗锯齿
                float alpha = 1f - Mathf.Clamp01((dist - radius + aaWidth) / aaWidth);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 根据当前占领/争夺状态应用颜色
    /// </summary>
    private void ApplyColor()
    {
        if (_spriteRenderer == null) return;

        Color color;
        if (_displayContested)
            color = contestedColor;
        else if (_displayCamp == null)
            color = unoccupiedColor;
        else if (_displayCamp == CampType.Player)
            color = playerColor;
        else
            color = enemyColor;

        _spriteRenderer.color = color;
    }

    void OnDestroy()
    {
        if (_circleChild != null)
            Destroy(_circleChild);
    }
}
