using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalManager : Singleton<DecalManager>
{
	private List<Decal> queuedDecals = new List<Decal>();

	private void Update()
	{
		if(queuedDecals.Count > 0)
		{
			queuedDecals[0].BuildDecal();
			queuedDecals.RemoveAt(0);
		}

		if (queuedDecals.Count == 0)
			enabled = false;
	}

	public Decal SpawnAndQueue(Decal decalPrefab)
	{ return SpawnAndQueue(decalPrefab, decalPrefab.transform.position, decalPrefab.transform.rotation); }

	public Decal SpawnAndQueue(Decal decalPrefab, Vector3 position, Quaternion rotation, Transform parent = null)
	{
		Decal decal = Instantiate(decalPrefab, position, rotation, parent);
		Queue(decal);

		return decal;
	}

	public void Queue(Decal decal)
	{
		queuedDecals.Add(decal);

		if (queuedDecals.Count == 1)
			enabled = true;
	}
}
