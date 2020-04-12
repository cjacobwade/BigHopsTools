using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using NaughtyAttributes;

public class InvokeManager : Singleton<InvokeManager>
{
	[System.Serializable]
	public abstract class DelayedInvoke
	{
		public IEnumerator enumerator;
		public MethodInfo methodInfo;
		public object target;

#if UNITY_EDITOR
		public string name;
#endif

		public abstract bool CanInvoke();
	}

	[System.Serializable]
	public class TimeInvoke : DelayedInvoke
	{
		public float endTime;

		public override bool CanInvoke()
		{ return Time.time >= endTime; }
	}

	[System.Serializable]
	public class FrameInvoke : DelayedInvoke
	{
		public int endFrame;

		public override bool CanInvoke()
		{ return Time.frameCount >= endFrame; }
	}

	private const int delayedInvokePoolSize = 500;

	[SerializeField, ReadOnly]
	private List<TimeInvoke> timeInvokePool = new List<TimeInvoke>(delayedInvokePoolSize);

	[SerializeField, ReadOnly]
	private List<TimeInvoke> unscaledTimeInvokePool = new List<TimeInvoke>(delayedInvokePoolSize);

	[SerializeField, ReadOnly]
	private List<FrameInvoke> frameInvokePool = new List<FrameInvoke>(delayedInvokePoolSize);

	[SerializeField, ReadOnly]
	private List<TimeInvoke> activeTimeInvokes = new List<TimeInvoke>();

	[SerializeField, ReadOnly]
	private List<TimeInvoke> activeUnscaledTimeInvokes = new List<TimeInvoke>();

	[SerializeField, ReadOnly]
	private List<FrameInvoke> activeFrameInvokes = new List<FrameInvoke>();

	protected override void Awake()
	{
		base.Awake();

		for (int i = 0; i < delayedInvokePoolSize; i++)
		{
			timeInvokePool.Add(new TimeInvoke());
			unscaledTimeInvokePool.Add(new TimeInvoke());
			frameInvokePool.Add(new FrameInvoke());
		}
	}

	private void LateUpdate()
	{
		for (int i = 0; i < activeTimeInvokes.Count; i++)
		{
			TimeInvoke timeInvoke = activeTimeInvokes[i];

			if (timeInvoke.CanInvoke())
			{
				activeTimeInvokes.RemoveAt(i);
				timeInvokePool.Add(timeInvoke);

				timeInvoke.enumerator.MoveNext();
			}
			else
			{
				break;
			}
		}

		for (int i = 0; i < activeUnscaledTimeInvokes.Count; i++)
		{
			TimeInvoke unscaledTimeInvoke = activeUnscaledTimeInvokes[i];

			if (Time.unscaledTime >= unscaledTimeInvoke.endTime)
			{
				activeUnscaledTimeInvokes.RemoveAt(i);
				unscaledTimeInvokePool.Add(unscaledTimeInvoke);

				unscaledTimeInvoke.enumerator.MoveNext();
			}
			else
			{
				break;
			}
		}

		for (int i = 0; i < activeFrameInvokes.Count; i++)
		{
			FrameInvoke frameInvoke = activeFrameInvokes[i];

			if (frameInvoke.CanInvoke())
			{
				activeFrameInvokes.RemoveAt(i);
				frameInvokePool.Add(frameInvoke);

				frameInvoke.enumerator.MoveNext();
			}
			else
			{
				break;
			}
		}

		if (activeTimeInvokes.Count == 0 &&
			activeFrameInvokes.Count == 0 &&
			activeUnscaledTimeInvokes.Count == 0)
		{
			enabled = false;
		}
	}

	private static void CancelMethod(MethodInfo method, object target)
	{
		if (Instance == null || isQuitting)
			return;

		for (int i = 0; i < Instance.activeTimeInvokes.Count; i++)
		{
			TimeInvoke timeInvoke = Instance.activeTimeInvokes[i];

			if (timeInvoke.methodInfo == method &&
				timeInvoke.target == target)
			{
				Instance.activeTimeInvokes.RemoveAt(i--);
				Instance.timeInvokePool.Add(timeInvoke);
				// Not breaking so we get every matching method
			}
		}

		for (int i = 0; i < Instance.activeUnscaledTimeInvokes.Count; i++)
		{
			TimeInvoke unscaledTimeInvoke = Instance.activeUnscaledTimeInvokes[i];

			if (unscaledTimeInvoke.methodInfo == method &&
				unscaledTimeInvoke.target == target)
			{
				Instance.activeUnscaledTimeInvokes.RemoveAt(i--);
				Instance.unscaledTimeInvokePool.Add(unscaledTimeInvoke);
				// Not breaking so we get every matching method
			}
		}

		for (int i = 0; i < Instance.activeFrameInvokes.Count; i++)
		{
			FrameInvoke frameInvoke = Instance.activeFrameInvokes[i];

			if (frameInvoke.methodInfo == method &&
				frameInvoke.target == target)
			{
				Instance.activeFrameInvokes.RemoveAt(i--);
				Instance.frameInvokePool.Add(frameInvoke);
				// Not breaking so we get every matching method
			}
		}
	}

	public static void CancelInvoke(System.Action action)
	{ CancelMethod(action.Method, action.Target); }

	public static void CancelInvoke<T>(System.Action<T> action)
	{ CancelMethod(action.Method, action.Target); }

	public static void Invoke(System.Action action, float time, bool timeScaleIndependent = false)
	{
		if (timeScaleIndependent)
			AddInvokeWithUnscaledTime(action.Method, action.Target, InvokeAsync(action), Time.unscaledTime + time);
		else
			AddInvokeWithTime(action.Method, action.Target, InvokeAsync(action), Time.time + time);
	}

	public static void Invoke<T>(System.Action<T> action, T argument, float time, bool timeScaleIndependent = false)
	{
		if (timeScaleIndependent)
			AddInvokeWithUnscaledTime(action.Method, action.Target, InvokeAsync<T>(action, argument), Time.unscaledTime + time);
		else
			AddInvokeWithTime(action.Method, action.Target, InvokeAsync<T>(action, argument), Time.time + time);
	}

	public static void Invoke<T, K>(System.Action<T, K> action, T argument, K argument2, float time, bool timeScaleIndependent = false)
	{
		if (timeScaleIndependent)
			AddInvokeWithUnscaledTime(action.Method, action.Target, InvokeAsync<T, K>(action, argument, argument2), Time.unscaledTime + time);
		else
			AddInvokeWithTime(action.Method, action.Target, InvokeAsync<T, K>(action, argument, argument2), Time.time + time);
	}

	static void AddInvokeWithTime(MethodInfo methodInfo, object target, IEnumerator enumerator, float endTime)
	{
		if (Instance == null || isQuitting)
			return;

		if (Instance.timeInvokePool.Count == 0)
		{
			Debug.LogError("TimeManager's TimeInvokePool is empty. Something is very wrong", Instance);
			return;
		}

		Instance.enabled = true;

		TimeInvoke timeInvoke = Instance.timeInvokePool[0];
		timeInvoke.endTime = endTime;
		timeInvoke.enumerator = enumerator;
		timeInvoke.methodInfo = methodInfo;
		timeInvoke.target = target;

#if UNITY_EDITOR
		timeInvoke.name = methodInfo.Name;
#endif

		Instance.timeInvokePool.RemoveAt(0);

		for (int i = 0; i < Instance.activeTimeInvokes.Count; i++)
		{
			TimeInvoke compareTimeInvoke = Instance.activeTimeInvokes[i];
			if (endTime < compareTimeInvoke.endTime)
			{
				Instance.activeTimeInvokes.Insert(i, timeInvoke);
				return;
			}
		}

		Instance.activeTimeInvokes.Add(timeInvoke);
	}

	static void AddInvokeWithUnscaledTime(MethodInfo methodInfo, object target, IEnumerator enumerator, float endTime)
	{
		if (Instance == null || isQuitting)
			return;

		if (Instance.unscaledTimeInvokePool.Count == 0)
		{
			Debug.LogError("TimeManager's UnscaledTimeInvokePool is empty. Something is very wrong", Instance);
			return;
		}

		Instance.enabled = true;

		TimeInvoke unscaledTimeInvoke = Instance.unscaledTimeInvokePool[0];
		unscaledTimeInvoke.endTime = endTime;
		unscaledTimeInvoke.enumerator = enumerator;
		unscaledTimeInvoke.methodInfo = methodInfo;
		unscaledTimeInvoke.target = target;

#if UNITY_EDITOR
		unscaledTimeInvoke.name = methodInfo.Name;
#endif

		Instance.unscaledTimeInvokePool.RemoveAt(0);

		for (int i = 0; i < Instance.activeUnscaledTimeInvokes.Count; i++)
		{
			TimeInvoke compareUnscaledTimeInvoke = Instance.activeUnscaledTimeInvokes[i];
			if (endTime < compareUnscaledTimeInvoke.endTime)
			{
				Instance.activeUnscaledTimeInvokes.Insert(i, unscaledTimeInvoke);
				return;
			}
		}

		Instance.activeUnscaledTimeInvokes.Add(unscaledTimeInvoke);
	}

	public static void Invoke(System.Action action, int frames)
	{ AddInvokeWithFrame(action.Method, action.Target, InvokeAsync(action), Time.frameCount + frames); }

	public static void Invoke<T>(System.Action<T> action, T argument, int frames)
	{ AddInvokeWithFrame(action.Method, action.Target, InvokeAsync<T>(action, argument), Time.frameCount + frames); }

	static void AddInvokeWithFrame(MethodInfo methodInfo, object target, IEnumerator enumerator, int endFrame)
	{
		if (Instance == null || isQuitting)
			return;

		if (Instance.frameInvokePool.Count == 0)
		{
			Debug.LogError("TimeManager's FrameInvokePool is empty. Something is very wrong", Instance);
			return;
		}

		Instance.enabled = true;

		FrameInvoke frameInvoke = Instance.frameInvokePool[0];
		frameInvoke.endFrame = endFrame;
		frameInvoke.enumerator = enumerator;
		frameInvoke.methodInfo = methodInfo;
		frameInvoke.target = target;

#if UNITY_EDITOR
		frameInvoke.name = methodInfo.Name;
#endif

		Instance.frameInvokePool.RemoveAt(0);

		for (int i = 0; i < Instance.activeFrameInvokes.Count; i++)
		{
			FrameInvoke compareFrameInvoke = Instance.activeFrameInvokes[i];
			if (endFrame < compareFrameInvoke.endFrame)
			{
				Instance.activeFrameInvokes.Insert(i, frameInvoke);
				return;
			}
		}

		Instance.activeFrameInvokes.Add(frameInvoke);
	}

	static IEnumerator InvokeAsync(System.Action action)
	{
		action.Invoke();
		yield break;
	}

	static IEnumerator InvokeAsync<T>(System.Action<T> action, T argument)
	{
		action.Invoke(argument);
		yield break;
	}

	static IEnumerator InvokeAsync<T, K>(System.Action<T, K> action, T argument, K argument2)
	{
		action.Invoke(argument, argument2);
		yield break;
	}
}
