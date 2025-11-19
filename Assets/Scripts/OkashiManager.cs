using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// お菓子マネジャー
/// スコアを管理し、スコアに応じてオブジェクトの表示/非表示を制御する
/// </summary>
public class OkashiManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static OkashiManager instance;
    public static OkashiManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OkashiManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("OkashiManager");
                    instance = go.AddComponent<OkashiManager>();
                }
            }
            return instance;
        }
    }

    [Header("スコア設定")]
    [SerializeField, Tooltip("初期スコア")]
    private int initialScore = 100;

    [SerializeField, Tooltip("現在のスコア（読み取り専用）")]
    public int currentScore;


    [SerializeField] public int subPoint = 5;
    [SerializeField] public int addPoint = 5;

    [System.Serializable]
    public class StageThreshold
    {
        [Tooltip("この段階の名前")]
        public string stageName;

        [Tooltip("この段階に入るボーダースコア")]
        public int borderScore;

        [Tooltip("この段階で削除/復活するオブジェクト")]
        public GameObject[] targetObjects;

        [HideInInspector]
        public bool isActive = true;  // この段階のオブジェクトが表示されているか
    }

    [SerializeField, Tooltip("各段階の設定（第1段階から順に設定）")]
    private StageThreshold[] stages = new StageThreshold[3]
    {
        new StageThreshold { stageName = "第1段階", borderScore = 70 },
        new StageThreshold { stageName = "第2段階", borderScore = 40 },
        new StageThreshold { stageName = "第3段階", borderScore = 10 }
    };

    [Header("ゲームオーバー設定")]
    [SerializeField, Tooltip("スコアが0になった時のフラグ")]
    public bool isGameOver = false;

    // スコアが0になったときのイベント
    public delegate void GameOverDelegate();
    public event GameOverDelegate OnGameOver;

    // スコア変更時のイベント
    public delegate void ScoreChangeDelegate(int newScore);
    public event ScoreChangeDelegate OnScoreChanged;

    // 段階変更時のイベント
    public delegate void StageChangeDelegate(string stageName, bool isActive);
    public event StageChangeDelegate OnStageChanged;

    /// <summary>
    /// 現在のスコアを取得
    /// </summary>
    public int CurrentScore => currentScore;

    /// <summary>
    /// ゲームオーバーフラグを取得
    /// </summary>
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        // シングルトンの設定
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 初期化
        Initialize();
    }

    private void Start()
    {
        // 初期状態でオブジェクトを全て表示
        ShowAllStageObjects();
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Initialize()
    {
        currentScore = initialScore;
        isGameOver = false;

        // 各段階を初期化
        foreach (var stage in stages)
        {
            stage.isActive = true;
        }
    }

    /// <summary>
    /// 段階の遷移をチェック
    /// </summary>
    private void CheckStageTransition(int oldScore, int newScore)
    {
        // スコアが減少した場合（オブジェクト削除チェック）
        if (newScore < oldScore)
        {
            for (int i = stages.Length - 1; i >= 0; i--)
            {
                var stage = stages[i];
                if (stage.isActive && newScore < stage.borderScore)
                {
                    HideStageObjects(stage);
                    stage.isActive = false;
                    OnStageChanged?.Invoke(stage.stageName, false);
                }
            }
        }
        // スコアが増加した場合（オブジェクト復活チェック）
        else if (newScore > oldScore)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (!stage.isActive && newScore >= stage.borderScore)
                {
                    ShowStageObjects(stage);
                    stage.isActive = true;
                    OnStageChanged?.Invoke(stage.stageName, true);
                }
            }
        }
    }

    /// <summary>
    /// 特定段階のオブジェクトを非表示にする
    /// </summary>
    private void HideStageObjects(StageThreshold stage)
    {
        if (stage.targetObjects == null) return;

        foreach (var obj in stage.targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"{stage.stageName}: オブジェクト「{obj.name}」を削除");
            }
        }
    }

    /// <summary>
    /// 特定段階のオブジェクトを表示する
    /// </summary>
    private void ShowStageObjects(StageThreshold stage)
    {
        if (stage.targetObjects == null) return;

        foreach (var obj in stage.targetObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"{stage.stageName}: オブジェクト「{obj.name}」を復活");
            }
        }
    }

    /// <summary>
    /// 全ての段階のオブジェクトを表示する
    /// </summary>
    private void ShowAllStageObjects()
    {
        foreach (var stage in stages)
        {
            ShowStageObjects(stage);
            stage.isActive = true;
        }
    }

    /// <summary>
    /// ゲームオーバー処理
    /// </summary>
    private void SetGameOver()
    {
        isGameOver = true;
        Debug.Log("ゲームオーバー！スコアが0になりました");
        OnGameOver?.Invoke();
    }

    public void SubScore()
    {
        int previousScore = currentScore;
        currentScore -= subPoint;

        // 段階チェック
        CheckStageTransition(previousScore, currentScore);

        // ゲームオーバーチェック
        if (currentScore <= 0 && !isGameOver)
        {
            SetGameOver();
        }
    }

    public void AddScore()
    {
        int previousScore = currentScore;
        currentScore += addPoint;

        CheckStageTransition(previousScore, currentScore);
    }

#if UNITY_EDITOR
    /// <summary>
    /// インスペクターで値が変更された時の処理
    /// </summary>
    private void OnValidate()
    {
        // 段階の配列サイズを3に固定
        if (stages == null || stages.Length != 3)
        {
            System.Array.Resize(ref stages, 3);
            for (int i = 0; i < 3; i++)
            {
                if (stages[i] == null)
                {
                    stages[i] = new StageThreshold
                    {
                        stageName = $"第{i + 1}段階",
                        borderScore = 100 - (i + 1) * 30
                    };
                }
            }
        }

        // 初期スコアの範囲制限
        initialScore = Mathf.Max(0, initialScore);
    }
#endif
}