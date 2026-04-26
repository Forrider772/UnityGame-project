using UnityEngine;

public enum Camp
{
    Player,
    Enemy
}

public class UnitBase : MonoBehaviour
{
    [Header("单位阵营属性")]
    public Camp camp;
    public float hp = 120f;
    public float atk = 8f;
    public float moveSpeed = 1.8f;
    public float atkRange = 1.2f;
    public float atkCD = 1f;

    [Header("血条")]
    public GameObject hpBarPrefab;
    private HPBar hpBar;
    private Canvas battleCanvas;

    private Transform curTarget;
    private float atkTimer;

    void Start()
    {
        // 自动寻找战斗画布
        battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas").GetComponent<Canvas>();

        // 自动创建血条
        if (hpBarPrefab != null)
        {
            GameObject bar = Instantiate(hpBarPrefab, battleCanvas.transform);
            hpBar = bar.GetComponent<HPBar>();
            hpBar.target = transform;
            hpBar.SetMaxHp(hp);
        }
    }

    void Update()
    {
        if (hp <= 0)
        {
            Die();
            return;
        }
        SearchTarget();
        MoveAndAttack();
    }

    void SearchTarget()
    {
        // 游戏结束直接不找目标
        if (BattleManager.Instance.isGameOver)
        {
            curTarget = null;
            return;
        }

        LayerMask unitMask = camp == Camp.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

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

    void MoveAndAttack()
    {
        if (curTarget == null) return;
        float dis = Vector2.Distance(transform.position, curTarget.position);

        if (dis <= atkRange)
        {
            atkTimer += Time.deltaTime;
            if (atkTimer >= atkCD)
            {
                if (curTarget.TryGetComponent(out UnitBase u))
                    u.TakeDamage(atk);
                if (curTarget.TryGetComponent(out TowerBase t))
                    t.TakeDamage(atk);
                atkTimer = 0;
            }
        }
        else
        {
            transform.position = Vector2.MoveTowards(
                transform.position, curTarget.position, moveSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;
        hpBar?.UpdateHp(hp);
    }

    void Die()
    {
        if (hpBar != null) Destroy(hpBar.gameObject);
        Destroy(gameObject);
    }
}