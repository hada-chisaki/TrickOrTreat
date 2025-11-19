using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    /// <summary>
    /// 画面全体のフェード制御用マネージャー
    /// シーン遷移やローディング画面で使用
    /// </summary>
    public class ScreenFadeManager : MonoBehaviour
    {
        private static ScreenFadeManager instance;
        public static ScreenFadeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ScreenFadeManager>();
                    if (instance == null)
                    {
                        CreateInstance();
                    }
                }
                return instance;
            }
        }
        
        [Header("Fade Panel Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private float defaultFadeDuration = 1.0f;
        
        [Header("Fade Curves")]
        [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Canvas fadeCanvas;
        private CanvasGroup canvasGroup;
        private Coroutine currentFadeCoroutine;
        private bool isFading = false;
        
        // プロパティ
        public bool IsFading => isFading;
        public float CurrentAlpha => fadeImage != null ? fadeImage.color.a : 0f;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private static void CreateInstance()
        {
            GameObject fadeObject = new GameObject("ScreenFadeManager");
            instance = fadeObject.AddComponent<ScreenFadeManager>();
            instance.CreateFadeCanvas();
        }
        
        private void Initialize()
        {
            if (fadeImage == null)
            {
                CreateFadeCanvas();
            }
            
            // 初期状態は完全に透明
            SetAlpha(0f);
        }
        
        private void CreateFadeCanvas()
        {
            // Canvas作成
            GameObject canvasObject = new GameObject("FadeCanvas");
            canvasObject.transform.SetParent(transform);
            
            fadeCanvas = canvasObject.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // 最前面に表示
            
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // CanvasGroup追加（インタラクション制御用）
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // フェード用Image作成
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            
            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = fadeColor;
            
            // 画面全体をカバー
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        
        #region Public Methods
        
        /// <summary>
        /// フェードイン（画面を表示）
        /// </summary>
        public void FadeIn(float? duration = null, Action onComplete = null)
        {
            StartFade(1f, 0f, duration ?? defaultFadeDuration, fadeInCurve, onComplete);
        }
        
        /// <summary>
        /// フェードアウト（画面を隠す）
        /// </summary>
        public void FadeOut(float? duration = null, Action onComplete = null)
        {
            StartFade(0f, 1f, duration ?? defaultFadeDuration, fadeOutCurve, onComplete);
        }
        
        /// <summary>
        /// フェードアウト → アクション実行 → フェードイン
        /// </summary>
        public void FadeOutAndIn(Action middleAction, float? fadeOutDuration = null, float? fadeInDuration = null)
        {
            FadeOut(fadeOutDuration, () =>
            {
                middleAction?.Invoke();
                FadeIn(fadeInDuration);
            });
        }
        
        /// <summary>
        /// 非同期版：フェードアウト → アクション実行 → フェードイン
        /// </summary>
        public IEnumerator FadeOutAndInAsync(Action middleAction, float? fadeOutDuration = null, float? fadeInDuration = null)
        {
            // フェードアウト
            bool fadeOutComplete = false;
            FadeOut(fadeOutDuration, () => fadeOutComplete = true);
            
            while (!fadeOutComplete)
            {
                yield return null;
            }
            
            // 中間処理実行
            middleAction?.Invoke();
            yield return null; // 1フレーム待機
            
            // フェードイン
            bool fadeInComplete = false;
            FadeIn(fadeInDuration, () => fadeInComplete = true);
            
            while (!fadeInComplete)
            {
                yield return null;
            }
        }
        
        /// <summary>
        /// フェード色を変更
        /// </summary>
        public void SetFadeColor(Color color)
        {
            fadeColor = color;
            if (fadeImage != null)
            {
                Color currentColor = fadeImage.color;
                currentColor.r = color.r;
                currentColor.g = color.g;
                currentColor.b = color.b;
                fadeImage.color = currentColor;
            }
        }
        
        /// <summary>
        /// 即座にアルファ値を設定
        /// </summary>
        public void SetAlpha(float alpha)
        {
            StopFade();
            ApplyAlpha(alpha);
        }
        
        /// <summary>
        /// フェードを停止
        /// </summary>
        public void StopFade()
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
                isFading = false;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartFade(float startAlpha, float endAlpha, float duration, AnimationCurve curve, Action onComplete)
        {
            StopFade();
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(startAlpha, endAlpha, duration, curve, onComplete));
        }
        
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, AnimationCurve curve, Action onComplete)
        {
            isFading = true;
            
            float elapsed = 0f;
            
            // レイキャストブロック設定
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float curveValue = curve.Evaluate(progress);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
                
                ApplyAlpha(alpha);
                
                yield return null;
            }
            
            // 最終値を設定
            ApplyAlpha(endAlpha);
            
            // 完全に透明になったらレイキャストを無効化
            if (canvasGroup != null && endAlpha <= 0.01f)
            {
                canvasGroup.blocksRaycasts = false;
            }
            
            isFading = false;
            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }
        
        private void ApplyAlpha(float alpha)
        {
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = alpha;
                fadeImage.color = color;
            }
        }
        
        #endregion
        
        #region Static Convenience Methods
        
        /// <summary>
        /// 静的メソッド：フェードイン
        /// </summary>
        public static void In(float? duration = null, Action onComplete = null)
        {
            Instance.FadeIn(duration, onComplete);
        }
        
        /// <summary>
        /// 静的メソッド：フェードアウト
        /// </summary>
        public static void Out(float? duration = null, Action onComplete = null)
        {
            Instance.FadeOut(duration, onComplete);
        }
        
        /// <summary>
        /// 静的メソッド：フェードアウト＆イン
        /// </summary>
        public static void OutAndIn(Action middleAction, float? fadeOutDuration = null, float? fadeInDuration = null)
        {
            Instance.FadeOutAndIn(middleAction, fadeOutDuration, fadeInDuration);
        }
        
        #endregion
    }
}