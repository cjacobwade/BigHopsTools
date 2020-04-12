using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentPool<T> where T : Component
{
	private T prefab = null;
	private List<T> pool = new List<T>();

	private int size = 0;
	private Transform parent = null;

	public ComponentPool(T inPrefab, int inSize, Transform inParent)
	{
		prefab = inPrefab;
		size = inSize;

		parent = inParent;

		pool.Capacity = size;
		for(int i =0; i < size; i++)
		{
			T instance = MonoBehaviour.Instantiate(prefab, parent);
			instance.gameObject.SetActive(false);
			pool.Add(instance);
		}
	}

	public T Fetch()
	{
		T instance = null;

		if (pool.Count > 0)
		{
			instance = pool[0];
			instance.transform.SetParent(null, true);
			instance.gameObject.SetActive(true);

			pool.RemoveAt(0);

			return instance;
		}
		else
		{
			instance = MonoBehaviour.Instantiate(prefab, null);
		}

		return instance;
	}

	public void Return(T instance)
	{
		pool.Add(instance);
		instance.transform.SetParent(parent, true);
		instance.gameObject.SetActive(false);
	}
}
