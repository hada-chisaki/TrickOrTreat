using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject plane;    // Planeオブジェクト
    [SerializeField] private float interval = 2f; // 出現間隔（秒）
    [SerializeField] private float heightOffset = 0.2f; // Plane上から少し浮かせる距離

    private Vector3 planeSize;

    void Start()
    {
        // Planeの大きさを取得（MeshRendererから）
        var rend = plane.GetComponent<MeshRenderer>();
        planeSize = rend.bounds.size;

        InvokeRepeating(nameof(SpawnEnemy), 0f, interval);
    }

    void SpawnEnemy()
    {
        var rend = plane.GetComponent<MeshRenderer>();
        Bounds bounds = rend.bounds;

        // Planeの範囲からランダムな点を取得
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);

        // Plane表面の高さ（bounds.max.y）を基準に少し上に出現
        float y = bounds.max.y + heightOffset;

        Instantiate(enemyPrefab, new Vector3(x, y, z), Quaternion.identity);
    }
}
