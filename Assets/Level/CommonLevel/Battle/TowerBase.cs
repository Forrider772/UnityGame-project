using UnityEngine;

// 防御塔基础脚本：控制塔的血量、攻击、索敌、死亡、胜负判定
public class TowerBase : MonoBehaviour
{
    [Header("塔属性")]
    public CampType camp;               // 塔所属阵营（玩家/敌人）
    public float hp = 800f;         // 当前血量
    public float atk = 12f;         // 攻击力
    public float atkRange = 3.5f;   // 攻击范围
    public float atkCD = 1.2f;      // 攻击冷却时间
    [Header("塔防御")]
    public float physicalDefense = 0f;
    public float magicDefense = 0f;
    [Header("血条")]
    public GameObject hpBarPrefab;  // 血条预制体
    private HPBar hpBar;            // 实例化后的血条组件
    private Canvas battleCanvas;    // 战斗UI画布（WorldSpace）

    private Transform curTarget;    // 当前锁定的攻击目标
    private float atkTimer;         // 攻击冷却计时器

    // 初始化：生成血条、绑定UI画布
    void Start()
    {
        // 查找场景中的战斗画布
        battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas").GetComponent<Canvas>();

        // 如果有血条预制体，则生成血条并初始化
        if (hpBarPrefab != null)
        {
            GameObject bar = Instantiate(hpBarPrefab, battleCanvas.transform);
            hpBar = bar.GetComponent<HPBar>();
            hpBar.target = transform;
            hpBar.offset = new Vector3(0, 1.8f, 0);
            hpBar.SetMaxHp(hp);
        }
    }

    // 每帧执行：判断死亡、搜索敌人、执行攻击逻辑
    void Update()
    {
        // 血量小于等于0，执行死亡并退出
        if (hp <= 0)
        {
            Die();
            return;
        }
        // 搜索范围内的敌人
        FindEnemy();
        // 执行攻击冷却与攻击
        AttackLogic();
    }

    // 搜索范围内最近的敌方目标
    void FindEnemy()
    {
        // 根据阵营设置检测层（只检测敌方单位）
        LayerMask mask = camp == CampType.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        // 圆形范围检测所有目标
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, atkRange, mask);
        float minDis = 999;
        curTarget = null;

        // 遍历所有目标，找到最近的一个
        foreach (var col in cols)
        {
            float dis = Vector2.Distance(transform.position, col.transform.position);
            if (dis < minDis)
            {
                minDis = dis;
                curTarget = col.transform;
            }
        }
    }

    // 攻击逻辑：冷却计时、造成伤害
    void AttackLogic()
    {
        // 没有目标直接退出
        if (curTarget == null) return;

        // 冷却计时
        atkTimer += Time.deltaTime;

        // 冷却完成，发起攻击
        if (atkTimer >= atkCD)
        {
            // 对目标单位造成伤害
            if (curTarget.TryGetComponent(out UnitCombat unit))
    unit.TakeDamage(atk, AttackType.Physical);

            // 重置计时器
            atkTimer = 0;
        }
    }

// 受到伤害：区分物理/法术防御
public void TakeDamage(float dmg, AttackType type)
{
    float finalDmg = dmg;
    if(type == AttackType.Physical)
    {
        finalDmg = Mathf.Max(1, dmg - physicalDefense);
    }
    else if(type == AttackType.Magic)
    {
        finalDmg = Mathf.Max(1, dmg - magicDefense);
    }
    hp -= finalDmg;
    hpBar?.UpdateHp(hp);
}

// 兼容旧调用，不报错
public void TakeDamage(float dmg)
{
    TakeDamage(dmg, AttackType.Physical);
}

    // 死亡逻辑：标记胜负状态、销毁血条、销毁自身、触发胜负判定
    void Die()
    {
        // 标记对应阵营的塔已死亡
        if(camp == CampType.Enemy)
        {
            BattleManager.Instance.enemyTowerAlive = false;
        }
        else
        {
            BattleManager.Instance.playerTowerAlive = false;
        }

        // 销毁血条
        if (hpBar != null) Destroy(hpBar.gameObject);
        // 销毁自身
        Destroy(gameObject);
        // 通知战斗管理器检测胜负
        BattleManager.Instance.CheckWin();
    }

    // 编辑器中绘制攻击范围辅助线
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, atkRange);
    }
}