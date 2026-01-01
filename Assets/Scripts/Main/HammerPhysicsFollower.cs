using UnityEngine;

public class HammerPhysicsFollower : MonoBehaviour
{
    [SerializeField] private Transform controller; // RightControllerInHandAnchor
    [SerializeField] private Rigidbody rb;

    void Reset() => rb = GetComponent<Rigidbody>();

    void Awake()
    {
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.constraints = RigidbodyConstraints.FreezeRotationY;
    }

    void FixedUpdate()
    {
        // 物理で追従（Transform 直接書き換えはNG）
        rb.MovePosition(controller.position);
        rb.MoveRotation(controller.rotation);
    }
}
