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

    //额外死亡条件(可选)
    
}