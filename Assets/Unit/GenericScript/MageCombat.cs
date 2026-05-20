using UnityEngine;
using System.Reflection;

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
    private FieldInfo currentTargetField;
    private float attackTimer;

    void Awake()
    {
        attr = GetComponent<UnitAttr>();
        originalCombat = GetComponent<UnitCombat>();
        
        // 冻结原有攻击逻辑
        originalCombat.enabled = false;
        
        // 反射获取目标
        currentTargetField = typeof(UnitCombat).GetField("currentTarget", 
            BindingFlags.Instance | BindingFlags.NonPublic);
    }

    void Update()
    {
        if (attr.currentHp <= 0) return;

        Transform currentTarget = (Transform)currentTargetField.GetValue(originalCombat);
        if (currentTarget != null)
        {
            attackTimer -= Time.deltaTime;
            
            if (attackTimer <= 0 && 
                Vector3.Distance(transform.position, currentTarget.position) <= attr.atkRange)
            {
                Attack(currentTarget);
            }
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
        Transform currentTarget = (Transform)currentTargetField.GetValue(originalCombat);
        if (currentTarget == null) return;

        GameObject fireball = Instantiate(fireballPrefab, 
            transform.position + Vector3.up * spawnHeightOffset, 
            Quaternion.identity);

        Bullet bullet = fireball.GetComponent<Bullet>();
        bullet.damage = attr.atk;
        bullet.target = currentTarget;
        bullet.speed = fireballSpeed;
        bullet.attackType = AttackType.Magic;
    }
}