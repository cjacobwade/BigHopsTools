using UnityEngine;

public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObjectSingleton<T>
{
	static bool _hasInstance = false;
	static T _instance = null;

	public static T Instance
	{
		get
		{
			if(!_hasInstance)
			{

				_instance = Resources.Load(typeof(T).ToString()) as T;
				
				if(!_instance)
					_instance = ScriptableObjectUtils.CreateAsset<T>();

				_hasInstance = _instance;
			}
			
			return _hasInstance ? _instance : null;
		}
	}
}