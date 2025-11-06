using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject ghostPrefab;
    public List<Transform> spawnPoints = new List<Transform>();
    public float spawnInterval = 3f;
    public int spawnCount = 1;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnGhosts();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnGhosts()
    {
        if (spawnPoints.Count == 0 || ghostPrefab == null) return;

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < spawnCount && availablePoints.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform point = availablePoints[randomIndex];
            GameObject ghost = Instantiate(ghostPrefab, point.position, Quaternion.identity);
            ghost.SetActive(true);
            availablePoints.RemoveAt(randomIndex);
        }
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }

    public void SetSpawnCount(int count)
    {
        spawnCount = count;
    }
}