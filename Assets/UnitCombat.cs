using UnityEngine;

/// <summary>
/// 敌我战斗核心脚本
/// 挂载：友方、敌方全部挂载
/// 阵营：isEnemy 勾选=敌方，不勾=友方
/// </summary>
public class UnitCombat : MonoBehaviour
{
    [Header("战斗属性")]
    public int maxHp = 100;        // 最大血量
    public int atkDamage = 15;    // 单次攻击伤害
    public float attackCD = 1f;   // 攻击间隔
    public float searchRange = 1.8f;// 索敌范围

    [Header("阵营设置")]
    public bool isEnemy;

    // 私有数据
    private int _currentHp;
    private float _attackTimer;
    private GameObject _lockTarget;  // 当前锁定的敌人
    private bool _isDead;

    // 移动脚本缓存（战斗开关移动用）
    private MonoBehaviour _moveLogic;


    void Start()
    {
        // 初始化血量
        _currentHp = maxHp;
        _isDead = false;

        // 自动获取自身移动脚本(UnitMove)
        _moveLogic = GetComponent<MonoBehaviour>();
    }


    void Update()
    {
        // 已死亡 → 直接停止所有逻辑
        if (_isDead) return;

        // 【关键】检测锁定目标是否已销毁/失效，失效就清空
        if (!TargetIsValid())
        {
            _lockTarget = null;
        }

        // 搜寻附近敌人
        SearchEnemy();

        // 有合法目标 → 停下移动+攻击
        if (_lockTarget != null)
        {
            StopMove();
            AttackLogic();
        }
        // 无目标 → 恢复正常移动
        else
        {
            OpenMove();
        }
    }


    // 1. 搜寻范围内对立阵营
    void SearchEnemy()
    {
        // 圆形范围检测所有碰撞体
        Collider2D[] allColl = Physics2D.OverlapCircleAll(transform.position, searchRange);

        foreach (var col in allColl)
        {
            // 获取对方战斗组件
            UnitCombat foe = col.GetComponent<UnitCombat>();

            // 过滤：没有战斗组件 / 已经死亡 → 跳过
            if (foe == null || foe._isDead) continue;

            // 阵营不同 = 敌人
            if (foe.isEnemy != this.isEnemy)
            {
                _lockTarget = col.gameObject;
                return;
            }
        }

        // 没找到敌人，清空目标
        _lockTarget = null;
    }


    // 2. 攻击计时+造成伤害
    void AttackLogic()
    {
        _attackTimer += Time.deltaTime;

        // 冷却结束，发起攻击
        if (_attackTimer >= attackCD)
        {
            _attackTimer = 0;
            // 对目标造成伤害
            _lockTarget.GetComponent<UnitCombat>().TakeDamage(atkDamage);
        }
    }


    // 3. 受伤逻辑
    public void TakeDamage(int dmg)
    {
        if (_isDead) return;

        // 扣血
        _currentHp -= dmg;

        // 血量归零 = 死亡
        if (_currentHp <= 0)
        {
            Die();
        }
    }


    // 4. 死亡逻辑
    void Die()
    {
        _isDead = true;
        // 延迟销毁一帧，防止瞬间引用报错
        Destroy(gameObject, 0.01f);
    }


    // 5. 判断当前锁定目标是否有效（核心防报错）
    private bool TargetIsValid()
    {
        if (_lockTarget == null) 
            return false;

        // 物体已被销毁 → 无效
        if (_lockTarget.gameObject == null) 
            return false;

        // 目标已死亡 → 无效
        UnitCombat foe = _lockTarget.GetComponent<UnitCombat>();
        if (foe == null || foe._isDead) 
            return false;

        return true;
    }


    // 关闭移动（战斗时）
    void StopMove()
    {
        if (_moveLogic != null)
            _moveLogic.enabled = false;
    }

    // 开启移动（战斗结束）
    void OpenMove()
    {
        if (_moveLogic != null)
            _moveLogic.enabled = true;
    }


    // 场景窗口显示索敌范围，方便调节
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRange);
    }
}
