using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MoveScene : MonoBehaviour
{
    [SerializeField] private InputActionReference sceneLoad;
    [SerializeField] FadeExample fadeExample;
    [SerializeField] string sceneName;

    // Update is called once per frame
    void Update()
    {
        // 追加: VRコントローラ（Input System）
        if (sceneLoad != null && sceneLoad.action.WasPressedThisFrame())
        {
            Debug.Log("XBotton");
            fadeExample.LoadSceneWithFade(sceneName);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            fadeExample.LoadSceneWithFade(sceneName);
        }
    }
}
