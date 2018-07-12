using UnityEngine;

namespace Assets.Scripts.SpaceMapper
{
    [ExecuteInEditMode]
    public class SoftFollow : MonoBehaviour
    {
        public Transform ToFollow;
        public Easing EasingType;
        public static float AnimTime = 1f;
        public enum Easing
        {
            Quadratic,
            Sinusoidal,
            Circular
        }
        public float MoveThreshold = 0.01f;
        public Vector3 MoveOffset;

        private bool _isAnimating;
        private float _animStart;
        private float _animEnd;
        private Vector3 _posStart;
        private Vector3 _scaleStart;

        public static float Ease(float factor, Easing easing)
        {
            var retVal = 0f;
            switch (easing)
            {
                case Easing.Sinusoidal:
                    retVal = -0.5f * (Mathf.Cos(Mathf.PI * factor) - 1);
                    break;
                case Easing.Quadratic:
                    var t = factor * 2f;
                    if (t < 1) retVal = 0.5f * t * t;
                    else
                    {
                        t--;
                        retVal = -0.5f * (t * (t - 2) - 1);
                    }
                    break;
                case Easing.Circular:
                    var tc = factor * 2f;
                    if (tc < 1) return -0.5f * (Mathf.Sqrt(1 - tc * tc) - 1);
                    tc -= 2;
                    retVal = 0.5f * (Mathf.Sqrt(1 - tc * tc) + 1);
                    break;
            }
            return retVal;
        }

        void Awake()
        {
            transform.localScale = ToFollow.localScale;
        }
	
        void Update ()
        {
            if (ToFollow == null)
                return;

            if (_isAnimating)
            {
                var factor = Mathf.Clamp01((Time.unscaledTime - _animStart) / (_animEnd - _animStart));
                var ease = Ease(factor, EasingType);
                var animCenter = Vector3.Lerp(_posStart, ToFollow.position, ease);
                transform.position = animCenter + MoveOffset;
                if (Time.unscaledTime > _animEnd)
                    _isAnimating = false;

                var newScale = Vector3.Lerp(_scaleStart, ToFollow.localScale, ease);
                transform.localScale = newScale;
            }
            else
            {
                CheckForPositionOffset();
            }
        }

        private void CheckForPositionOffset()
        {
            var distance = Vector3.Distance(transform.position - MoveOffset, ToFollow.position);
            if (distance < MoveThreshold)
                return;

            _isAnimating = true;

            _animStart = Time.unscaledTime;
            _animEnd = Time.unscaledTime + AnimTime;
            _posStart = transform.position - MoveOffset;
            _scaleStart = transform.localScale;
        }
    }
}