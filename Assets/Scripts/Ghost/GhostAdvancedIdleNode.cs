using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using DG.Tweening;
using System.Collections;

namespace GhostAI.Behaviors
{
    [TaskCategory("Ghost AI")]
    [TaskDescription("Advanced Ghost idle state with multiple animation patterns")]
    public class GhostAdvancedIdleNode : Action
    {
        public enum IdleAnimationType
        {
            Float,          // Simple up/down floating
            Figure8,        // Figure-8 pattern
            Circle,         // Circular motion
            Random,         // Random wandering within bounds
            Pulse,          // Size pulsing effect
            Combined        // Combination of multiple effects
        }

        [Header("Idle Settings")]
        [UnityEngine.Tooltip("Type of idle animation")]
        public IdleAnimationType animationType = IdleAnimationType.Float;

        [UnityEngine.Tooltip("Duration of idle state in seconds (0 = infinite)")]
        public SharedFloat idleDuration = 3f;

        [UnityEngine.Tooltip("Look at target while idling")]
        public SharedBool lookAtTarget = false;

        [UnityEngine.Tooltip("Target to look at")]
        public SharedTransform lookTarget;

        [Header("Animation Parameters")]
        [UnityEngine.Tooltip("Movement amplitude")]
        public SharedFloat amplitude = 0.5f;

        [UnityEngine.Tooltip("Animation speed")]
        public SharedFloat animationSpeed = 1f;

        [UnityEngine.Tooltip("Use smooth transitions between animations")]
        public SharedBool smoothTransitions = true;

        [Header("Float Animation")]
        public SharedFloat floatHeight = 0.5f;
        public SharedFloat floatDuration = 2f;
        public Ease floatEase = Ease.InOutSine;

        [Header("Circle Animation")]
        public SharedFloat circleRadius = 1f;
        public SharedFloat circleDuration = 4f;
        public SharedBool horizontalCircle = true;

        [Header("Pulse Animation")]
        public SharedFloat pulseScale = 1.2f;
        public SharedFloat pulseDuration = 1f;
        public Ease pulseEase = Ease.InOutQuad;

        [Header("Random Animation")]
        public SharedVector3 randomBounds = new Vector3(2f, 1f, 2f);
        public SharedFloat randomMoveTime = 1.5f;
        public SharedFloat randomWaitTime = 0.5f;

        // Private variables
        private float startTime;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private DG.Tweening.Sequence animationSequence;
        private bool isAnimating = false;

        public override void OnStart()
        {
            startTime = Time.time;

            // Store original transform values
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;

            // Start the selected animation type
            StartAnimation();

            isAnimating = true;
        }

        public override TaskStatus OnUpdate()
        {
            // Handle look at target
            if (lookAtTarget.Value && lookTarget.Value != null)
            {
                Vector3 direction = (lookTarget.Value.position - transform.position).normalized;
                direction.y = 0; // Keep ghost upright
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }

            // Check if idle duration has elapsed
            if (idleDuration.Value > 0 && Time.time - startTime >= idleDuration.Value)
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            StopAllAnimations();

            // Smoothly return to original state
            if (smoothTransitions.Value && isAnimating)
            {
                DG.Tweening.Sequence returnSequence = DOTween.Sequence();
                returnSequence.Join(transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad));
                returnSequence.Join(transform.DORotateQuaternion(originalRotation, 0.5f).SetEase(Ease.OutQuad));
                returnSequence.Join(transform.DOScale(originalScale, 0.5f).SetEase(Ease.OutQuad));
                returnSequence.OnComplete(() => isAnimating = false);
            }
            else
            {
                transform.position = originalPosition;
                transform.rotation = originalRotation;
                transform.localScale = originalScale;
                isAnimating = false;
            }
        }

        private void StartAnimation()
        {
            switch (animationType)
            {
                case IdleAnimationType.Float:
                    CreateFloatAnimation();
                    break;
                case IdleAnimationType.Figure8:
                    CreateFigure8Animation();
                    break;
                case IdleAnimationType.Circle:
                    CreateCircleAnimation();
                    break;
                case IdleAnimationType.Random:
                    CreateRandomAnimation();
                    break;
                case IdleAnimationType.Pulse:
                    CreatePulseAnimation();
                    break;
                case IdleAnimationType.Combined:
                    CreateCombinedAnimation();
                    break;
            }
        }

        private void CreateFloatAnimation()
        {
            animationSequence = DOTween.Sequence();

            Vector3 targetPosition = originalPosition + Vector3.up * floatHeight.Value;

            animationSequence.Append(
                transform.DOMoveY(targetPosition.y, floatDuration.Value)
                    .SetEase(floatEase)
            );
            animationSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void CreateFigure8Animation()
        {
            animationSequence = DOTween.Sequence();

            float duration = 4f / animationSpeed.Value;
            Vector3 center = originalPosition;

            // Create figure-8 path points
            Vector3[] path = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                float x = Mathf.Sin(angle) * amplitude.Value;
                float y = Mathf.Sin(angle * 2) * amplitude.Value * 0.5f;
                path[i] = center + new Vector3(x, y, 0);
            }

            animationSequence.Append(
                transform.DOPath(path, duration, PathType.CatmullRom, PathMode.Full3D)
                    .SetEase(Ease.Linear)
                    .SetOptions(true, AxisConstraint.None, AxisConstraint.Y)
            );
            animationSequence.SetLoops(-1, LoopType.Restart);
        }

        private void CreateCircleAnimation()
        {
            animationSequence = DOTween.Sequence();

            float duration = circleDuration.Value / animationSpeed.Value;
            Vector3 center = originalPosition;

            // Create circular path
            Vector3[] path = new Vector3[16];
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * Mathf.PI * 2f;
                if (horizontalCircle.Value)
                {
                    float x = Mathf.Cos(angle) * circleRadius.Value;
                    float z = Mathf.Sin(angle) * circleRadius.Value;
                    path[i] = center + new Vector3(x, 0, z);
                }
                else
                {
                    float x = Mathf.Cos(angle) * circleRadius.Value;
                    float y = Mathf.Sin(angle) * circleRadius.Value;
                    path[i] = center + new Vector3(x, y, 0);
                }
            }

            animationSequence.Append(
                transform.DOPath(path, duration, PathType.CatmullRom, PathMode.Full3D)
                    .SetEase(Ease.Linear)
                    .SetOptions(true)
            );
            animationSequence.SetLoops(-1, LoopType.Restart);
        }

        private void CreatePulseAnimation()
        {
            animationSequence = DOTween.Sequence();

            animationSequence.Append(
                transform.DOScale(originalScale * pulseScale.Value, pulseDuration.Value)
                    .SetEase(pulseEase)
            );
            animationSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void CreateRandomAnimation()
        {
            // Random movement uses a different approach
            RandomMovement();
        }

        private void RandomMovement()
        {
            if (!isAnimating) return;

            Vector3 randomOffset = new Vector3(
                Random.Range(-randomBounds.Value.x, randomBounds.Value.x),
                Random.Range(-randomBounds.Value.y, randomBounds.Value.y),
                Random.Range(-randomBounds.Value.z, randomBounds.Value.z)
            );

            Vector3 targetPosition = originalPosition + randomOffset;

            transform.DOMove(targetPosition, randomMoveTime.Value)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    if (isAnimating)
                    {
                        DOVirtual.DelayedCall(randomWaitTime.Value, () => RandomMovement());
                    }
                });
        }

        private void CreateCombinedAnimation()
        {
            animationSequence = DOTween.Sequence();

            // Combine float and pulse animations
            animationSequence.Append(
                transform.DOMoveY(originalPosition.y + floatHeight.Value, floatDuration.Value)
                    .SetEase(floatEase)
            );

            animationSequence.Join(
                transform.DOScale(originalScale * 1.1f, floatDuration.Value * 0.5f)
                    .SetEase(Ease.InOutQuad)
                    .SetLoops(2, LoopType.Yoyo)
            );

            // Add a gentle rotation
            animationSequence.Join(
                transform.DORotate(new Vector3(0, 360, 0), floatDuration.Value * 2f, RotateMode.LocalAxisAdd)
                    .SetEase(Ease.Linear)
            );

            animationSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void StopAllAnimations()
        {
            if (animationSequence != null && animationSequence.IsActive())
            {
                animationSequence.Kill();
                animationSequence = null;
            }

            // DOKill will stop all tweens on this transform, including random movement
            transform.DOKill();
        }

        public override void OnPause(bool paused)
        {
            if (animationSequence != null && animationSequence.IsActive())
            {
                if (paused)
                    animationSequence.Pause();
                else
                    animationSequence.Play();
            }
        }

        public override void OnReset()
        {
            animationType = IdleAnimationType.Float;
            idleDuration = 3f;
            lookAtTarget = false;
            lookTarget = null;
            amplitude = 0.5f;
            animationSpeed = 1f;
            smoothTransitions = true;
            floatHeight = 0.5f;
            floatDuration = 2f;
            floatEase = Ease.InOutSine;
            circleRadius = 1f;
            circleDuration = 4f;
            horizontalCircle = true;
            pulseScale = 1.2f;
            pulseDuration = 1f;
            pulseEase = Ease.InOutQuad;
            randomBounds = new Vector3(2f, 1f, 2f);
            randomMoveTime = 1.5f;
            randomWaitTime = 0.5f;
        }
    }
}