using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("ゲーム設定")]
    [SerializeField] private float gameTime = 120f; // ゲーム時間（秒）

    [Header("UI要素")]
    [SerializeField] private GameObject resultUI; // 結果表示UI
    [SerializeField] private Text scoreText; // スコア表示テキスト
    [SerializeField] private Text timerText; // タイマー表示テキスト（オプション）

    private int destroyedCount = 0;
    private float remainingTime;
    private bool isGameActive = true;

    void Start()
    {
        remainingTime = gameTime;
        if (resultUI != null)
            resultUI.SetActive(false);
    }

    void Update()
    {
        if (!isGameActive) return;

        // タイマー更新
        remainingTime -= Time.deltaTime;

        // タイマー表示（オプション）
        if (timerText != null)
            timerText.text = $"残り時間: {Mathf.Max(0, Mathf.CeilToInt(remainingTime))}秒";

        // 時間切れでゲーム終了
        if (remainingTime <= 0)
        {
            StopGame();
        }
    }

    // Ghostが破壊された時に呼び出す
    public void OnEnemyDestroyed(GameObject enemy)
    {
        if (!isGameActive) return;

        if (enemy.CompareTag("Ghost"))
        {
            destroyedCount++;
        }
    }

    // ゲーム停止とUI表示
    void StopGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; // ゲーム一時停止

        // 結果表示
        if (resultUI != null)
        {
            resultUI.SetActive(true);
            if (scoreText != null)
                scoreText.text = $"撃破数: {destroyedCount}";
        }
    }

    // ゲームリスタート（オプション）
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}