using UnityEngine;

/// <summary>
/// 射手专属远程战斗模块
/// 完全不修改原有UnitCombat代码
/// 只接管攻击逻辑，其他全部复用原有系统
/// </summary>
public class ArcherCombat : MonoBehaviour
{
    [Header("远程攻击配置")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 8f;

    private UnitAttr attr;
    private UnitCombat originalCombat;
    private Transform currentTarget;
    private float attackTimer;

    void Awake()
    {
        // 获取原有组件引用
        attr = GetComponent<UnitAttr>();
        originalCombat = GetComponent<UnitCombat>();

        // ✅ 新增：关键空保护，组件缺失直接禁用脚本不报错
        if (originalCombat == null || attr == null)
        {
            Debug.LogError("ArcherCombat：缺少UnitAttr或UnitCombat组件！", this);
            enabled = false;
            return;
        }

        // 关键：冻结原有UnitCombat的攻击，让它永远不会出手
        originalCombat.enabled = false;
    }

    void Update()
    {
        // ✅ 新增：死亡或组件缺失则停止所有逻辑
        if (attr == null || attr.currentHp <= 0 || originalCombat == null) return;

        // 1. 复用原有系统的索敌结果（完全和战士一样的索敌逻辑）
        currentTarget = GetCurrentTarget();

        // 2. 有目标就执行远程攻击
        if (currentTarget != null)
        {
            AttackTarget();
        }
    }

    // ✅ 彻底删除反射！直接访问public的currentTarget（你UnitCombat里已经是public了）
    // 同时加了100%空保护，再也不会报NullReferenceException
    private Transform GetCurrentTarget()
    {
        if (originalCombat == null) return null;
        return originalCombat.currentTarget;
    }

    // 射手专属远程攻击
    private void AttackTarget()
    {
        // 目标超出攻击范围则停止攻击
        if (Vector3.Distance(transform.position, currentTarget.position) > attr.atkRange)
        {
            return;
        }

        // 自己管理攻击冷却
        attackTimer += Time.deltaTime;
        if (attackTimer < attr.atkCD) return;
        attackTimer = 0;

        // ✅ 新增：预制体空保护，没拖子弹也不会报错
        if (bulletPrefab == null)
        {
            Debug.LogWarning("ArcherCombat：未赋值子弹预制体！", this);
            return;
        }

        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        
        // ✅ 新增：Bullet组件空保护
        if (bullet.TryGetComponent(out Bullet bulletScript))
        {
            // 传递伤害参数
            bulletScript.damage = attr.atk;
            bulletScript.target = currentTarget;
            bulletScript.speed = bulletSpeed;
            bulletScript.attackType = attr.attackType;
        }
    }
}