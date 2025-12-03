using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("👻 スポーン設定")]
    public float spawnInterval = 3f;
    public int spawnCount = 1;

    [Header("👻 通常おばけ")]
    public List<GameObject> ghostPrefabs = new List<GameObject>();

    private List<Transform> spawnPoints = new List<Transform>();

    [Header("💀 特別おばけ")]
    public GameObject specialGhostA;
    public GameObject specialGhostB;
    public float specialSpawnIntervalA = 20f;
    public float specialSpawnIntervalB = 30f;

    void Start()
    {
        // 子オブジェクトを変換して登録
        CacheSpawnPoints();

        // 通常おばけループ開始
        StartCoroutine(NormalSpawnLoop());

        // 特別おばけループ開始
        if (specialGhostA) StartCoroutine(SpecialSpawnLoop(specialGhostA, specialSpawnIntervalA));
        if (specialGhostB) StartCoroutine(SpecialSpawnLoop(specialGhostB, specialSpawnIntervalB));
    }

    // ---------------------------------
    // 子オブジェクトから spawnPoints 作成
    // ---------------------------------
    private void CacheSpawnPoints()
    {
        spawnPoints.Clear();

        List<GameObject> children = new List<GameObject>();

        this.gameObject.GetChildGameObjects(children);

        foreach (GameObject child in children)
        {
            spawnPoints.Add(child.transform);
        }

        Debug.Log($"SpawnPoint 読み込み完了: {spawnPoints.Count} 個");
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
    public void ChangeSpawnIntaval(int intaval)
    {
        spawnInterval = intaval;
    }

    public void ChangeSpawnGhostCount(int count)
    {
        spawnCount = count;
    }

    public void SetSpawnInterval(float interval) => spawnInterval = interval;
    public void SetSpawnCount(int count) => spawnCount = count;
}
