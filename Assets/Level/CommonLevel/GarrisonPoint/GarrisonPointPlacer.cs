using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 驻扎点放置交互系统
/// 流程：
/// 1. 按钮调用 StartDeployXxx() → 进入放置模式，所有路线半透明显示
/// 2. 鼠标悬浮路线 → 悬停点显示圆形高亮指示器
/// 3. 左键点击 → 在吸附位置创建驻扎点 → 退出放置模式
/// 4. 右键 → 退出放置模式（不创建）
///
/// 使用方式：
/// - Inspector 中给 Button 的 onClick 绑定 StartDeployPlayer / StartDeployEnemy / StartDeployBoth
/// - 或脚本调用：placer.StartDeploy(new List<CampType> { CampType.Player })
/// </summary>
public class GarrisonPointPlacer : MonoBehaviour
{
    [Header("放置设置")]
    [Tooltip("鼠标悬停路线的检测阈值（世界单位）")]
    public float hoverThreshold = 0.5f;

    [Header("路线半透明")]
    [Tooltip("放置模式下路线的透明度（0~1）")]
    [Range(0f, 1f)]
    public float routeAlpha = 0.3f;

    [Header("悬浮圆形指示器")]
    [Tooltip("圆形指示器的半径")]
    public float hoverCircleRadius = 0.4f;

    [Tooltip("圆形指示器的颜色")]
    public Color hoverCircleColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);

    [Tooltip("圆形指示器的宽度")]
    public float hoverCircleWidth = 0.15f;

    [Tooltip("圆形指示器分段数（越大越圆滑）")]
    public int hoverCircleSegments = 32;

    private bool isDeploying = false;
    private List<CampType> selectedCamps = new List<CampType>();
    private List<PathManager> allPaths = new List<PathManager>();
    private PathManager currentHoveredPath;
    private float currentCurveT;
    private Camera mainCamera;

    // 悬浮圆形指示器
    private GameObject hoverCircleObj;
    private LineRenderer hoverCircleRenderer;

    private void Start()
    {
        mainCamera = Camera.main;
        CreateHoverCircle();
    }

    private void Update()
    {
        if (!isDeploying) return;

        Vector2 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 查找鼠标下方最近的路径（使用 GetClosestPoint 统一处理曲线/直线）
        PathManager hitPath = null;
        float bestCurveT = 0f;
        Vector2 snapPoint = mouseWorldPos;
        float minDist = hoverThreshold;

        foreach (var path in allPaths)
        {
            if (path == null) continue;
            var result = path.GetClosestPoint(mouseWorldPos);
            if (result.distance < minDist)
            {
                minDist = result.distance;
                hitPath = path;
                snapPoint = result.point;
                bestCurveT = result.curveT;
            }
        }

        currentHoveredPath = hitPath;

        if (hitPath != null)
        {
            currentCurveT = bestCurveT;
            ShowHoverCircle(true, snapPoint);
        }
        else
        {
            ShowHoverCircle(false, mouseWorldPos);
        }

        // 左键点击：放置驻扎点
        if (Input.GetMouseButtonDown(0) && hitPath != null)
        {
            PlaceGarrisonPoint();
        }

        // 右键：取消放置
        if (Input.GetMouseButtonDown(1))
        {
            ExitDeployMode();
        }
    }

    // ==================== Inspector 按钮绑定方法（无参数）====================

    /// <summary>仅对己方生效</summary>
    public void StartDeployPlayer() => StartDeploy(new List<CampType> { CampType.Player });

    /// <summary>仅对敌方生效</summary>
    public void StartDeployEnemy() => StartDeploy(new List<CampType> { CampType.Enemy });

    /// <summary>对双方生效</summary>
    public void StartDeployBoth() => StartDeploy(new List<CampType> { CampType.Player, CampType.Enemy });

    // ==================== 脚本 API ====================

    /// <summary>
    /// 启动放置模式（由脚本调用，传入自定义生效阵营列表）
    /// </summary>
    public void StartDeploy(List<CampType> camps)
    {
        if (camps == null || camps.Count == 0)
        {
            Debug.LogWarning("GarrisonPointPlacer: 生效阵营列表不能为空");
            return;
        }

        selectedCamps = new List<CampType>(camps);

        // 收集所有路线（双方阵营）
        allPaths.Clear();
        if (LevelPathManager.Instance != null)
        {
            foreach (CampType camp in System.Enum.GetValues(typeof(CampType)))
            {
                List<PathManager> paths = LevelPathManager.Instance.GetAllPathsByCamp(camp);
                allPaths.AddRange(paths);
            }
        }

        if (allPaths.Count == 0)
        {
            Debug.LogWarning("GarrisonPointPlacer: 场景中没有可用路线");
            return;
        }

        // 所有路线半透明显示
        foreach (var path in allPaths)
        {
            path.SetVisible(true);
            path.SetAlpha(routeAlpha);
        }

        isDeploying = true;
        currentHoveredPath = null;

        Debug.Log($"进入驻扎点放置模式，生效阵营: [{string.Join(",", selectedCamps)}]");
    }

    /// <summary>
    /// 是否正在放置中
    /// </summary>
    public bool IsDeploying => isDeploying;

    /// <summary>
    /// 放置驻扎点（在当前位置创建）
    /// </summary>
    private void PlaceGarrisonPoint()
    {
        if (GarrisonPointManager.Instance == null)
        {
            Debug.LogError("GarrisonPointPlacer: GarrisonPointManager.Instance 不存在！");
            ExitDeployMode();
            return;
        }

        GarrisonPointManager.Instance.CreateGarrisonPointOnPath(
            currentHoveredPath, currentCurveT, selectedCamps);

        ExitDeployMode();
    }

    /// <summary>
    /// 退出放置模式
    /// </summary>
    private void ExitDeployMode()
    {
        if (!isDeploying) return;

        foreach (var path in allPaths)
        {
            if (path != null)
            {
                path.SetVisible(false);
                path.SetAlpha(1f);
            }
        }

        // 隐藏悬浮圆形指示器
        ShowHoverCircle(false, Vector2.zero);

        isDeploying = false;
        currentHoveredPath = null;
        selectedCamps.Clear();
        allPaths.Clear();

        Debug.Log("退出驻扎点放置模式");
    }

    // ==================== 悬浮圆形指示器 ====================

    /// <summary>
    /// 创建悬浮圆形指示器（程序化生成，无需预制体）
    /// 使用 LineRenderer loop 模式绘制圆环
    /// </summary>
    private void CreateHoverCircle()
    {
        hoverCircleObj = new GameObject("HoverCircle");
        hoverCircleObj.SetActive(false);

        hoverCircleRenderer = hoverCircleObj.AddComponent<LineRenderer>();

        // 使用 Sprites/Default 材质
        Material mat = new Material(Shader.Find("Sprites/Default"));
        hoverCircleRenderer.material = mat;

        hoverCircleRenderer.useWorldSpace = true;
        hoverCircleRenderer.loop = true;
        hoverCircleRenderer.positionCount = hoverCircleSegments;
        hoverCircleRenderer.startWidth = hoverCircleWidth;
        hoverCircleRenderer.endWidth = hoverCircleWidth;
        hoverCircleRenderer.startColor = hoverCircleColor;
        hoverCircleRenderer.endColor = hoverCircleColor;

        UpdateCirclePoints(Vector2.zero);
    }

    /// <summary>
    /// 更新圆形指示器的顶点位置
    /// </summary>
    private void UpdateCirclePoints(Vector2 center)
    {
        float angleStep = 360f / hoverCircleSegments;
        for (int i = 0; i < hoverCircleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * hoverCircleRadius;
            float y = center.y + Mathf.Sin(angle) * hoverCircleRadius;
            hoverCircleRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    /// <summary>
    /// 显示/隐藏悬浮圆形指示器
    /// </summary>
    private void ShowHoverCircle(bool visible, Vector2 position)
    {
        if (hoverCircleObj == null) return;

        hoverCircleObj.SetActive(visible);
        if (visible)
        {
            UpdateCirclePoints(position);
        }
    }

    void OnDestroy()
    {
        if (hoverCircleObj != null)
            Destroy(hoverCircleObj);
    }
}
