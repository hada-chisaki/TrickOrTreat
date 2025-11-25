using Oculus.Interaction;
using UnityEngine;
using Meta.XR.BuildingBlocks;
using Oculus.Interaction.HandGrab;


public class GrabListener : MonoBehaviour
{

    Rigidbody rb;
    bool isGrabbing;
    bool isFloating = false;
    float floatTimer;
    Vector3 basePos;

    // Rigidbodyの設定を保存
    float savedMass = 1f;
    float savedDrag = 0f;
    float savedAngularDrag = 0.05f;

    [SerializeField] float floatAmplitude = 0.05f; // 上下の振幅(m)
    [SerializeField] float floatSpeed = 2f;       // 上下スピード

    //RigidBodyをなくした時に停止するもの
    Grabbable grabbable;
    BuildingBlock buildingBlock;

    GrabInteractable grabInteractable;
    HandGrabInteractable handGrabInteractable;

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += OnPointerEvent;
        buildingBlock = GetComponent<BuildingBlock>();

        Transform child = transform.GetChild(0);
        grabInteractable = child.GetComponent<GrabInteractable>();
        handGrabInteractable = child.GetComponent<HandGrabInteractable>();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            SaveRigidbodySettings();
        }

        // 初回チェック
        CheckParentAndToggleRigidbody();
    }

    void OnDestroy()
    {
        grabbable.WhenPointerEventRaised -= OnPointerEvent;
    }

    void OnTransformParentChanged()
    {
        // 親子関係が変わった時にRigidbodyコンポーネントの追加/削除
        CheckParentAndToggleRigidbody();
    }

    void SaveRigidbodySettings()
    {
        if (rb != null)
        {
            savedMass = rb.mass;
            savedDrag = rb.linearDamping;
            savedAngularDrag = rb.angularDamping;
        }
    }

    void CheckParentAndToggleRigidbody()
    {
        if (transform.parent != null)
        {
            // 親がいる場合はRigidbodyコンポーネントを削除
            if (rb != null)
            {
                grabbable.enabled = false;
                buildingBlock.enabled = false;
                InteractableTriggerBroadcaster inter = GetComponent<InteractableTriggerBroadcaster>();
                if (inter != null) inter.enabled = false;
                grabInteractable.enabled = false;
                handGrabInteractable.enabled = false;

                SaveRigidbodySettings();
                Destroy(rb);
                rb = null;
                isFloating = false;
                isGrabbing = false;

                Debug.Log("🔒 親オブジェクト検出: Rigidbodyコンポーネント削除");
            }
        }
        else
        {
            // 親がいない場合はRigidbodyコンポーネントを追加
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.mass = savedMass;
                rb.linearDamping = savedDrag;
                rb.angularDamping = savedAngularDrag;
                rb.useGravity = true;
                rb.isKinematic = false;
                Debug.Log("🔓 親オブジェクトなし: Rigidbodyコンポーネント追加");


                buildingBlock.enabled = true;
                grabbable.enabled = true;
                InteractableTriggerBroadcaster inter = GetComponent<InteractableTriggerBroadcaster>();
                if (inter != null) inter.enabled = true;
                grabInteractable.enabled = true;
                handGrabInteractable.enabled = true;
            }
        }
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
        // 親がいる場合またはRigidbodyがない場合は処理をスキップ
        if (transform.parent != null || rb == null) return;

        switch (evt.Type)
        {
            case PointerEventType.Select:   // 掴み開始
                isGrabbing = true;
                isFloating = false;
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
                isFloating = false;
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // 親がいる場合またはRigidbodyがない場合は処理をスキップ
        if (transform.parent != null || rb == null) return;

        if (!isGrabbing && collision.gameObject.CompareTag("Ground"))
        {
            // 浮遊開始
            rb.useGravity = false;
            rb.isKinematic = true;

            basePos = transform.position;
            basePos.y += 1.0f;

            isFloating = true;
            floatTimer = 0f;
            Debug.Log("✨ 浮遊モード開始");
        }
    }
}