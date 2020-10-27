using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TubePathMesh : PathMesh
{
	[SerializeField, Range(3, 30), OnValueChanged("BuildMesh")]
	protected int subdivisions = 8;

	protected override void AddEdgeLoop(float iter, int numIter, Vector3? forwardOverride = null)
	{
		Vector3 point = path.GetPoint(iter);

		Vector3 forward = path.GetDirection(iter);
		if (forwardOverride.HasValue)
			forward = forwardOverride.Value;

		Vector3 up = path.GetNormal(iter);
		Vector3 right = Vector3.Cross(forward, up).normalized;
		up = Vector3.Cross(right, forward).normalized;

		float radius = path.Radius;
		float dist = dists[numIter];

		float yAlpha = dist / pathLength;
		if (float.IsNaN(yAlpha))
			yAlpha = 0f;

		Color color = Color.black;
		if (colorVerts)
			color = colorGradient.Evaluate(yAlpha);

		float widthMod = 1f;
		if (variableWidth)
			widthMod = widthCurve.Evaluate(yAlpha);

		int startNum = (subdivisions + 1) * numIter;
		for (int i = startNum; i <= startNum + subdivisions; i++)
		{
			float xAlpha = (i - startNum)/ (float)subdivisions;

			Vector3 offset = right * Mathf.Cos(xAlpha * LuckshotMath.TAU) + up * Mathf.Sin(xAlpha * LuckshotMath.TAU);
			Vector3 vert = point + offset * radius * widthMod;

			verts.Add(transform.InverseTransformPoint(vert));
			normals.Add(transform.InverseTransformVector(offset));
			uvs.Add(new Vector2(xAlpha, dist));

			if(colorVerts)
				colors.Add(color);

			if (numIter == 0)
			{
				if (i > startNum)
				{
					indices.Add(i);
					indices.Add(i + 1);
					indices.Add(0);
				}
			}
			else
			{
				if (iter == fillAmount)
				{
					if (i > startNum)
					{
						indices.Add(startNum + (subdivisions + 1) + 1);
						indices.Add(i + 1);
						indices.Add(i);
					}
				}

				if (i > startNum)
				{
					indices.Add(i - (subdivisions + 1));
					indices.Add(i);
					indices.Add(i + 1);

					indices.Add(i - (subdivisions + 1));
					indices.Add(i + 1); 
					indices.Add(i + 1 - (subdivisions + 1));
				}
			}
		}
	}
}
