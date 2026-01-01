using System.Collections;
using MessagePipe;
using UnityEngine;
using VContainer;
using GameLoopEvents;

public sealed class GameManager : MonoBehaviour
{
    [Header("Seconds")]
    [SerializeField] float startAfterSeconds = 1f;   // 終了後、次の開始まで
    [SerializeField] float timeLimitSeconds = 60f;  // 制限時間（<=0 なら時間切れ判定なし）

    public int Score { get; private set; } = 0;

    public float GameTimeSeconds { get; private set; }     // ゲーム中の経過
    public float SinceEventSeconds { get; private set; }   // 最後にイベント発行してからの経過

    IPublisher<GameStart> _pubStart;
    IPublisher<GameEnd> _pubEnd;
    IPublisher<GameOver> _pubOver;
    IPublisher<GameClear> _pubClear;

    Coroutine _startRoutine;
    bool _isRunning;
    bool _finished; // 二重発行防止

    [Inject]
    void Construct(
        IPublisher<GameStart> pubStart,
        IPublisher<GameEnd> pubEnd,
        IPublisher<GameOver> pubOver,
        IPublisher<GameClear> pubClear)
    {
        _pubStart = pubStart;
        _pubEnd = pubEnd;
        _pubOver = pubOver;
        _pubClear = pubClear;
    }

    void Start()
    {
        ScheduleStart(startAfterSeconds);
    }

    void Update()
    {
        SinceEventSeconds += Time.deltaTime;

        if (!_isRunning || _finished) return;

        GameTimeSeconds += Time.deltaTime;

        // 条件判定（毎フレーム）
        CheckGameResult();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // --------------------
    // 外部からスコア操作（最小）
    // --------------------
    public void SetScore(int value)
    {
        Score = value;
        if (_isRunning && !_finished) CheckGameResult(); // 即反映
    }

    public void AddScore(int delta)
    {
        Score += delta;
        if (_isRunning && !_finished) CheckGameResult(); // 即反映
    }

    // --------------------
    // ループ制御
    // --------------------
    void ScheduleStart(float delay)
    {
        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = StartCoroutine(StartAfter(delay));
    }

    IEnumerator StartAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        StartGame();
    }

    void ResetOnEvent() => SinceEventSeconds = 0f;

    public void StartGame()
    {
        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = null;

        _isRunning = true;
        _finished = false;
        GameTimeSeconds = 0f;

        ResetOnEvent();
        _pubStart.Publish(new GameStart());

        // 開始時点でスコアが0以下なら即ゲームオーバー
        CheckGameResult();
    }

    void EndGame()
    {
        if (!_isRunning) return;

        _isRunning = false;

        ResetOnEvent();
        _pubEnd.Publish(new GameEnd());

        // 次の開始予約（必要ないなら消してOK）
        ScheduleStart(startAfterSeconds);
    }

    // --------------------
    // 条件判定（ここが仕様の心臓）
    // --------------------
    void CheckGameResult()
    {
        // ゲームオーバー：スコア 0以下
        if (Score <= 0)
        {
            _finished = true;
            ResetOnEvent();
            _pubOver.Publish(new GameOver());
            EndGame();
            return;
        }

        // ゲームクリア：スコア1以上 かつ 時間切れ
        if (timeLimitSeconds > 0f && GameTimeSeconds >= timeLimitSeconds && Score >= 1)
        {
            _finished = true;
            ResetOnEvent();
            _pubClear.Publish(new GameClear());
            EndGame();
            return;
        }
    }
}
