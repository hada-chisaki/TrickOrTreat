using Unity.VisualScripting;
using UnityEngine;

public class ObjDeleter : MonoBehaviour
{
    public GameObject gameObject;
    void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }
}
