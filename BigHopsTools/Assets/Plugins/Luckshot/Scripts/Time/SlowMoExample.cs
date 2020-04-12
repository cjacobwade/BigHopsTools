using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlowMoExample : MonoBehaviour
{
    [SerializeField]
    private Text text = null;

    [SerializeField]
    private Transform spawner = null;

    [SerializeField]
    private GameObject prefab = null;

    [SerializeField]
    private FloatRange launchForceRange = new FloatRange(6f, 10f);

    [SerializeField]
    private FloatRange launchIntervalRange = new FloatRange(0.1f, 0.3f);

    [SerializeField]
    private KeyCode key = KeyCode.Space;

    private bool slow = false;

    [SerializeField]
    private float slowTimeScale = 0.1f;

    [SerializeField]
    private AnimationCurve changeTimeScaleCurve = new AnimationCurve();

    [SerializeField]
    private float transitionSlowTime = 0.5f;

    private Lens<float> timeScaleRequest = null;

    private Coroutine transitionRoutine = null;

    private void Start()
    {
        text.text = string.Format("Time Scale: {0:0.00}", 1f);
        timeScaleRequest = new Lens<float>(this, 1f);

        InvokeManager.Invoke(LaunchBall, launchIntervalRange.Random);
    }

    private void LaunchBall()
    {
        GameObject go = Instantiate(prefab, spawner.position, spawner.rotation);
        go.gameObject.SetActive(true);

        go.GetComponent<Rigidbody>().AddForce(spawner.forward * launchForceRange.Random, ForceMode.Impulse);

        InvokeManager.Invoke(LaunchBall, launchIntervalRange.Random);
    }

    private void Update()
    {
        if (Input.GetKeyDown(key))
        {
            slow = !slow;

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(TransitionSlow_Async());
        }
    }

    private IEnumerator TransitionSlow_Async()
    {
        TimeScaleManager.Instance.TimeScaleLens.AddRequest(timeScaleRequest);

        float startTimeScale = timeScaleRequest.Value;
        float endTimeScale = slow ? slowTimeScale : 1f;

        float timer = 0f;
        while(timer < transitionSlowTime)
        {
            timer += Time.deltaTime;

            float alpha = changeTimeScaleCurve.Evaluate(timer / transitionSlowTime);
            timeScaleRequest.Value = Mathf.Lerp(startTimeScale, endTimeScale, alpha);
            TimeScaleManager.Instance.TimeScaleLens.EvaluateRequests();

            text.text = string.Format("Time Scale: {0:0.00}", timeScaleRequest.Value);

            yield return null;
        }

        if (endTimeScale == 1f)
            TimeScaleManager.Instance.TimeScaleLens.RemoveRequest(timeScaleRequest);

        transitionRoutine = null;
    }
}
