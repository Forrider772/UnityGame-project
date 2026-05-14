// using UnityEngine;

// public class SpawnTest : MonoBehaviour
// {
//     public GameObject playerUnitPrefab;
//     public GameObject enemyUnitPrefab;

//     public Transform playerSpawnPos;
//     public Transform enemySpawnPos;

//     // 玩家召唤单位 费用3
//     public void SpawnPlayerUnit()
//     {
//         if(BattleManager.Instance.PlayerCostUse(1))
//         {
//             Instantiate(playerUnitPrefab, playerSpawnPos.position, Quaternion.identity);
//         }
//     }

//     public void Start()
//     {
//         SpawnPlayerUnit();
//     }
//     // 敌方AI自动召唤
//     void Update()
//     {
//         if(Input.GetKeyDown(KeyCode.B))
//         {
//             if(BattleManager.Instance.PlayerCostUse(1))
//             {
//                 Instantiate(enemyUnitPrefab, enemySpawnPos.position, Quaternion.identity);
//             }
//         }
//     }
// }