using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Utils
{
    /// <summary>
    /// フェードイン・フェードアウトを制御するコンポーネント
    /// Image, RawImage, CanvasGroupに対応
    /// </summary>
    public class FadeController : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 1.0f;
        [SerializeField] private float fadeOutDuration = 1.0f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Auto Fade")]
        [SerializeField] private bool fadeInOnStart = false;
        [SerializeField] private float startDelay = 0f;
        
        // キャッシュ用
        private CanvasGroup canvasGroup;
        private Image image;
        private RawImage rawImage;
        private Coroutine currentFadeCoroutine;
        
        // 現在のアルファ値
        private float currentAlpha = 1.0f;
        
        // イベント
        public event Action OnFadeInComplete;
        public event Action OnFadeOutComplete;
        
        private void Awake()
        {
            // コンポーネントを取得してキャッシュ
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
            rawImage = GetComponent<RawImage>();
            
            // 現在のアルファ値を取得
            currentAlpha = GetCurrentAlpha();
        }
        
        private void Start()
        {
            if (fadeInOnStart)
            {
                SetAlpha(0f);
                if (startDelay > 0)
                {
                    StartCoroutine(DelayedFadeIn());
                }
                else
                {
                    FadeIn();
                }
            }
        }
        
        private IEnumerator DelayedFadeIn()
        {
            yield return new WaitForSeconds(startDelay);
            FadeIn();
        }
        
        /// <summary>
        /// フェードイン開始
        /// </summary>
        public void FadeIn(float? duration = null, Action onComplete = null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }
            
            float fadeDuration = duration ?? fadeInDuration;
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, fadeDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeInComplete?.Invoke();
            }));
        }
        
        /// <summary>
        /// フェードアウト開始
        /// </summary>
        public void FadeOut(float? duration = null, Action onComplete = null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }
            
            float fadeDuration = duration ?? fadeOutDuration;
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, fadeDuration, () =>
            {
                onComplete?.Invoke();
                OnFadeOutComplete?.Invoke();
            }));
        }
        
        /// <summary>
        /// 指定したアルファ値にフェード
        /// </summary>
        public void FadeTo(float targetAlpha, float duration, Action onComplete = null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }
            
            float startAlpha = GetCurrentAlpha();
            currentFadeCoroutine = StartCoroutine(FadeCoroutine(startAlpha, targetAlpha, duration, onComplete));
        }
        
        /// <summary>
        /// フェードを即座に停止
        /// </summary>
        public void StopFade()
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
                currentFadeCoroutine = null;
            }
        }
        
        /// <summary>
        /// アルファ値を即座に設定
        /// </summary>
        public void SetAlpha(float alpha)
        {
            StopFade();
            ApplyAlpha(alpha);
        }
        
        /// <summary>
        /// 現在のアルファ値を取得
        /// </summary>
        public float GetCurrentAlpha()
        {
            if (canvasGroup != null)
                return canvasGroup.alpha;
            else if (image != null)
                return image.color.a;
            else if (rawImage != null)
                return rawImage.color.a;
            
            return 1f;
        }
        
        /// <summary>
        /// フェード処理のコルーチン
        /// </summary>
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float curveValue = fadeCurve.Evaluate(progress);
                float alpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
                
                ApplyAlpha(alpha);
                
                yield return null;
            }
            
            // 最終値を確実に設定
            ApplyAlpha(endAlpha);
            
            currentFadeCoroutine = null;
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// アルファ値を適用
        /// </summary>
        private void ApplyAlpha(float alpha)
        {
            currentAlpha = alpha;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                // フェードアウト時はインタラクションも無効化
                canvasGroup.interactable = alpha > 0.01f;
                canvasGroup.blocksRaycasts = alpha > 0.01f;
            }
            else if (image != null)
            {
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
            else if (rawImage != null)
            {
                Color color = rawImage.color;
                color.a = alpha;
                rawImage.color = color;
            }
        }
        
        /// <summary>
        /// フェードイン・アウトをトグル
        /// </summary>
        public void ToggleFade(float? duration = null)
        {
            if (currentAlpha > 0.5f)
            {
                FadeOut(duration);
            }
            else
            {
                FadeIn(duration);
            }
        }
        
        /// <summary>
        /// クロスフェード（別のFadeControllerと同時にフェード）
        /// </summary>
        public void CrossFade(FadeController other, float duration)
        {
            this.FadeOut(duration);
            other.FadeIn(duration);
        }
        
        #region Static Methods for Quick Use
        
        /// <summary>
        /// GameObjectにFadeControllerを追加してフェードイン
        /// </summary>
        public static FadeController QuickFadeIn(GameObject target, float duration = 1.0f, Action onComplete = null)
        {
            FadeController controller = target.GetComponent<FadeController>();
            if (controller == null)
            {
                controller = target.AddComponent<FadeController>();
            }
            
            controller.SetAlpha(0f);
            controller.FadeIn(duration, onComplete);
            return controller;
        }
        
        /// <summary>
        /// GameObjectにFadeControllerを追加してフェードアウト
        /// </summary>
        public static FadeController QuickFadeOut(GameObject target, float duration = 1.0f, Action onComplete = null)
        {
            FadeController controller = target.GetComponent<FadeController>();
            if (controller == null)
            {
                controller = target.AddComponent<FadeController>();
            }
            
            controller.SetAlpha(1f);
            controller.FadeOut(duration, onComplete);
            return controller;
        }
        
        #endregion
    }
}