using UnityEngine;

// 阵营枚举：区分玩家单位和敌方单位
public enum Camp
{
    Player,  // 玩家阵营
    Enemy    // 敌方阵营
}

// 攻击类型枚举：区分物理/法术攻击
public enum AttackType
{
    Physical,  // 物理攻击（战士/射手）
    Magic      // 法术攻击（法师）
}

// 单位基础脚本：控制所有士兵/怪物的移动、攻击、索敌、血量、死亡
public class UnitBase : MonoBehaviour
{
    [Header("单位阵营属性")]
    public Camp camp;             // 当前单位所属阵营
    public float hp = 120f;       // 单位血量
    public float atk = 8f;        // 单位攻击力
    public float moveSpeed = 1.8f;// 移动速度
    public float atkRange = 1.2f; // 攻击范围
    public float atkCD = 1f;      // 攻击冷却时间

    // ========== 攻击类型和双防御属性 ==========
    [Header("攻击类型与防御")]
    public AttackType attackType = AttackType.Physical; // 默认物理攻击，法师改成Magic
    public float physicalDefense = 0f;  // 物理防御（战士/射手用）
    public float magicDefense = 0f;    // 法术防御（只有法术攻击会触发减伤）

    [Header("血条")]
    public GameObject hpBarPrefab;// 血条预制体
    private HPBar hpBar;          // 血条组件引用
    private Canvas battleCanvas;   // 战斗UI画布（世界空间）

    private Transform curTarget;  // 当前锁定的攻击目标
    private float atkTimer;       // 攻击冷却计时器

    // 初始化：创建血条、绑定画布
    void Start()
    {
        // 自动寻找场景中的战斗画布
        battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas").GetComponent<Canvas>();

        // 如果有血条预制体，自动生成血条
        if (hpBarPrefab != null)
        {
            GameObject bar = Instantiate(hpBarPrefab, battleCanvas.transform);
            hpBar = bar.GetComponent<HPBar>();
            hpBar.target = transform;
            hpBar.SetMaxHp(hp);
        }
    }

    // 每帧执行：判断死亡、搜索目标、执行移动攻击
    void Update()
    {
        // 血量≤0 执行死亡
        if (hp <= 0)
        {
            Die();
            return;
        }
        // 搜索可攻击目标
        SearchTarget();
        // 执行移动或攻击逻辑
        MoveAndAttack();
    }

    // 搜索目标逻辑：优先找敌方单位，没有则攻击敌方塔
    void SearchTarget()
    {
        // 根据阵营设置检测的目标层
        LayerMask unitMask = camp == Camp.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        // 大范围检测敌方单位
        Collider2D[] unitCols = Physics2D.OverlapCircleAll(transform.position, 15f, unitMask);
        if (unitCols.Length > 0)
        {
            curTarget = unitCols[0].transform;
            return;
        }

        // ---------------- 安全判空 ----------------
        if (camp == Camp.Player)
        {
            // 敌方塔存在 → 锁定
            if (BattleManager.Instance.enemyTower != null)
            {
                curTarget = BattleManager.Instance.enemyTower.transform;
            }
            else
            {
                // 塔没了 → 置空，不报错
                curTarget = null;
            }
        }
        else
        {
            // 我方塔存在 → 锁定
            if (BattleManager.Instance.playerTower != null)
            {
                curTarget = BattleManager.Instance.playerTower.transform;
            }
            else
            {
                // 塔没了 → 置空，不报错
                curTarget = null;
            }
        }
    }

    // 移动与攻击逻辑：目标在范围外则移动，在范围内则攻击
    void MoveAndAttack()
    {
        // 无目标直接返回
        if (curTarget == null) return;

        // 计算与目标的距离
        float dis = Vector2.Distance(transform.position, curTarget.position);

        // 在攻击范围内 → 开始攻击
        if (dis <= atkRange)
        {
            atkTimer += Time.deltaTime;
            if (atkTimer >= atkCD)
            {
                // 攻击单位（把攻击类型传进去）
                if (curTarget.TryGetComponent(out UnitBase u))
                    u.TakeDamage(atk, attackType);
                // 攻击塔
                if (curTarget.TryGetComponent(out TowerBase t))
    t.TakeDamage(atk, attackType);

                atkTimer = 0;
            }
        }
        // 不在攻击范围内 → 向目标移动
        else
        {
            transform.position = Vector2.MoveTowards(
                transform.position, curTarget.position, moveSpeed * Time.deltaTime);
        }
    }

    // 带攻击类型的受伤害逻辑：区分物理/法术减伤
    public void TakeDamage(float dmg, AttackType attackerType)
    {
        float finalDamage = dmg;

        if (attackerType == AttackType.Physical)
        {
            // 物理攻击 → 用物理防御减伤
            finalDamage = Mathf.Max(1f, dmg - physicalDefense);
            Debug.Log($"{gameObject.name} 受到【物理伤害】：原始伤害{dmg} | 物理防御{physicalDefense} | 最终扣血{finalDamage}");
        }
        else if (attackerType == AttackType.Magic)
        {
            // 法术攻击 → 用法术防御减伤
            finalDamage = Mathf.Max(1f, dmg - magicDefense);
            Debug.Log($"{gameObject.name} 受到【法术伤害】：原始伤害{dmg} | 法术防御{magicDefense} | 最终扣血{finalDamage}");
        }

        hp -= finalDamage;
        hpBar?.UpdateHp(hp);
    }

    // 死亡：销毁血条 + 销毁自身
    void Die()
    {
        if (hpBar != null) Destroy(hpBar.gameObject);
        Destroy(gameObject);
    }
}