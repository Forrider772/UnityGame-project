using UnityEngine;
using UnityEngine.UI;

// 血条UI控制脚本：负责血条跟随目标、血量更新、颜色变化
public class HPBar : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target; // 血条需要跟随的目标物体（单位/塔）

    [Header("头顶偏移")]
    public Vector3 offset = new Vector3(0, 1.1f, 0); // 血条相对于目标头顶的偏移量

    public Image fillImg; // 血条填充图片（负责显示血量比例）
    private float maxHp;  // 最大血量值

    // 设置最大血量（初始化调用）
    public void SetMaxHp(float value)
    {
        maxHp = value; // 存储最大血量
    }

    // 更新当前血量显示
    public void UpdateHp(float current)
    {
        // 计算当前血量百分比
        float percent = current / maxHp;
        // 设置填充图片的填充量，直观显示血量
        fillImg.fillAmount = percent;

        // 血量低于30%时血条变为红色，否则为绿色
        fillImg.color = percent < 0.3f ? Color.red : Color.green;
    }

    // 每帧最后执行：保证血条跟随平滑，不出现抖动
    void LateUpdate()
    {
        // 如果目标为空（已销毁），直接停止执行
        if (target == null) return;

        // 更新血条位置，始终跟随在目标头顶偏移位置
        transform.position = target.position + offset;
    }
}