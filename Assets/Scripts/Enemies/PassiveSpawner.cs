using UnityEngine;
using System.Collections.Generic;

public class PassiveSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject[] npcPrefabs;      
    public int maxNPCs = 3;             
    public float minSpawnTime = 3f;     
    public float maxSpawnTime = 7f;     

    [Header("Audio SFX")]
    public AudioClip spawnSound; // 🟢 เสียงตอนที่ NPC โผล่ออกมา (เช่น เสียงป๊อป! หรือเสียงฟรึ่บ!)

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
        
        // 🟢 เล่นเสียง Spawn!
        if (AudioManager.Instance != null && spawnSound != null)
        {
            AudioManager.Instance.PlaySFX(spawnSound, 0.8f);
        }

        spawnedNPCs.Add(newNPC);
        newNPC.transform.SetParent(this.transform);

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