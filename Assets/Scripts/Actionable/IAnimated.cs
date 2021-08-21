using System.Collections;

namespace Actionable
{
    public interface IAnimated
    {
        public IEnumerator RunAnimation(float duration = 1f);
    }
}