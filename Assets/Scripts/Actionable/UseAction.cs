using System.Collections;
using UnityEngine;

namespace Actionable
{
    public abstract class UseAction : MonoBehaviour
    {
        [HideInInspector] public bool triggered;

        // ReSharper disable once Unity.IncorrectMethodSignature
        // It's actually the right one bucko
        private IEnumerator Reset()
        {
            triggered = false;
            yield return new WaitForEndOfFrame();
            triggered = true;
        }

        public void Trigger()
        {
            if (!triggered)
                triggered = true;
            else
                StartCoroutine(nameof(Reset));
        }

        protected abstract IEnumerator RunAnimation(float duration = 1f);
    }
}