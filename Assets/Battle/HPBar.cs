using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    [Header("头顶偏移")]
    public Vector3 offset = new Vector3(0, 1.1f, 0);

    public Image fillImg;
    private float maxHp;

    // 初始化最大血量
    public void SetMaxHp(float value)
    {
        maxHp = value;
    }

    // 刷新当前血量
    public void UpdateHp(float current)
    {
        float percent = current / maxHp;
        fillImg.fillAmount = percent;
        // 低血量变红
        fillImg.color = percent < 0.3f ? Color.red : Color.green;
    }

    // 每帧跟随物体头顶
    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}