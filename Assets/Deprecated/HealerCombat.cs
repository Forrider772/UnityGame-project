using UnityEngine;

public class HealerCombat : MonoBehaviour
{
    [Header("治疗配置")]
    public float healAmount = 25f;
    public float healRange = 10f;
    public float healCooldown = 2f;
    public GameObject healEffectPrefab;
    public float effectHeightOffset = 2f;
    public bool canHealSelf = true;

    private UnitAttr attr;
    private UnitCombat originalCombat;
    private UnitMovement movement;
    private UnitAI originalAI;
    private float healTimer;

    void Awake()
    {
        attr = GetComponent<UnitAttr>();
        originalCombat = GetComponent<UnitCombat>();
        movement = GetComponent<UnitMovement>();
        originalAI = GetComponent<UnitAI>();

        originalCombat.enabled = false;
        originalAI.enabled = false;
        healTimer = healCooldown;

        MeshRenderer[] allRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in allRenderers)
        {
            r.enabled = true;
        }
        transform.localScale = Vector3.one;
    }

    void Update()
    {
        if (attr.currentHp <= 0) return;

        healTimer -= Time.deltaTime;
        Transform lowestHpAlly = FindLowestHpAlly();

        if (lowestHpAlly != null)
        {
            Vector3 dir = (lowestHpAlly.position - transform.position).normalized;
            transform.rotation = Quaternion.Lerp(transform.rotation,
                Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)),
                10f * Time.deltaTime);

            if (healTimer <= 0)
            {
                HealTarget(lowestHpAlly);
            }
        }
        else
        {
            if (!movement.IsPathCompleted())
            {
                movement.MoveAlongPath();
            }
        }
    }

    Transform FindLowestHpAlly()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, healRange);
        Transform lowestHpTarget = null;
        float lowestHpPercent = 1f;

        foreach (Collider2D collider in colliders)
        {
            if (!canHealSelf && collider.transform == transform) continue;

            UnitAttr allyAttr = collider.GetComponent<UnitAttr>();
            if (allyAttr != null && allyAttr.currentHp > 0 && allyAttr.camp == CampType.Player)
            {
                float hpPercent = allyAttr.currentHp / allyAttr.maxHp;
                if (hpPercent < lowestHpPercent && hpPercent < 0.99f)
                {
                    lowestHpPercent = hpPercent;
                    lowestHpTarget = collider.transform;
                }
            }
        }
        return lowestHpTarget;
    }

    void HealTarget(Transform target)
    {
        healTimer = healCooldown;
        UnitAttr targetAttr = target.GetComponent<UnitAttr>();
        if (targetAttr == null) return;

        targetAttr.currentHp = Mathf.Min(targetAttr.currentHp + healAmount, targetAttr.maxHp);

        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab,
                target.position + Vector3.up * effectHeightOffset,
                Quaternion.identity);
        }
    }
}