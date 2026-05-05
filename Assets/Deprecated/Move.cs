using UnityEngine;

public class Move : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2f; // 敌人移动速度
    public Transform moveTarget;    // 目标位置

    void Update()
    {
        MoveTowardsPlayer();
    }

    // 朝目标位置移动
    void MoveTowardsPlayer()
    {
        if (moveTarget == null) return;

        // 计算方向：目标位置 - 自己位置，得到方向向量
        Vector2 direction = (moveTarget.position - transform.position).normalized;

        // 移动：Rigidbody2D方式（推荐，物理更稳定）
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction * moveSpeed;
        }
        else
        {
            // 没有刚体时，用transform移动（简单但不推荐，容易穿模）
            transform.Translate(direction * moveSpeed * Time.deltaTime);
        }

    }
}