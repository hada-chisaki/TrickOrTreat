using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject plane;          // 生成面（Collider 必須）
    [SerializeField] private float interval = 2f;       // 出現間隔
    [SerializeField] private float heightOffset = 0.2f; // 面から少し浮かせる距離

    MeshRenderer rend;
    Collider planeCol;

    void Start()
    {
        rend = plane.GetComponent<MeshRenderer>();
        planeCol = plane.GetComponent<Collider>();
        InvokeRepeating(nameof(SpawnEnemy), 0f, interval);
    }

    void SpawnEnemy()
    {
        Bounds b = rend.bounds;

        // まずXZをAABB内でランダムに決める（ワールド座標）
        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);

        // 上から面に向けてレイキャストして、実際の表面位置を取得
        Vector3 rayOrigin = new Vector3(x, b.max.y + 5f, z);
        Ray ray = new Ray(rayOrigin, Vector3.down);
        Vector3 spawnPos;

        // plane のレイヤーだけに当てたい場合
        int layerMask = 1 << plane.layer;

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, layerMask))
        {
            // 表面＋法線方向にオフセット
            spawnPos = hit.point + hit.normal * heightOffset;
        }
        else
        {
            // 念のためのフォールバック
            spawnPos = new Vector3(x, b.center.y + heightOffset, z);
        }

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
