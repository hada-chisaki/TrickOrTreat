using UnityEngine;
using System.Collections;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 右コントローラーを振動
    /// </summary>
    public void VibrateRight(float strength = 0.8f, float duration = 0.2f)
    {
        StartCoroutine(VibrateCoroutine(OVRInput.Controller.RTouch, strength, duration));
    }

    /// <summary>
    /// 左コントローラーを振動
    /// </summary>
    public void VibrateLeft(float strength = 0.8f, float duration = 0.2f)
    {
        StartCoroutine(VibrateCoroutine(OVRInput.Controller.LTouch, strength, duration));
    }

    private IEnumerator VibrateCoroutine(OVRInput.Controller controller, float strength, float duration)
    {
        // 第1引数: 周波数(0~1)　第2引数: 振幅(0~1)
        OVRInput.SetControllerVibration(1f, strength, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}
