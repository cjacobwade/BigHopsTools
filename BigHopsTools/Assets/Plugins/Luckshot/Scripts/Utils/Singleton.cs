using UnityEngine;

public class Singleton : MonoBehaviour
{
	protected static bool isQuitting = false;

	[RuntimeInitializeOnLoadMethod]
	static void RunOnStart()
	{
		Application.quitting += Application_OnQuit;
	}

	static void Application_OnQuit()
	{
		isQuitting = true;
		Application.quitting -= Application_OnQuit;
	}
}

public class Singleton<T> : Singleton where T : MonoBehaviour
{
	private static T instance = null;
	public static T Instance
	{
		get
		{
			if(instance == null)
				instance = FindObjectOfType<T>();

			return instance;
		}
	}

	protected virtual void Awake()
	{
		if(instance == null)
		{
			instance = this as T;
		}
		else if (instance != this)
		{
			Debug.LogWarning(string.Format("Multiple instances of {0}. Destroying new instance.", typeof(T)));
			Destroy(gameObject);
		}
	}

	protected virtual void OnDestroy()
	{
		instance = null;
	}
}
