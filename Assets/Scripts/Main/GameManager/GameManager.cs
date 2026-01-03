using System.Collections;
using MessagePipe;
using UnityEngine;
using VContainer;
using GameLoopEvents;
using System;

public sealed class GameManager : MonoBehaviour
{
    [Header("Seconds")]
    [SerializeField] float startAfterSeconds = 1f;   // 最初の開始待ち
    [SerializeField] float timeLimitSeconds = 60f;   // 制限時間（<=0 なら無効）

    [Header("Scene")]
    [SerializeField] string resultSceneName = "Result";
    [SerializeField] float toResultDelaySeconds = 0f;

    public int Score { get; private set; } = 100;
    public float GameTimeSeconds { get; private set; }
    public float SinceEventSeconds { get; private set; }

    private IDisposable _subFadeFinished;

    //発行するイベント
    IPublisher<GameStart> _pubStart;
    IPublisher<GameEnd> _pubEnd;
    IPublisher<GameOver> _pubOver;
    IPublisher<GameClear> _pubClear;
    IPublisher<SceneTransitionRequest> _pubScene;

    Coroutine _startRoutine;
    bool _isRunning;
    bool _finished;     // GameOver/Clear 二重発行防止
    bool _terminated;   // GameEnd 発行後は二度と開始しない

    private bool isFadeIn = false;

    [Inject]
    void Construct(
        ISubscriber<FadeFinished> subFadeFinished,
        IPublisher<GameStart> pubStart,
        IPublisher<GameEnd> pubEnd,
        IPublisher<GameOver> pubOver,
        IPublisher<GameClear> pubClear,
        IPublisher<SceneTransitionRequest> pubScene,
        SceneManager sceneManager)
    {
        _pubStart = pubStart;
        _pubEnd = pubEnd;
        _pubOver = pubOver;
        _pubClear = pubClear;
        _pubScene = pubScene;

        _subFadeFinished = subFadeFinished.Subscribe(e => ScheduleStart(startAfterSeconds, e));
    }

    void Update()
    {
        SinceEventSeconds += Time.deltaTime;
        if (!_isRunning || _finished || _terminated) return;

        GameTimeSeconds += Time.deltaTime;
        CheckGameResult();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        _startRoutine = null;
    }

    // ---- ループ制御 ----
    void ScheduleStart(float delay, FadeFinished fadeFinished)
    {
        if (_terminated) return;
        if (!isFadeIn && fadeFinished.Kind == FadeKind.FadeIn)
        {
            if (_startRoutine != null) StopCoroutine(_startRoutine);
            _startRoutine = StartCoroutine(StartAfter(delay));


            isFadeIn = true;
        }
    }

    IEnumerator StartAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (_terminated) yield break; // ★終了後は開始しない
        StartGame();
    }

    void ResetOnEvent() => SinceEventSeconds = 0f;

    public void StartGame()
    {
        if (_terminated) return; // ★終了後は開始しない

        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = null;

        _isRunning = true;
        _finished = false;
        GameTimeSeconds = 0f;

        ResetOnEvent();
        _pubStart.Publish(new GameStart());

        CheckGameResult();
        Debug.Log("ゲーム開始");
    }

    void EndGame()
    {
        if (!_isRunning || _terminated) return;

        _isRunning = false;
        _terminated = true; // ★ここから先は二度と開始しない

        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = null;

        ResetOnEvent();
        _pubEnd.Publish(new GameEnd());

        // ★Resultへ遷移要求
        _pubScene.Publish(new SceneTransitionRequest(resultSceneName, toResultDelaySeconds));

        Debug.Log("ゲーム終了");
    }

    // ---- 条件判定 ----
    void CheckGameResult()
    {
        if (_terminated) return;

        // GameOver：スコア0以下
        if (Score <= 0)
        {
            _finished = true;
            ResetOnEvent();
            _pubOver.Publish(new GameOver());
            EndGame();
            return;
        }

        // GameClear：スコア1以上 かつ 時間切れ
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
