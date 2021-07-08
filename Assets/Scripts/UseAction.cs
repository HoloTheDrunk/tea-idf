using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class UseAction : MonoBehaviour
    {
        [HideInInspector]
        public bool triggered;

        public AnimationCurve curve;

        private Vector3 _originalPos;
        
        void Start()
        {
            triggered = false;
            _originalPos = transform.position;
        }

        void Update()
        {
            if (triggered)
            {
                switch (tag)
                {
                    case "Button":
                        StartCoroutine("ButtonAnimation");
                        break;
                    default:
                        throw new ArgumentException("The following tag does not have any programmed behaviour: ", tag);
                }
            }
            else
            {
                    
                switch (tag)
                {
                    case "Button":
                        transform.position = _originalPos;
                        break;
                    default:
                        throw new ArgumentException("The following tag does not have any programmed behaviour: ", tag);
                }
            }
        }

        public void Trigger()
        {
            if (!triggered)
            {
                triggered = true;
            }
            else
            {
                StartCoroutine("Reset");
            }
        }

        private IEnumerator Reset()
        {
            triggered = false;
            yield return new WaitForEndOfFrame();
            triggered = true;
        }

        private IEnumerator ButtonAnimation()
        {
            Vector3 direction = transform.parent.position - transform.position;
            direction = new Vector3(direction.x > 0 ? 1 : 0, 0, direction.z > 0 ? 1 : 0);
            for (int i = 0; i < 60 && triggered; i++)
            {
                float eval = i < 30 ? curve.Evaluate(i / 30f) : curve.Evaluate(1f - (i - 30f) / 30f);
                transform.position = _originalPos + direction * eval /
                    (transform.localScale.magnitude * transform.parent.localScale.magnitude);
                yield return null;
            }

            triggered = false;
        }
    }
}
