using System.Collections;
using UnityEngine;

public class UseAction : MonoBehaviour
{
    private Vector3 _originalPos;

    [Tooltip("Animation curve")]
    public AnimationCurve animationCurve;
    [Tooltip("Animation duration in seconds")]
    public float animationDuration;
    [HideInInspector] public bool triggered;

    private void Start()
    {
        triggered = false;
        _originalPos = transform.position;
    }

    private void Update()
    {
        if (triggered)
            switch (tag)
            {
                case "Button":
                    StartCoroutine(nameof(ButtonAnimationBis), animationDuration);
                    break;
                default:
                    Debug.LogErrorFormat(
                        "ArgumentException: The following tag does not have any programmed triggered behaviour: <b>{0}</b>",
                        tag);
                    triggered = false;
                    break;
            }
        else
            switch (tag)
            {
                case "Button":
                    transform.position = _originalPos;
                    break;
                default:
                    Debug.LogErrorFormat(
                        "ArgumentException: The following tag does not have any programmed un-triggered behaviour: <b>{0}</b>",
                        tag);
                    Destroy(this);
                    break;
            }
    }

    public void Trigger()
    {
        if (!triggered)
            triggered = true;
        else
            StartCoroutine("Reset");
    }

    private IEnumerator Reset()
    {
        triggered = false;
        yield return new WaitForEndOfFrame();
        triggered = true;
    }

    private IEnumerator ButtonAnimationBis(float duration = 1f)
    {
        Vector3 direction = transform.forward;
        float hdur = duration / 2f;
        float progress = 0f;
        while (progress/duration < 1f && triggered)
        {
            float eval = progress < hdur
                ? animationCurve.Evaluate(progress/hdur)
                : animationCurve.Evaluate((1f - (progress - hdur) / hdur));
            transform.position = _originalPos + direction * eval /
                (transform.localScale.magnitude * transform.parent.localScale.magnitude);
            progress += Time.deltaTime;
            yield return null;
        }

        triggered = false;
    }
}