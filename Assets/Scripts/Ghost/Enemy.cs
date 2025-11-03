using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Hit by")]
    [SerializeField] private string hammerTag = "Hammer";

    [Header("Effects")]
    [SerializeField] private GameObject hitVfxPrefab;    // 消える直前に出すパーティクル
    [SerializeField] private float vfxLifetime = 2f;
    [SerializeField] private AudioClip hitSfx;           // 消える直前に鳴らすSE

    [SerializeField] private AudioClip hitsound;        // 消える直前に鳴らすSE

    [SerializeField] private float squashTime = 0.15f;   // ぶよよん時間

    bool dying;

    private Rigidbody rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision c)
    {
        if (dying) return;
        if (!c.collider.CompareTag(hammerTag)) return;

        rigidbody.isKinematic = true;

        Vector3 hitPos = c.contacts.Length > 0 ? c.contacts[0].point : transform.position;
        StartCoroutine(SquashAndDie(hitPos));

        AudioSource.PlayClipAtPoint(hitsound, hitPos);

    }

    IEnumerator SquashAndDie(Vector3 hitPos)
    {
        dying = true;

        // ぶよよん（スケール演出：横広げ＆縦つぶし → 元に戻す）
        Vector3 baseScale = transform.localScale;
        Vector3 squash = new Vector3(baseScale.x * 1.2f, baseScale.y * 0.6f, baseScale.z * 1.2f);

        // 行き
        float t = 0f;
        while (t < squashTime)
        {
            float u = t / squashTime;
            transform.localScale = Vector3.Lerp(baseScale, squash, u);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = squash;

        // 戻り（少しだけオーバーシュート抑えめで元へ）
        t = 0f;
        while (t < squashTime * 0.8f)
        {
            float u = t / (squashTime * 0.8f);
            transform.localScale = Vector3.Lerp(squash, baseScale, u);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = baseScale;

        // 消滅演出（VFX & SE）
        if (hitVfxPrefab)
        {
            var vfx = Instantiate(hitVfxPrefab, hitPos, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }
        if (hitSfx)
        {
            AudioSource.PlayClipAtPoint(hitSfx, hitPos);
        }

        // 余計な多重ヒットを防ぐため、当たりを切って即消し
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Destroy(gameObject);
    }
}
