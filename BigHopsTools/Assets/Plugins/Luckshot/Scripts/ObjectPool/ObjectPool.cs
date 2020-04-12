using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class, new()
{
	private List<T> pool = new List<T>();
	private int size = 0;

	public ObjectPool(int inSize)
	{
		size = inSize;

		pool.Capacity = size;
		for(int i =0; i < size; i++)
			pool.Add(new T());
	}

	public T Fetch()
	{
		T instance = null;

		if (pool.Count > 0)
		{
			instance = pool[0];
			pool.RemoveAt(0);

			return instance;
		}
		else
		{
			instance = new T();
		}

		return instance;
	}

	public void Return(T instance)
	{ pool.Add(instance); }
}
