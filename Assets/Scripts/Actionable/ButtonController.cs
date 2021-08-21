using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Actionable
{
    public class ButtonController : UseAction
    {
        private Vector3 _originalPos;

        [Tooltip("Animation curve")]
        public AnimationCurve animationCurve;
        [Tooltip("Animation duration in seconds")]
        public float animationDuration;
        
        // Start is called before the first frame update
        public void Start()
        {
            triggered = false;
            _originalPos = transform.position;
        }

        // Update is called once per frame
        public void Update()
        {
            if (triggered)
            {
                StartCoroutine(nameof(RunAnimation), animationDuration);
            }
            else
            {
                transform.position = _originalPos;
            }
        
        }
        
        protected override IEnumerator RunAnimation(float duration = 1f)
        {
            Vector3 direction = transform.forward;
            float halfDuration = duration / 2f;
            float progress = 0f;
            while (progress/duration < 1f && triggered)
            {
                float eval = progress < halfDuration
                    ? animationCurve.Evaluate(progress/halfDuration)
                    : animationCurve.Evaluate((1f - (progress - halfDuration) / halfDuration));
                
                transform.position = _originalPos + direction * eval /
                    (transform.localScale.magnitude * transform.parent.localScale.magnitude);
                
                progress += Time.deltaTime;
                
                yield return null;
            }

            triggered = false;
        }
    }
}
