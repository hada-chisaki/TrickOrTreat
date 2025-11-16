using Oculus.Interaction;
using UnityEngine;

public class GrabListener : MonoBehaviour
{
    Grabbable grabbable;
    Rigidbody rb;
    bool isGrabbing;
    bool isFloating = false;
    float floatTimer;
    Vector3 basePos;

    [SerializeField] float floatAmplitude = 0.05f; // 上下の振幅（m）
    [SerializeField] float floatSpeed = 2f;       // 上下スピード

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += OnPointerEvent;
    }

    void OnDestroy()
    {
        grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    void Update()
    {
        if (isFloating)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            Vector3 pos = basePos;
            pos.y += Mathf.Sin(floatTimer) * floatAmplitude;

            // 親がいる場合はローカル座標で制御、いない場合はワールド座標で制御
            if (transform.parent != null)
            {
                transform.localPosition = pos;
            }
            else
            {
                transform.position = pos;
            }
        }
    }

    void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:   // 掴み開始
                isGrabbing = true;
                StopFloating();
                rb.useGravity = false;
                rb.isKinematic = false;
                break;

            case PointerEventType.Unselect: // 掴み終了
                isGrabbing = false;
                rb.useGravity = true;
                rb.isKinematic = false;
                break;

            case PointerEventType.Cancel:
                isGrabbing = false;
                StopFloating();
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isGrabbing && collision.gameObject.CompareTag("Ground"))
        {
            // 浮遊開始
            rb.useGravity = false;
            rb.isKinematic = true;

            // 親がいる場合はローカル座標、いない場合はワールド座標を基準にする
            if (transform.parent != null)
            {
                basePos = transform.localPosition;
                basePos.y += 1.0f;
            }
            else
            {
                basePos = transform.position;
                basePos.y += 1.0f;
            }

            isFloating = true;
            floatTimer = 0f;
            Debug.Log("✨ 浮遊モード開始");
        }
    }

    void StopFloating()
    {
        isFloating = false;
    }
}