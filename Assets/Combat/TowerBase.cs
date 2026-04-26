using UnityEngine;

public enum Camp
{
    Player,
    Enemy
}

public class TowerBase : MonoBehaviour
{
    [Header("塔属性")]
    public Camp camp;
    public float hp = 800f;
    public float atk = 12f;
    public float atkRange = 3.5f;
    public float atkCD = 1.2f;

    private Transform curTarget;
    private float atkTimer;

    void Update()
    {
        if(hp <= 0)
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

        foreach(var col in cols)
        {
            float dis = Vector2.Distance(transform.position, col.transform.position);
            if(dis < minDis)
            {
                minDis = dis;
                curTarget = col.transform;
            }
        }
    }

    void AttackLogic()
    {
        if(curTarget == null) return;

        atkTimer += Time.deltaTime;
        if(atkTimer >= atkCD)
        {
            if(curTarget.TryGetComponent(out UnitBase unit))
            {
                unit.TakeDamage(atk);
            }
            atkTimer = 0;
        }
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;
    }

    void Die()
    {
        Destroy(gameObject);
        BattleManager.Instance.CheckWin();
    }

    // 范围可视化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, atkRange);
    }
}