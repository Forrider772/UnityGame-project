using UnityEngine;

/// <summary>
/// 单位战斗模块
/// 职责：只处理战斗核心逻辑
/// 1. 搜寻敌方单位
/// 2. 锁定防御塔目标
/// 3. 攻击追击、伤害结算
/// 4. 受击减伤、死亡判定
/// 完全剥离UI逻辑，只调用UI接口不实现UI
/// </summary>
public class UnitCombat : MonoBehaviour
{
    private UnitAttr attr;         // 属性引用
    private UnitMovement movement; // 移动模块引用
    private UnitUI unitUI;         // UI模块引用

    private Transform currentTarget;// 当前攻击目标
    private float attackTimer;     // 攻击冷却计时器

    /// <summary>
    /// 自动获取同物体所有依赖组件
    /// </summary>
    void Awake()
    {
        attr = GetComponent<UnitAttr>();
        movement = GetComponent<UnitMovement>();
        unitUI = GetComponent<UnitUI>();

        // 初始化当前血量为最大血量
        attr.currentHp = attr.maxHp;
    }

    /// <summary>
    /// 搜寻范围内敌方战斗单位（只找小兵/怪物，不找塔）
    /// </summary>
    /// <returns>是否搜到有效敌人</returns>
    public bool DetectEnemy()
    {
        // 根据阵营确定检测层级
        LayerMask targetMask = attr.camp == Camp.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        // 圆形范围检测
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attr.detectRange, targetMask);

        if (hits.Length > 0)
        {
            currentTarget = hits[0].transform;
            return true;
        }

        // 无敌人清空目标
        currentTarget = null;
        return false;
    }

    /// <summary>
    /// 路径走完后，锁定对方防御塔作为攻击目标
    /// </summary>
    public void SetTargetToTower()
    {
        // ---------------- 安全判空 ----------------
        if (attr.camp == Camp.Player)
        {
            // 敌方塔存在 → 锁定
            if (BattleManager.Instance.enemyTower != null)
            {
                currentTarget = BattleManager.Instance.enemyTower.transform;
            }
            else
            {
                // 塔没了 → 置空，不报错
                currentTarget = null;
            }
        }
        else
        {
            // 我方塔存在 → 锁定
            if (BattleManager.Instance.playerTower != null)
            {
                currentTarget = BattleManager.Instance.playerTower.transform;
            }
            else
            {
                // 塔没了 → 置空，不报错
                currentTarget = null;
            }
        }
    }

    /// <summary>
    /// 尝试攻击逻辑：范围外追击，范围内普攻
    /// </summary>
    public void TryAttack()
    {
        // 无目标直接返回
        if (currentTarget == null) return;

        // 计算与目标距离
        float distance = Vector2.Distance(transform.position, currentTarget.position);

        // 超出攻击范围 → 向目标追击移动
        if (distance > attr.atkRange)
        {
            movement.MoveToTarget(currentTarget.position);
            return;
        }

        // 攻击冷却计时
        attackTimer += Time.deltaTime;
        // 冷却结束发起攻击
        if (attackTimer >= attr.atkCD)
        {
            DealDamageToTarget();
            attackTimer = 0;
        }
    }

    /// <summary>
    /// 对当前目标造成伤害（兼容单位和防御塔）
    /// </summary>
    void DealDamageToTarget()
    {
        // 攻击敌方单位
        if (currentTarget.TryGetComponent<UnitCombat>(out var unit))
        {
            unit.TakeDamage(attr.atk, attr.attackType);
        }
        // 攻击防御塔
        if (currentTarget.TryGetComponent<TowerBase>(out var tower))
        {
            tower.TakeDamage(attr.atk, attr.attackType);
        }
    }

    /// <summary>
    /// 受伤害对外接口
    /// 自动区分物理/法术防御结算减伤
    /// </summary>
    /// <param name="damage">原始伤害</param>
    /// <param name="type">伤害类型</param>
    public void TakeDamage(float damage, AttackType type)
    {
        float finalDmg = damage;

        // 物理伤害结算物理防御
        if (type == AttackType.Physical){
            finalDmg = Mathf.Max(1f, damage - attr.physicalDefense);
            Debug.Log($"{gameObject.name} 受到【物理伤害】：原始伤害{damage} | 物理防御{attr.physicalDefense} | 最终扣血{finalDmg}");
        }    
        // 法术伤害结算法术防御
        else if (type == AttackType.Magic){
            finalDmg = Mathf.Max(1f, damage - attr.magicDefense);
            Debug.Log($"{gameObject.name} 受到【法术伤害】：原始伤害{damage} | 法术防御{attr.magicDefense} | 最终扣血{finalDmg}");
        }

        // 扣除血量
        attr.currentHp -= finalDmg;

        // 调用UI刷新血条
        unitUI.RefreshHp(attr.currentHp);

        // 血量低于0 触发死亡
        if (attr.currentHp <= 0)
            Die();
    }

    /// <summary>
    /// 死亡逻辑
    /// </summary>
    void Die()
    {
        // 交给UI销毁血条
        unitUI.DestroyHpBar();
        // 销毁自身物体
        Destroy(gameObject);
    }

    /// <summary>
    /// 编辑器绘制索敌范围辅助线
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (attr == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attr.detectRange);
    }
}