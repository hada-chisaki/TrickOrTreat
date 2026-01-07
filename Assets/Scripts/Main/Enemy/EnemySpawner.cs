using System.Collections;
using UnityEngine;
using DG.Tweening;

public sealed class EnemySpawner : MonoBehaviour
{
    [Header("Ground Start")]
    [SerializeField] private string groundTag = "Ground";

    [Header("Move Child (local Y)")]
    [SerializeField] private Transform childToMove;
    [SerializeField] private float targetLocalY = 0.5f;
    [SerializeField] private float moveSpeed = 0.5f;

    [Header("Light")]
    [SerializeField] private Light spawnerLight;
    [SerializeField] private float lightDelaySeconds = 2f;

    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnIntervalSeconds = 1.0f;

    [Header("Spawn VFX (Light + Scale)")]
    [SerializeField] private Color baseLightColor = Color.blue;
    [SerializeField] private Color spawnLightColor = Color.yellow;
    [SerializeField] private float colorFlashSeconds = 0.12f;     // 黄→青に戻すまで
    [SerializeField] private float scaleUpSeconds = 0.35f;        // 0→1の時間
    [SerializeField] private Ease scaleEase = Ease.OutBack;       // 好きに変更OK
    [SerializeField] private float blinkIntervalSeconds = 0.08f;  // 点滅間隔（小さいほど速い）

    [Header("Destroy On Hit")]
    [SerializeField] private string destroyTargetTag = "PlayerBullet";
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionLifeSeconds = 2f;

    private bool _started;
    private Coroutine _spawnLoop;
    private Coroutine _spawnVfx; // 点滅・色変化の制御用

    private void Awake()
    {
        if (childToMove == null && transform.childCount > 0)
            childToMove = transform.GetChild(0);

        if (spawnerLight == null && childToMove != null)
            spawnerLight = childToMove.GetComponentInChildren<Light>(true);

        if (spawnerLight != null)
        {
            spawnerLight.enabled = false;
            spawnerLight.color = baseLightColor;
        }
    }

    private void OnTriggerEnter(Collider other) => HandleHit(other);
    private void OnCollisionEnter(Collision other) => HandleHit(other.collider);

    private void HandleHit(Collider other)
    {
        // 破壊（指定タグが当たったら相手を破壊＋爆発）
        if (!string.IsNullOrEmpty(destroyTargetTag) && other.CompareTag(destroyTargetTag))
        {
            Vector3 p = other.transform.position;
            Quaternion r = other.transform.rotation;

            Destroy(other.gameObject);

            if (explosionPrefab != null)
            {
                var fx = Instantiate(explosionPrefab, p, r);
                Destroy(fx, explosionLifeSeconds);
            }
            return;
        }

        // 接地開始（地面タグに触れたら一度だけ起動）
        if (!_started && !string.IsNullOrEmpty(groundTag) && other.CompareTag(groundTag))
        {
            _started = true;
            StartCoroutine(StartupSequence());
        }
    }

    private IEnumerator StartupSequence()
    {
        // 子のローカルYを目標へ移動
        if (childToMove != null)
        {
            Vector3 lp = childToMove.localPosition;
            while (!Mathf.Approximately(lp.y, targetLocalY))
            {
                lp.y = Mathf.MoveTowards(lp.y, targetLocalY, moveSpeed * Time.deltaTime);
                childToMove.localPosition = lp;
                yield return null;
            }
        }

        // 数秒後にライト点灯
        if (lightDelaySeconds > 0f)
            yield return new WaitForSeconds(lightDelaySeconds);

        if (spawnerLight != null)
        {
            spawnerLight.color = baseLightColor;
            spawnerLight.enabled = true;
        }

        // ライトが付いたらスポーン開始
        if (enemyPrefab != null)
            _spawnLoop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        float interval = Mathf.Max(0.01f, spawnIntervalSeconds);

        while (true)
        {
            DoSpawnWithVfx();
            yield return new WaitForSeconds(interval);
        }
    }

    private void DoSpawnWithVfx()
    {
        Transform pt = spawnPoint != null ? spawnPoint : transform;

        // 生成
        var go = Instantiate(enemyPrefab, pt.position, pt.rotation);

        // Scale 0 → 1（イージング）
        go.transform.localScale = Vector3.zero;
        go.transform.DOScale(Vector3.one, scaleUpSeconds).SetEase(scaleEase);

        // ライト演出（青→黄フラッシュ + 点滅を拡大中だけ）
        if (spawnerLight != null && spawnerLight.enabled)
        {
            if (_spawnVfx != null) StopCoroutine(_spawnVfx);
            _spawnVfx = StartCoroutine(SpawnLightVfx(scaleUpSeconds));
        }
    }

    private IEnumerator SpawnLightVfx(float vfxDuration)
    {
        // 黄フラッシュ（すぐ青に戻す）
        spawnerLight.color = spawnLightColor;
        if (colorFlashSeconds > 0f)
            yield return new WaitForSeconds(colorFlashSeconds);
        spawnerLight.color = baseLightColor;

        // 点滅（vfxDurationの間だけ）
        float t = 0f;
        bool state = true;
        while (t < vfxDuration)
        {
            state = !state;
            spawnerLight.enabled = state;

            float step = Mathf.Max(0.01f, blinkIntervalSeconds);
            yield return new WaitForSeconds(step);

            t += step;
        }

        // 最後は点灯状態に戻して終了
        spawnerLight.enabled = true;
        spawnerLight.color = baseLightColor;
        _spawnVfx = null;
    }

    private void OnDisable()
    {
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
        if (_spawnVfx != null)
        {
            StopCoroutine(_spawnVfx);
            _spawnVfx = null;
        }
    }
}
