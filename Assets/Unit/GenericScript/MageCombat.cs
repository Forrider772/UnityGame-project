using UnityEngine;

/// <summary>
/// 法师专属远程战斗模块
/// 零基础代码修改，无动画依赖
/// </summary>
public class MageCombat : MonoBehaviour
{
    [Header("远程攻击配置")]
    public GameObject fireballPrefab;
    public float fireballSpeed = 8f;
    [Tooltip("火球发射点高度偏移")]
    public float spawnHeightOffset = 1.5f;

    private UnitAttr attr;
    private UnitCombat originalCombat;
    private float attackTimer;

    void Awake()
    {
        attr = GetComponent<UnitAttr>();
        originalCombat = GetComponent<UnitCombat>();

        // ✅ 新增：组件缺失保护，直接禁用脚本不崩溃
        if (originalCombat == null || attr == null)
        {
            Debug.LogError("MageCombat：缺少UnitAttr或UnitCombat组件！", this);
            enabled = false;
            return;
        }

        // 冻结原有攻击逻辑
        originalCombat.enabled = false;
    }

    void Update()
    {
        // ✅ 新增：死亡/组件缺失直接返回
        if (attr == null || attr.currentHp <= 0 || originalCombat == null) return;

        // ✅ 彻底删除反射！直接访问public的currentTarget
        Transform currentTarget = originalCombat.currentTarget;
        if (currentTarget == null) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0 && Vector3.Distance(transform.position, currentTarget.position) <= attr.atkRange)
        {
            Attack(currentTarget);
        }
    }

    void Attack(Transform target)
    {
        attackTimer = attr.atkCD;
        // 直接发射火球，去掉了所有动画调用
        Invoke(nameof(LaunchFireball), 0.15f);
    }

    void LaunchFireball()
    {
        Transform currentTarget = originalCombat.currentTarget;
        // ✅ 新增：目标消失/火球预制体未赋值 直接返回
        if (currentTarget == null || fireballPrefab == null) return;

        GameObject fireball = Instantiate(
            fireballPrefab,
            transform.position + Vector3.up * spawnHeightOffset,
            Quaternion.identity
        );

        // ✅ 新增：Bullet组件空保护
        if (fireball.TryGetComponent(out Bullet bullet))
        {
            bullet.damage = attr.atk;
            bullet.target = currentTarget;
            bullet.speed = fireballSpeed;
            bullet.attackType = AttackType.Magic;
        }
    }
}