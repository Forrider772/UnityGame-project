using UnityEngine;

/// <summary>
/// 单位UI模块
/// 职责：专门管理单位所有UI表现
/// 目前只负责：血条生成、血量刷新、血条销毁
/// 与战斗逻辑完全解耦，后期可扩展buff图标、状态图标
/// </summary>
public class UnitUI : MonoBehaviour
{
    private UnitAttr attr;     // 自身属性引用
    private HPBar hpBar;       // 血条组件引用
    private Canvas battleCanvas;// 场景战斗UI画布

    /// <summary>
    /// 自动获取同物体属性组件
    /// </summary>
    void Awake()
    {
        attr = GetComponent<UnitAttr>();
    }

    /// <summary>
    /// 初始化生成血条
    /// </summary>
    void Start()
    {
        InitHpBar();
    }

    /// <summary>
    /// 实例化血条并绑定跟随
    /// </summary>
    void InitHpBar()
    {
        // 没有血条预制体直接返回
        if (attr.hpBarPrefab == null) return;

        // 查找全局战斗画布
        battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas").GetComponent<Canvas>();
        // 实例化血条到画布下
        GameObject barObj = Instantiate(attr.hpBarPrefab, battleCanvas.transform);

        hpBar = barObj.GetComponent<HPBar>();
        // 绑定跟随目标和最大血量
        hpBar.target = transform;
        hpBar.SetMaxHp(attr.maxHp);
    }

    /// <summary>
    /// 刷新血条当前血量
    /// </summary>
    /// <param name="curHp">当前血量值</param>
    public void RefreshHp(float curHp)
    {
        // 有血条才刷新
        hpBar?.UpdateHp(curHp);
    }

    /// <summary>
    /// 销毁血条物体
    /// </summary>
    public void DestroyHpBar()
    {
        if (hpBar != null)
        {
            Destroy(hpBar.gameObject);
            hpBar = null;
        }
    }
}