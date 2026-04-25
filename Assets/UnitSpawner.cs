using UnityEngine;
using UnityEngine.UI;

public class UnitSpawner : MonoBehaviour
{
    public GameObject unitPrefab;

    public Transform spawnPoint;

    public Transform moveTarget;

    public Button unitSpawnButton;

    // 随机偏移的范围，x和y分别对应水平和垂直的随机范围
    public Vector2 randomRange = new Vector2(1f, 1f);
    
    void Start()
    {
        unitSpawnButton.onClick.AddListener(SpawnUnit);
    }
    public void SpawnUnit()
    {
        // 生成随机偏移量
        float randomX = Random.Range(-randomRange.x, randomRange.x);
        float randomY = Random.Range(-randomRange.y, randomRange.y);
        // 计算本次生成的位置：初始位置 + 随机偏移
        Vector3 spawnPos = spawnPoint.position + new Vector3(randomX, randomY, 0);
        // 生成物体
        GameObject newUnit = Instantiate(unitPrefab, spawnPos, Quaternion.identity);
        
        // 设置目标位置
        newUnit.GetComponent<Move>().moveTarget = moveTarget;
    }
}
