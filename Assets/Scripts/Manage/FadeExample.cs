using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

/// <summary>
/// フェード機能の使用例
/// </summary>
public class FadeExample : MonoBehaviour
{
    [Header("UI要素のフェード例")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private GameObject titleText;

    private void Start()
    {
        // 例1: 画面全体をフェードイン
        ScreenFadeManager.In(1.5f, () =>
        {
            Debug.Log("画面フェードイン完了");
        });
    }

    /// <summary>
    /// UI要素をフェードイン
    /// </summary>
    public void ShowUI()
    {
        if (uiPanel != null)
        {
            FadeController fadeController = uiPanel.GetComponent<FadeController>();
            if (fadeController == null)
            {
                fadeController = uiPanel.AddComponent<FadeController>();
            }

            fadeController.FadeIn(0.5f, () =>
            {
                Debug.Log("UIパネル表示完了");
            });
        }
    }

    /// <summary>
    /// UI要素をフェードアウト
    /// </summary>
    public void HideUI()
    {
        if (uiPanel != null)
        {
            FadeController fadeController = uiPanel.GetComponent<FadeController>();
            if (fadeController != null)
            {
                fadeController.FadeOut(0.5f, () =>
                {
                    Debug.Log("UIパネル非表示完了");
                    // 必要に応じてGameObjectを無効化
                    // uiPanel.SetActive(false);
                });
            }
        }
    }

    /// <summary>
    /// シーン遷移（フェード付き）
    /// </summary>
    public void LoadSceneWithFade(string sceneName)
    {
        // フェードアウト → シーン読み込み → フェードイン
        ScreenFadeManager.OutAndIn(() =>
        {
            SceneManager.LoadScene(sceneName);
        }, 1.0f, 1.0f);
    }

    /// <summary>
    /// 非同期シーン遷移（フェード付き）
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        // フェードアウト開始
        bool fadeComplete = false;
        ScreenFadeManager.Out(1.0f, () => fadeComplete = true);

        // フェードアウト完了待機
        while (!fadeComplete)
        {
            yield return null;
        }

        // シーンを非同期で読み込み
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // ローディング処理
        while (asyncLoad.progress < 0.9f)
        {
            Debug.Log($"Loading: {asyncLoad.progress * 100}%");
            yield return null;
        }

        // シーンをアクティベート
        asyncLoad.allowSceneActivation = true;

        // シーンの切り替わりを待つ
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // フェードイン
        ScreenFadeManager.In(1.0f);
    }

    /// <summary>
    /// クロスフェード例
    /// </summary>
    public void CrossFadeImages(GameObject fromImage, GameObject toImage)
    {
        FadeController fromController = fromImage.GetComponent<FadeController>();
        FadeController toController = toImage.GetComponent<FadeController>();

        if (fromController == null)
            fromController = fromImage.AddComponent<FadeController>();
        if (toController == null)
            toController = toImage.AddComponent<FadeController>();

        // toImageを非表示状態から開始
        toController.SetAlpha(0f);
        toImage.SetActive(true);

        // クロスフェード実行
        fromController.CrossFade(toController, 1.5f);
    }

    /// <summary>
    /// 簡易的なフェード使用例
    /// </summary>
    public void QuickFadeExample()
    {
        // 素早くフェードイン
        if (titleText != null)
        {
            FadeController.QuickFadeIn(titleText, 0.3f, () =>
            {
                Debug.Log("タイトル表示完了");
            });
        }
    }

    /// <summary>
    /// フェード色の変更例
    /// </summary>
    public void ChangeFadeColor()
    {
        // 白いフェードに変更
        ScreenFadeManager.Instance.SetFadeColor(Color.white);

        // フェードアウト＆イン
        ScreenFadeManager.OutAndIn(() =>
        {
            Debug.Log("白いフラッシュエフェクト");
        }, 0.2f, 0.2f);

        // 元の色（黒）に戻す
        System.Collections.IEnumerator ResetColor()
        {
            yield return new WaitForSeconds(0.5f);
            ScreenFadeManager.Instance.SetFadeColor(Color.black);
        }
        StartCoroutine(ResetColor());
    }
}