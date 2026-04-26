using UnityEngine;

public class UnitBase : MonoBehaviour
{
    [Header("单位阵营属性")]
    public Camp camp;
    public float hp = 120f;
    public float atk = 8f;
    public float moveSpeed = 1.8f;
    public float atkRange = 1.2f;
    public float atkCD = 1f;

    private Transform curTarget;
    private float atkTimer;

    void Update()
    {
        if(hp <= 0)
        {
            Die();
            return;
        }

        SearchTarget();
        MoveAndAttack();
    }

    // 搜寻目标：敌方单位 > 敌方主塔
    void SearchTarget()
    {
        LayerMask unitMask = camp == Camp.Player 
            ? LayerMask.GetMask("EnemyUnit") 
            : LayerMask.GetMask("PlayerUnit");

        Collider2D[] unitCols = Physics2D.OverlapCircleAll(transform.position, 15f, unitMask);
        
        if(unitCols.Length > 0)
        {
            curTarget = unitCols[0].transform;
            return;
        }

        // 无单位就锁定敌方主塔
        curTarget = camp == Camp.Player 
            ? BattleManager.Instance.enemyTower.transform 
            : BattleManager.Instance.playerTower.transform;
    }

    void MoveAndAttack()
    {
        if(curTarget == null) return;

        float dis = Vector2.Distance(transform.position, curTarget.position);
        if(dis <= atkRange)
        {
            // 范围内：攻击
            atkTimer += Time.deltaTime;
            if(atkTimer >= atkCD)
            {
                if(curTarget.TryGetComponent(out UnitBase u))
                    u.TakeDamage(atk);
                if(curTarget.TryGetComponent(out TowerBase t))
                    t.TakeDamage(atk);

                atkTimer = 0;
            }
        }
        else
        {
            // 范围外：向目标移动
            transform.position = Vector2.MoveTowards(
                transform.position, curTarget.position, moveSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}