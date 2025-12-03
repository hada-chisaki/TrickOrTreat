using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("ゲーム設定")]
    [SerializeField] private float gameTime = 120f; // ゲーム時間（秒）


    [Header("UI要素")]
    [SerializeField] private GameObject resultUI; // 結果表示UI
    [SerializeField] private Text scoreText;      // スコア表示テキスト
    [SerializeField] private Text timerText;      // タイマー表示テキスト（オプション）
    [SerializeField] private GameObject Location;
    [SerializeField] private FadeExample fadeExample;

    // 参照オブジェクト
    [SerializeField] OkashiManager okashiManager;

    [System.Serializable]
    public struct LeaveTimeAction
    {
        public int time;
        public UnityEvent action;
    }

    [Header("時間ごとに実行する関数")]
    [SerializeField] public List<LeaveTimeAction> leaveTimeActions = new List<LeaveTimeAction>();

    [Header("ゲーム開始時にアクティブ化するもの")]
    public GameObject spawner;

    // ▼▼ ここから カウントダウン関係 ▼▼
    [Header("開始カウントダウン")]
    [SerializeField] private bool useStartCountdown = true;
    [SerializeField] private int startCountdownSeconds = 3;
    [SerializeField] private Text startCountdownText;
    [SerializeField] private string startCountdownEndText = "START!";

    [Header("終了カウントダウン")]
    [SerializeField] private bool useEndCountdown = true;
    [SerializeField] private int endCountdownSeconds = 3;
    [SerializeField] private Text endCountdownText;
    // ▲▲ ここまで カウントダウン関係 ▲▲

    private int destroyedCount = 0;
    private float remainingTime;
    private bool isGameActive = false;   // ← 最初はfalseにしておく

    void Start()
    {
        remainingTime = gameTime;

        if (resultUI != null)
            resultUI.SetActive(false);

        if (startCountdownText != null)
            startCountdownText.gameObject.SetActive(false);

        if (endCountdownText != null)
            endCountdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        // 開始カウントダウン
        if (useStartCountdown && startCountdownText != null && fadeExample.isFadeIn)
        {
            startCountdownText.gameObject.SetActive(false);
            fadeExample.isFadeIn = false;
            StartCoroutine(StartCountdownRoutine());
        }
        else if (!useStartCountdown)
        {
            isGameActive = true;
        }

        if (!isGameActive) return;

        // タイマー更新
        remainingTime -= Time.deltaTime;

        // 終了カウントダウン表示
        HandleEndCountdownUI();

        if (okashiManager.isGameOver)
        {
            GameOver();
        }
        else if (remainingTime <= 0)
        {
            StopGame();
        }

        // タイマー表示（オプション）
        if (timerText != null)
            timerText.text = $"残り時間: {Mathf.Max(0, Mathf.CeilToInt(remainingTime))}秒";

        // 時間ごとのイベント
        foreach (var leaveTimeAction in leaveTimeActions)
        {
            int compareTime = (int)remainingTime;
            if (leaveTimeAction.time == compareTime)
            {
                leaveTimeAction.action?.Invoke();
                return;
            }
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

        // カウントダウンUIを消す
        if (startCountdownText != null)
            startCountdownText.gameObject.SetActive(false);
        if (endCountdownText != null)
            endCountdownText.gameObject.SetActive(false);

        // 結果表示
        if (resultUI != null)
        {
            resultUI.SetActive(true);
            if (scoreText != null)
                scoreText.text = $"撃破数: {destroyedCount}";

            if (Location) Location.SetActive(false);
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

    private void GameOver()   //ゲームオーバー時に発火する
    {
        StopGame();
    }

    // -------------------------
    // 開始カウントダウン処理
    // -------------------------
    private IEnumerator StartCountdownRoutine()
    {
        isGameActive = false;

        startCountdownText.gameObject.SetActive(true);

        for (int t = startCountdownSeconds; t > 0; t--)
        {
            startCountdownText.text = t.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (!string.IsNullOrEmpty(startCountdownEndText))
        {
            startCountdownText.text = startCountdownEndText;
            yield return new WaitForSeconds(1f);
        }

        startCountdownText.gameObject.SetActive(false);
        spawner.SetActive(true);
        isGameActive = true;
    }

    // -------------------------
    // 終了カウントダウンUI
    // -------------------------
    private void HandleEndCountdownUI()
    {
        if (!useEndCountdown || endCountdownText == null) return;
        if (remainingTime <= 0f)
        {
            endCountdownText.gameObject.SetActive(false);
            return;
        }

        int intTime = Mathf.CeilToInt(remainingTime);

        if (intTime <= endCountdownSeconds)
        {
            endCountdownText.gameObject.SetActive(true);
            endCountdownText.text = intTime.ToString();
        }
        else if (endCountdownText.gameObject.activeSelf)
        {
            endCountdownText.gameObject.SetActive(false);
        }
    }
}
