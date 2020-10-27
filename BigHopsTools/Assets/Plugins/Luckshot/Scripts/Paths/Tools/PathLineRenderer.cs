using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;
using NaughtyAttributes;

[RequireComponent(typeof(LineRenderer))]
public class PathLineRenderer : MonoBehaviour
{
	[SerializeField, AutoCache]
	private PathBase path = null;

	[SerializeField, AutoCache]
	private LineRenderer line = null;

	[SerializeField, OnValueChanged("UpdateLine")]
	private float pointsPerUnit = 5f;

	private void Awake()
	{
		if (path == null)
		{
			Debug.LogError("No PathBase found. Trying to find one now.", this);
			path = GetComponent<PathBase>();
		}

		path.OnPathChanged -= Path_OnPathChanged;
	}

	private void Path_OnPathChanged(PathBase inPath)
	{
		if (path == inPath)
			UpdateLine();
		else
			inPath.OnPathChanged -= Path_OnPathChanged;
	}

	private void UpdateLine()
	{
		if (path != null && line != null)
		{
			int numPoints = Mathf.CeilToInt(path.GetLength() * pointsPerUnit);

			Vector3[] points = new Vector3[numPoints];
			for (int i = 0; i < numPoints; i++)
				points[i] = path.GetPoint(i/(float)(numPoints - 1));

			line.positionCount = numPoints;
			line.SetPositions(points);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (path != null)
		{
			path.OnPathChanged -= Path_OnPathChanged;
			path.OnPathChanged += Path_OnPathChanged;
		}
	}
}
