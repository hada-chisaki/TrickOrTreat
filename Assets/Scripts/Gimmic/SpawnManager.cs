using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("👻 通常おばけ")]
    public List<GameObject> ghostPrefabs = new List<GameObject>(); // ← 通常おばけ4種類
    public List<Transform> spawnPoints = new List<Transform>();
    public float spawnInterval = 3f;
    public int spawnCount = 1;

    [Header("💀 特別おばけ")]
    public GameObject specialGhostA;
    public GameObject specialGhostB;
    public float specialSpawnIntervalA = 20f;
    public float specialSpawnIntervalB = 30f;

    void Start()
    {
        // 通常おばけループ開始
        StartCoroutine(NormalSpawnLoop());

        // 特別おばけループ開始
        if (specialGhostA) StartCoroutine(SpecialSpawnLoop(specialGhostA, specialSpawnIntervalA));
        if (specialGhostB) StartCoroutine(SpecialSpawnLoop(specialGhostB, specialSpawnIntervalB));
    }

    // -----------------------
    // 通常おばけ生成ループ
    // -----------------------
    IEnumerator NormalSpawnLoop()
    {
        while (true)
        {
            SpawnNormalGhosts();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnNormalGhosts()
    {
        if (spawnPoints.Count == 0 || ghostPrefabs.Count == 0) return;

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount && availablePoints.Count > 0; i++)
        {
            int pointIndex = Random.Range(0, availablePoints.Count);
            int prefabIndex = Random.Range(0, ghostPrefabs.Count);

            Transform point = availablePoints[pointIndex];
            GameObject prefab = ghostPrefabs[prefabIndex];

            GameObject ghost = Instantiate(prefab, point.position, Quaternion.identity);
            ghost.SetActive(true);

            availablePoints.RemoveAt(pointIndex);
        }
    }

    // -----------------------
    // 特別おばけ生成ループ
    // -----------------------
    IEnumerator SpecialSpawnLoop(GameObject specialPrefab, float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            SpawnSpecialGhost(specialPrefab);
        }
    }

    void SpawnSpecialGhost(GameObject prefab)
    {
        if (spawnPoints.Count == 0 || prefab == null) return;

        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject ghost = Instantiate(prefab, randomPoint.position, Quaternion.identity);
        ghost.SetActive(true);

    }

    // -----------------------
    // 外部制御用
    // -----------------------
    public void SetSpawnInterval(float interval) => spawnInterval = interval;
    public void SetSpawnCount(int count) => spawnCount = count;
}
