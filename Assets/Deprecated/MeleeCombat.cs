// // 战士、近战单位
// using UnityEngine;

// public class MeleeCombat : UnitBase
// {
//     protected override void Update()
//     {
//         if (isDead) return;
//         if (!TargetValid()) FindEnemy();

//         if (target != null)
//         {
//             StopMove();
//             Attack();
//         }
//         else ResumeMove();
//     }

//     void Attack()
//     {
//         attackTimer += Time.deltaTime;
//         if (attackTimer >= attackCD)
//         {
//             attackTimer = 0;
//             target.GetComponent<UnitBase>().TakeDamage(attack);
//         }
//     }
// }