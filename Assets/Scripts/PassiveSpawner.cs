using UnityEngine;
using System.Collections.Generic;

public class PassiveSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    // 🟢 เปลี่ยนเป็น Array เพื่อให้ใส่ Prefab ได้หลายแบบ
    public GameObject[] npcPrefabs;      
    public int maxNPCs = 3;             
    public float minSpawnTime = 3f;     
    public float maxSpawnTime = 7f;     

    private List<GameObject> spawnedNPCs = new List<GameObject>();
    private float spawnTimer;

    void Start()
    {
        SetNextSpawnTime();
    }

    void Update()
    {
        spawnedNPCs.RemoveAll(item => item == null);

        if (spawnedNPCs.Count < maxNPCs)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                SpawnNPC();
                SetNextSpawnTime();
            }
        }
    }

    void SpawnNPC()
    {
        // 🟢 เช็คว่ามี Prefab ให้สุ่มไหม
        if (npcPrefabs == null || npcPrefabs.Length == 0) return;

        // 🟢 สุ่มเลือก 1 ตัวจาก Array
        int randomIndex = Random.Range(0, npcPrefabs.Length);
        GameObject selectedPrefab = npcPrefabs[randomIndex];

        GameObject newNPC = Instantiate(selectedPrefab, transform.position, Quaternion.identity);
        spawnedNPCs.Add(newNPC);

        EnemyBehavior behavior = newNPC.GetComponent<EnemyBehavior>();
        if (behavior != null)
        {
            behavior.SetSafeZone(transform);
        }
    }

    void SetNextSpawnTime()
    {
        spawnTimer = Random.Range(minSpawnTime, maxSpawnTime);
    }
}