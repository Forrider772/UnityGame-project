using System.Collections;
using UnityEngine;

/// <summary>
/// 单位视觉反馈组件
/// 提供受击闪红等视觉效果，挂载在单位 GameObject 上。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UnitVisual : MonoBehaviour
{
    [Header("=== 受击反馈 ===")]
    [Tooltip("受击闪红持续时间（秒）")]
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashRoutine;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    /// <summary>
    /// 受击闪红
    /// </summary>
    public void FlashRed()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        sr.color = originalColor;
        flashRoutine = null;
    }
}
