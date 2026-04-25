using UnityEngine;

// 所有单位的父类：战士、射手、法师 都继承这个
public class UnitBase : MonoBehaviour
{
    [Header("基础属性")]
    public int maxHp = 100;
    public int attack = 10;
    public float attackCD = 1f;
    public float searchRange = 2f;

    [Header("阵营")]
    public bool isEnemy;

    protected int currentHp;
    protected bool isDead;
    protected GameObject target;
    protected float attackTimer;

    protected MonoBehaviour moveScript;

    protected virtual void Start()
    {
        currentHp = maxHp;
        moveScript = GetComponent<MonoBehaviour>();
    }

    protected virtual void Update(){}
    // 受伤
    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHp -= damage;
        if (currentHp <= 0) Die();
    }

    // 死亡
    protected virtual void Die()
    {
        isDead = true;
        Destroy(gameObject, 0.01f);
    }

    // 寻找敌人（所有职业共用）
    protected bool FindEnemy()
    {
        target = null;
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, searchRange);

        foreach (var c in cols)
        {
            UnitBase unit = c.GetComponent<UnitBase>();
            if (unit == null || unit.isDead) continue;
            if (unit.isEnemy != isEnemy)
            {
                target = c.gameObject;
                return true;
            }
        }
        return false;
    }

    // 移动控制
    protected void StopMove()
    {
        if (moveScript != null)
            moveScript.enabled = false;

        // 关键：强行停止刚体惯性
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if(rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }
    protected void ResumeMove() => moveScript.enabled = true;

    // 目标是否有效
    protected bool TargetValid()
    {
        if (target == null) return false;
        UnitBase u = target.GetComponent<UnitBase>();
        return u != null && !u.isDead;
    }
}