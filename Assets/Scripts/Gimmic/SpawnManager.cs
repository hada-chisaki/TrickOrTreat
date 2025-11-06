using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnerData
    {
        public GhostSpawner spawner;
        public float interval = 3f;
    }
    
    public List<SpawnerData> spawners = new List<SpawnerData>();
    
    void Start()
    {
        // シーン内の全スポーナーを自動取得
        GhostSpawner[] found = FindObjectsOfType<GhostSpawner>();
        foreach (var s in found)
        {
            spawners.Add(new SpawnerData { spawner = s, interval = 3f });
        }
        
        // 各スポーナーの間隔を設定
        foreach (var data in spawners)
        {
            if (data.spawner != null)
            {
                data.spawner.SetInterval(data.interval);
            }
        }
    }
}