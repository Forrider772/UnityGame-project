using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("费用设置")]
    public int playerCost = 10;
    public int enemyCost = 10;
    public int maxCost = 10;

    [Header("塔引用")]
    public TowerBase playerTower;
    public TowerBase enemyTower;

    public bool battleOver = false;

    void Awake()
    {
        Instance = this;
    }

    // 玩家扣费用
    public bool PlayerCostUse(int cost)
    {
        if (battleOver || playerCost < cost) return false;
        playerCost -= cost;
        return true;
    }

    // 敌方扣费用
    public bool EnemyCostUse(int cost)
    {
        if (battleOver || enemyCost < cost) return false;
        enemyCost -= cost;
        return true;
    }

    // 胜负检测
    public void CheckWin()
    {
        if (battleOver) return;

        if(playerTower.hp <= 0)
        {
            battleOver = true;
            Debug.Log("敌方获胜");
        }
        if(enemyTower.hp <= 0)
        {
            battleOver = true;
            Debug.Log("我方获胜");
        }
    }
}