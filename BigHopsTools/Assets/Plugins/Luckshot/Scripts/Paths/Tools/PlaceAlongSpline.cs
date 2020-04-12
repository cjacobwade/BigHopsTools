using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(OrientSpline))]
public class PlaceAlongSpline : MonoBehaviour
{
#if UNITY_EDITOR
	[SerializeField]
	private OrientSpline spline = null;

	[SerializeField]
	private GameObject prefab = null;

	[SerializeField, Range(0.05f, 10f)]
	private float spacing = 0.1f;

	[SerializeField]
	private bool snapToGround = false;

	private float alphaIter = 0.01f;
	private float lastPlaceTime = 0f;

	private OrientSplineData splineData = new OrientSplineData();

	[ContextMenu("Place Items")]
	private void PlaceItems()
	{
		if (spline == null || prefab == null)
			return;

		int numChildren = transform.childCount;
		for(int i = numChildren - 1; i >= 0; i--)
		{
			DestroyImmediate(transform.GetChild(i).gameObject);
		}

		SpawnPrefab(0f);

		Vector3 lastPos = spline.GetPoint(0f);
		float iter = 0f;
		while(iter < 1f)
		{
			float moveDist = spacing;
			while(moveDist > 0f && iter < 1f)
			{
				float prevIter = iter;
				iter += alphaIter;
				Vector3 pos = spline.GetPoint(iter);
				Vector3 toPos = pos - lastPos;
				float toPosDist = toPos.magnitude;

				if(toPosDist < moveDist)
				{
					moveDist -= toPosDist;
					lastPos = pos;
				}
				else
				{
					float alpha = toPosDist / moveDist;
					iter = Mathf.Lerp(prevIter, iter, alpha);
					lastPos = Vector3.Lerp(lastPos, pos, alpha);

					SpawnPrefab(iter);

					moveDist = 0f;
				}
			}
		}
	}

	private void SpawnPrefab(float iter)
	{
		spline.InterpNodeData(splineData, iter);

		Vector3 spawnPos = spline.GetPoint(iter);

		Vector3 direction = spline.GetVelocity(iter).normalized;
		Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
		Vector3 normal = Vector3.Cross(direction, right).normalized;
		Vector3 up = Quaternion.Euler(direction * splineData.angle) * normal;
		if (snapToGround)
		{
			RaycastHit hit = new RaycastHit();
			if(Physics.Raycast(new Ray(spawnPos, -up), out hit, 20f, 1, QueryTriggerInteraction.Ignore))
			{
				spawnPos = hit.point;
				up = hit.normal;
			}
		}

		Quaternion spawnRot = Quaternion.LookRotation(Vector3.Cross(up, right), up);
		GameObject go = (GameObject) PrefabUtility.InstantiatePrefab(prefab, transform);
		go.transform.SetPositionAndRotation(spawnPos, spawnRot);
	}

	private void OnDrawGizmosSelected()
	{
		if (Time.time > lastPlaceTime + 0.1f)
		{
			PlaceItems();
			lastPlaceTime = Time.time;
		}
	}
#endif
}
