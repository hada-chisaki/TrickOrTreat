using System.Collections;
using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    public GameObject ghostPrefab;
    public float spawnInterval = 3f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (ghostPrefab != null)
            {
                Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SetInterval(float interval)
    {
        spawnInterval = interval;
    }
}