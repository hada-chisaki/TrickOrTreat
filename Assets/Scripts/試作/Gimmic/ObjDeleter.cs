using Unity.VisualScripting;
using UnityEngine;

public class ObjDeleter : MonoBehaviour
{
    public void DeleteObj(GameObject gameObject)
    {
        Destroy(gameObject);
    }
}
