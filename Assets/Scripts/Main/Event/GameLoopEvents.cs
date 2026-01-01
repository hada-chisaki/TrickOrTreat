// GameLoopEvents.cs
namespace GameLoopEvents
{
    public sealed class GameStart { }
    public sealed class GameEnd { }
    public sealed class GameOver { }
    public sealed class GameClear { }

    // Scene
    public readonly struct SceneTransitionRequest
    {
        public readonly string SceneName;
        public readonly float DelaySeconds;
        public SceneTransitionRequest(string sceneName, float delaySeconds = 0f)
        {
            SceneName = sceneName;
            DelaySeconds = delaySeconds;
        }
    }

    public readonly struct SceneTransition
    {
        public readonly string SceneName;
        public SceneTransition(string sceneName) => SceneName = sceneName;
    }

    // Fade（追加）
    public enum FadeStyle
    {
        AlphaBlack,
        SlideLeft,
    }

    public enum FadeKind
    {
        FadeIn,
        FadeOut
    }

    public readonly struct FadeRequest
    {
        public readonly int Id;          // 完了待ち用
        public readonly bool FadeIn;     // true: 透明へ / false: 黒へ
        public readonly float Seconds;
        public readonly FadeStyle Style;

        public FadeRequest(int id, bool fadeIn, float seconds, FadeStyle style)
        {
            Id = id;
            FadeIn = fadeIn;
            Seconds = seconds;
            Style = style;
        }
    }

    public readonly struct FadeFinished
    {
        public readonly int Id;
        public readonly FadeKind Kind;

        public FadeFinished(int id, FadeKind kind)
        {
            Id = id;
            Kind = kind;
        }
    }

}
