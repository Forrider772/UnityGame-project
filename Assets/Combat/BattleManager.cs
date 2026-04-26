using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [Header("双塔引用")]
    public GameObject playerTower;
    public GameObject enemyTower;

    // 单独标记双塔是否存活
    public bool playerTowerAlive = true;
    public bool enemyTowerAlive = true;
    [Header("费用")]
    public int nowCost;
    public int maxCost = 10;
    public float costAddSpeed = 1f;

    public bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        // 游戏结束就不再执行任何逻辑
        if (isGameOver) return;

        // 费用自动增加
        CostAdd();
    }

    // 费用增加
    void CostAdd()
    {
        if(nowCost < maxCost)
        {
            nowCost += (int)(Time.deltaTime * costAddSpeed);
        }
    }

    // 扣费用
    public bool PlayerCostUse(int cost)
    {
        if (nowCost >= cost)
        {
            nowCost -= cost;
            return true;
        }
        return false;
    }

    // 胜负判定核心
    public void CheckWin()
    {
        if (isGameOver) return;

        // 敌方塔销毁 = 玩家胜利
        if (!enemyTowerAlive)
        {
            GameWin();
        }
        // 我方塔销毁 = 失败
        else if (!playerTowerAlive)
        {
            GameLose();
        }
    }

    // 胜利逻辑
    void GameWin()
    {
        isGameOver = false;
        Debug.Log("===== 游戏胜利 =====");
        
        // 1. 暂停所有游戏逻辑
        Time.timeScale = 0;

        // 后续你可以加：胜利弹窗、跳转场景
    }

    // 失败逻辑
    void GameLose()
    {
        isGameOver = true;
        Debug.Log("===== 游戏失败 =====");
        
        Time.timeScale = 0;
    }
}