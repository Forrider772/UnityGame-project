using UnityEngine;

/// <summary>
/// 驻扎点视觉组件
/// 负责显示驻扎点的 Sprite 图标、碰撞体检测区域、
/// 以及根据占领状态和生效阵营切换颜色
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class GarrisonPointVisual : MonoBehaviour
{
    [Header("颜色配置")]
    public Color unoccupiedColor = new Color(1f, 1f, 1f, 0.5f);   // 无人占领：半透明白色
    public Color playerOccupiedColor = new Color(0.3f, 1f, 0.3f, 0.9f); // Player占领：绿色
    public Color enemyOccupiedColor = new Color(1f, 0.3f, 0.3f, 0.9f);  // Enemy占领：红色
    public Color contestedColor = new Color(1f, 1f, 0.3f, 0.9f);  // 争夺中：黄色

    [Header("脉动配置")]
    public float pulseSpeed = 4f;
    public float pulseMinAlpha = 0.5f;
    public float pulseMaxAlpha = 1f;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private GarrisonPoint garrisonPoint;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
        garrisonPoint = GetComponent<GarrisonPoint>();

        // 配置碰撞体为 Trigger
        circleCollider.isTrigger = true;
    }

    void Start()
    {
        // 同步碰撞体半径
        if (garrisonPoint != null)
            circleCollider.radius = garrisonPoint.garrisonRange;

        ApplyColor();
    }

    void Update()
    {
        if (garrisonPoint == null) return;

        // 同步位置到驻扎点的世界坐标
        transform.position = garrisonPoint.worldPosition;

        // 同步碰撞体半径（支持运行时修改）
        circleCollider.radius = garrisonPoint.garrisonRange;

        // 更新颜色
        ApplyColor();
    }

    /// <summary>
    /// 根据当前占领/争夺状态应用颜色
    /// </summary>
    void ApplyColor()
    {
        if (garrisonPoint == null) return;

        Color targetColor;

        if (garrisonPoint.isContested)
        {
            // 争夺中：黄色脉动
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            targetColor = contestedColor;
            targetColor.a = alpha;
        }
        else if (garrisonPoint.occupyingCamp == null)
        {
            // 无人占领：半透明白色
            targetColor = unoccupiedColor;
        }
        else if (garrisonPoint.occupyingCamp == CampType.Player)
        {
            targetColor = playerOccupiedColor;
        }
        else
        {
            targetColor = enemyOccupiedColor;
        }

        spriteRenderer.color = targetColor;
    }

    void OnDrawGizmos()
    {
        // 编辑器下绘制范围指示圈
        if (garrisonPoint == null) return;

        Gizmos.color = garrisonPoint.occupyingCamp == null ? Color.gray
            : garrisonPoint.occupyingCamp == CampType.Player ? Color.green : Color.red;
        if (garrisonPoint.isContested) Gizmos.color = Color.yellow;

        Vector3 pos = Application.isPlaying ? (Vector3)garrisonPoint.worldPosition : transform.position;
        Gizmos.DrawWireSphere(pos, garrisonPoint.garrisonRange);
    }
}
