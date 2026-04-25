using UnityEngine;

public class Enemy : MonoBehaviour
{
    public delegate void DeathAction();
    public event DeathAction OnDeath;

    // 敌人死亡时调用这个方法
    public void Die()
    {
        OnDeath?.Invoke(); // 触发死亡事件
        Destroy(gameObject);
    }

    // 举个例子：碰到玩家就死亡（你可以改成其他条件）
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Die();
        }
    }
}