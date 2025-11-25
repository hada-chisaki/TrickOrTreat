using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MoveScene : MonoBehaviour
{
    [SerializeField] private InputActionReference nextSentence;
    [SerializeField] FadeExample fadeExample;
    [SerializeField] string sceneName;

    // Update is called once per frame
    void Update()
    {
        // 追加: VRコントローラ（Input System）
        if (nextSentence != null && nextSentence.action.WasPressedThisFrame())
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
