using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂载到场景中任意 GameObject 上即可自动生成完整 HUD
/// 包含：血条（含拱形装饰）+ 底部快捷栏
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("=== 血条设置 ===")]
    [Tooltip("血条最大宽度（像素）")]
    public float healthBarWidth = 820f;
    [Tooltip("血条高度（像素）")]
    public float healthBarHeight = 28f;
    [Tooltip("当前血量百分比 0~1")]
    [Range(0f, 1f)]
    public float healthPercent = 1f;

    [Header("=== 快捷栏设置 ===")]
    [Tooltip("快捷栏格子数量")]
    public int slotCount = 6;
    [Tooltip("每个格子大小（像素）")]
    public float slotSize = 72f;
    [Tooltip("格子间距（像素）")]
    public float slotSpacing = 8f;

    // === 内部引用 ===
    private Image _healthFillImage;
    private RectTransform _healthFillRect;

    private void Start()
    {
        BuildHUD();
        SetHealth(healthPercent);
    }

    // ================================================================
    //  构建入口
    // ================================================================
    private void BuildHUD()
    {
        Canvas canvas = FindOrCreateCanvas();

        // ---------- 血条 ----------
        GameObject healthRoot = CreateUIObject("HealthBar_Root", canvas.transform);
        RectTransform healthRootRT = healthRoot.GetComponent<RectTransform>();
        healthRootRT.anchorMin = new Vector2(0.5f, 1f);
        healthRootRT.anchorMax = new Vector2(0.5f, 1f);
        healthRootRT.pivot = new Vector2(0.5f, 1f);
        healthRootRT.anchoredPosition = new Vector2(0f, -30f);
        healthRootRT.sizeDelta = new Vector2(healthBarWidth + 100f, 120f);

        BuildHealthBar(healthRoot.transform);

        // ---------- 快捷栏 ----------
        GameObject hotbarRoot = CreateUIObject("Hotbar_Root", canvas.transform);
        RectTransform hotbarRootRT = hotbarRoot.GetComponent<RectTransform>();
        hotbarRootRT.anchorMin = new Vector2(0.5f, 0f);
        hotbarRootRT.anchorMax = new Vector2(0.5f, 0f);
        hotbarRootRT.pivot = new Vector2(0.5f, 0f);
        hotbarRootRT.anchoredPosition = new Vector2(0f, 10f);
        hotbarRootRT.sizeDelta = new Vector2(slotCount * (slotSize + slotSpacing) + 180f, slotSize + 40f);

        BuildHotbar(hotbarRoot.transform);
    }

    // ================================================================
    //  血条
    // ================================================================
    private void BuildHealthBar(Transform parent)
    {
        // --- 血条背景 ---
        GameObject bg = CreateUIObject("HealthBG", parent);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        bgRT.anchoredPosition = new Vector2(0f, -30f);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.17f, 0.22f, 0.9f);

        // --- 血条填充 ---
        GameObject fill = CreateUIObject("HealthFill", bg.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        _healthFillImage = fillImg;
        _healthFillRect = fillRT;

        // 渐变色：用脚本生成渐变纹理
        Texture2D gradTex = CreateGradientTexture(
            new Color(0.1f, 0.6f, 1f, 1f),    // 左：浅蓝
            new Color(0.15f, 0.25f, 0.7f, 1f)  // 右：深蓝
        );
        fillImg.sprite = Sprite.Create(gradTex, new Rect(0, 0, gradTex.width, gradTex.height), Vector2.one * 0.5f);

        // --- 拱形装饰（中间凸起的半圆） ---
        BuildArch(parent);

        // --- 背景边框线（上下两根细线增加层次感） ---
        GameObject lineTop = CreateUIObject("LineTop", parent);
        RectTransform ltRT = lineTop.GetComponent<RectTransform>();
        ltRT.anchorMin = new Vector2(0.5f, 0.5f);
        ltRT.anchorMax = new Vector2(0.5f, 0.5f);
        ltRT.pivot = new Vector2(0.5f, 0.5f);
        ltRT.sizeDelta = new Vector2(healthBarWidth + 4f, 2f);
        ltRT.anchoredPosition = new Vector2(0f, -30f + healthBarHeight * 0.5f + 1f);
        Image ltImg = lineTop.AddComponent<Image>();
        ltImg.color = new Color(0.4f, 0.5f, 0.6f, 0.6f);

        GameObject lineBot = CreateUIObject("LineBot", parent);
        RectTransform lbRT = lineBot.GetComponent<RectTransform>();
        lbRT.anchorMin = new Vector2(0.5f, 0.5f);
        lbRT.anchorMax = new Vector2(0.5f, 0.5f);
        lbRT.pivot = new Vector2(0.5f, 0.5f);
        lbRT.sizeDelta = new Vector2(healthBarWidth + 4f, 2f);
        lbRT.anchoredPosition = new Vector2(0f, -30f - healthBarHeight * 0.5f - 1f);
        Image lbImg = lineBot.AddComponent<Image>();
        lbImg.color = new Color(0.4f, 0.5f, 0.6f, 0.6f);
    }

    private void BuildArch(Transform parent)
    {
        // 拱形 = 上半圆 + 下矩形，组合成向上凸起的形状
        float archWidth = 120f;
        float archHeight = 35f;  // 凸出血条的高度
        float barY = -30f;       // 血条中心 Y

        // --- 上半圆 ---
        GameObject circle = CreateUIObject("ArchCircle", parent);
        RectTransform cRT = circle.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f);
        cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0f);
        cRT.sizeDelta = new Vector2(archWidth, archHeight * 2f);
        cRT.anchoredPosition = new Vector2(0f, barY + healthBarHeight * 0.5f);

        Image cImg = circle.AddComponent<Image>();
        cImg.color = new Color(0.15f, 0.17f, 0.22f, 0.9f);

        // 用 Mask 裁剪出上半圆
        circle.AddComponent<Mask>().showMaskGraphic = true;
        cImg.maskable = true;

        // 在 Mask 内放一个全尺寸填充来只显示上半部分
        GameObject clipRect = CreateUIObject("ArchClip", circle.transform);
        RectTransform clipRT = clipRect.GetComponent<RectTransform>();
        clipRT.anchorMin = new Vector2(0f, 0.5f);
        clipRT.anchorMax = Vector2.one;
        clipRT.offsetMin = Vector2.zero;
        clipRT.offsetMax = Vector2.zero;
        Image clipImg = clipRect.AddComponent<Image>();
        clipImg.color = new Color(0.15f, 0.17f, 0.22f, 0.9f);
        clipImg.raycastTarget = false;

        // --- 拱形内部的蓝色填充 ---
        GameObject archFill = CreateUIObject("ArchFill", circle.transform);
        RectTransform afRT = archFill.GetComponent<RectTransform>();
        afRT.anchorMin = new Vector2(0.5f, 0f);
        afRT.anchorMax = new Vector2(0.5f, 0.5f);
        afRT.pivot = new Vector2(0.5f, 0f);
        afRT.sizeDelta = new Vector2(archWidth - 12f, archHeight);
        afRT.anchoredPosition = new Vector2(0f, 0f);
        Image afImg = archFill.AddComponent<Image>();
        afImg.color = new Color(0.1f, 0.55f, 0.9f, 0.95f);
        afImg.raycastTarget = false;

        // --- 下方矩形填充（与血条颜色一致的过渡区） ---
        GameObject rectFill = CreateUIObject("ArchRectFill", parent);
        RectTransform rfRT = rectFill.GetComponent<RectTransform>();
        rfRT.anchorMin = new Vector2(0.5f, 0.5f);
        rfRT.anchorMax = new Vector2(0.5f, 0.5f);
        rfRT.pivot = new Vector2(0.5f, 0f);
        rfRT.sizeDelta = new Vector2(archWidth, healthBarHeight * 0.5f + 2f);
        rfRT.anchoredPosition = new Vector2(0f, barY);
        Image rfImg = rectFill.AddComponent<Image>();
        rfImg.color = new Color(0.1f, 0.55f, 0.9f, 0.95f);
    }

    // ================================================================
    //  快捷栏
    // ================================================================
    private void BuildHotbar(Transform parent)
    {
        // --- 整体背景条 ---
        float totalWidth = slotCount * (slotSize + slotSpacing) - slotSpacing + 160f;
        GameObject bg = CreateUIObject("HotbarBG", parent);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(totalWidth, slotSize + 16f);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.88f, 0.86f, 0.82f, 0.85f);

        // --- 左侧圆形槽 ---
        BuildCircularSlot(bg.transform, -totalWidth * 0.5f + slotSize * 0.5f + 12f);

        // --- 右侧圆形槽 ---
        BuildCircularSlot(bg.transform, totalWidth * 0.5f - slotSize * 0.5f - 12f);

        // --- 中间方形槽 ---
        float slotsWidth = slotCount * (slotSize + slotSpacing) - slotSpacing;
        float startX = -slotsWidth * 0.5f;

        for (int i = 0; i < slotCount; i++)
        {
            float x = startX + i * (slotSize + slotSpacing) + slotSize * 0.5f;
            BuildSquareSlot(bg.transform, x, i + 1);
        }
    }

    private void BuildCircularSlot(Transform parent, float xPos)
    {
        GameObject slot = CreateUIObject("CircularSlot", parent);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(slotSize, slotSize);
        rt.anchoredPosition = new Vector2(xPos, 0f);

        Image img = slot.AddComponent<Image>();
        img.color = new Color(0.78f, 0.76f, 0.72f, 0.9f);

        // 内圈（稍小的圆，增加层次感）
        GameObject inner = CreateUIObject("Inner", slot.transform);
        RectTransform iRT = inner.GetComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero;
        iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(4f, 4f);
        iRT.offsetMax = new Vector2(-4f, -4f);
        Image iImg = inner.AddComponent<Image>();
        iImg.color = new Color(0.72f, 0.70f, 0.66f, 0.8f);
    }

    private void BuildSquareSlot(Transform parent, float xPos, int index)
    {
        GameObject slot = CreateUIObject($"Slot_{index}", parent);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(slotSize, slotSize);
        rt.anchoredPosition = new Vector2(xPos, 0f);

        Image img = slot.AddComponent<Image>();
        img.color = new Color(0.82f, 0.80f, 0.76f, 0.9f);

        // 内部边框
        GameObject border = CreateUIObject("Border", slot.transform);
        RectTransform bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(3f, 3f);
        bRT.offsetMax = new Vector2(-3f, -3f);
        Image bImg = border.AddComponent<Image>();
        bImg.color = new Color(0.75f, 0.73f, 0.69f, 0.8f);
    }

    // ================================================================
    //  公共 API
    // ================================================================

    /// <summary>设置血量百分比 0~1</summary>
    public void SetHealth(float percent)
    {
        percent = Mathf.Clamp01(percent);
        if (_healthFillImage != null)
            _healthFillImage.fillAmount = percent;
    }

    /// <summary>平滑过渡血量</summary>
    public void SetHealthSmooth(float percent, float duration = 0.5f)
    {
        StartCoroutine(SmoothHealth(percent, duration));
    }

    private System.Collections.IEnumerator SmoothHealth(float target, float duration)
    {
        float start = _healthFillImage.fillAmount;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _healthFillImage.fillAmount = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        _healthFillImage.fillAmount = target;
    }

    // ================================================================
    //  工具方法
    // ================================================================
    private Canvas FindOrCreateCanvas()
    {
        Canvas existing = FindObjectOfType<Canvas>();
        if (existing != null) return existing;

        GameObject canvasGO = new GameObject("GameHUD_Canvas");
        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        return c;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private Texture2D CreateGradientTexture(Color left, Color right)
    {
        int w = 256;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        for (int i = 0; i < w; i++)
        {
            float t = (float)i / (w - 1);
            tex.SetPixel(i, 0, Color.Lerp(left, right, t));
        }
        tex.Apply();
        return tex;
    }
}
