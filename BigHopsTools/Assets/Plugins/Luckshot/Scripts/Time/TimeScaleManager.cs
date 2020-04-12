using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleManager : Singleton<TimeScaleManager>
{
	private const float defaultFixedDeltaTime = 1f / 60f;

	public LensManager<float> TimeScaleLens = new LensManager<float>(requests => LensUtils.Min(requests));

	protected override void Awake()
	{
		base.Awake();

		Application.targetFrameRate = 60;

		Time.timeScale = 1f;
		Time.fixedDeltaTime = defaultFixedDeltaTime;

		TimeScaleLens.OnValueChanged += TimeScaleLens_OnValueChanged;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		TimeScaleLens.OnValueChanged -= TimeScaleLens_OnValueChanged;
	}

	private void TimeScaleLens_OnValueChanged(float timeScale)
	{
		Time.timeScale = timeScale;
		Time.fixedDeltaTime = timeScale * defaultFixedDeltaTime;
	}
}
