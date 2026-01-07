using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Start SE")]
    [SerializeField] private AudioSource audioSource;   // 未設定なら GetComponent で拾う
    [SerializeField] private AudioClip startClip;
    [SerializeField, Range(0f, 1f)] private float startVolume = 1f;

    [Header("Hit")]
    [SerializeField] private string hitTag = "Hammer";
    [SerializeField] private float totalBounceSeconds = 0.22f;
    [SerializeField] private float stretchY = 1.35f; // 伸び
    [SerializeField] private float squashY = 0.85f; // つぶれ
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Explosion")]
    [SerializeField] private ParticleSystem explosionPrefab;
    [SerializeField] private Transform explosionPoint; // 未設定なら自分の位置

    private Vector3 _baseScale;
    private bool _dead;

    protected virtual void Awake()
    {
        _baseScale = transform.localScale;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    protected virtual void Start()
    {
        // Start時：音源再生
        if (audioSource != null && startClip != null)
            audioSource.PlayOneShot(startClip, startVolume);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (!other.CompareTag(hitTag)) return;

        StartCoroutine(DieSequence());
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (_dead) return;
        if (!collision.collider.CompareTag(hitTag)) return;

        StartCoroutine(DieSequence());
    }

    protected virtual IEnumerator DieSequence()
    {
        _dead = true;

        // ぼよよん（Yスケールだけ）
        yield return BounceY();

        // 爆発
        SpawnExplosion();

        // 破壊
        Destroy(gameObject);
    }

    private IEnumerator BounceY()
    {
        // 伸び→つぶれ→戻る（3段）
        float a = totalBounceSeconds * 0.45f;
        float b = totalBounceSeconds * 0.35f;
        float c = totalBounceSeconds * 0.45f;

        yield return LerpY(_baseScale.y, _baseScale.y * stretchY, a);
        yield return LerpY(_baseScale.y * stretchY, _baseScale.y * squashY, b);
        yield return LerpY(_baseScale.y * squashY, _baseScale.y, c);

        // 念のため元に戻す
        transform.localScale = _baseScale;
    }

    private IEnumerator LerpY(float fromY, float toY, float dur)
    {
        if (dur <= 0f)
        {
            SetYScale(toY);
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            float e = ease != null ? ease.Evaluate(u) : u;

            SetYScale(Mathf.Lerp(fromY, toY, e));
            yield return null;
        }

        SetYScale(toY);
    }

    private void SetYScale(float y)
    {
        var s = transform.localScale;
        s.y = y;
        transform.localScale = s;
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab == null) return;

        Vector3 pos = explosionPoint ? explosionPoint.position : transform.position;
        Quaternion rot = explosionPoint ? explosionPoint.rotation : Quaternion.identity;

        var fx = Instantiate(explosionPrefab, pos, rot);
        fx.Play();

        // 自動破棄（duration + lifetime をざっくり考慮）
        float life = fx.main.duration;
        if (fx.main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            life += fx.main.startLifetime.constantMax;
        else
            life += fx.main.startLifetime.constant;

        Destroy(fx.gameObject, life + 0.2f);
    }
}
