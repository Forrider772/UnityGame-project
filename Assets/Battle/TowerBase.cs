using UnityEngine;

public class TowerBase : MonoBehaviour
{
    [Header("塔属性")]
    public Camp camp;
    public float hp = 800f;
    public float atk = 12f;
    public float atkRange = 3.5f;
    public float atkCD = 1.2f;

    [Header("血条")]
    public GameObject hpBarPrefab;
    private HPBar hpBar;
    private Canvas battleCanvas;

    private Transform curTarget;
    private float atkTimer;

    void Start()
    {
        battleCanvas = GameObject.FindGameObjectWithTag("BattleCanvas").GetComponent<Canvas>();

        if (hpBarPrefab != null)
        {
            GameObject bar = Instantiate(hpBarPrefab, battleCanvas.transform);
            hpBar = bar.GetComponent<HPBar>();
            hpBar.target = transform;
            hpBar.offset = new Vector3(0, 1.8f, 0);
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
        FindEnemy();
        AttackLogic();
    }

    void FindEnemy()
    {
        LayerMask mask = camp == Camp.Player
            ? LayerMask.GetMask("EnemyUnit")
            : LayerMask.GetMask("PlayerUnit");

        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, atkRange, mask);
        float minDis = 999;
        curTarget = null;
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

    void AttackLogic()
    {
        if (curTarget == null) return;
        atkTimer += Time.deltaTime;
        if (atkTimer >= atkCD)
        {
            if (curTarget.TryGetComponent(out UnitBase unit))
                unit.TakeDamage(atk);
            atkTimer = 0;
        }
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;
        hpBar?.UpdateHp(hp);
    }

    void Die()
    {
        if(camp == Camp.Enemy)
        {
            BattleManager.Instance.enemyTowerAlive = false;
        }
        else
        {
            BattleManager.Instance.playerTowerAlive = false;
        }

        if (hpBar != null) Destroy(hpBar.gameObject);
        Destroy(gameObject);
        BattleManager.Instance.CheckWin();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, atkRange);
    }
}