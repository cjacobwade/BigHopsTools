using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Luckshot.Paths
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class FlatPathMesh : PathMesh
	{
		[SerializeField, OnValueChanged("BuildMesh")]
		private Vector2 sideLength = Vector2.one;

		private Vector3[] offsets = new Vector3[]
		{
			new Vector3(0.5f, 0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f),
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f)
		};

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

			int subdivisions = offsets.Length - 1;
			int startNum = (subdivisions + 1) * numIter;
			for (int i = startNum; i <= startNum + subdivisions; i++)
			{
				int index = i - startNum;

				Vector3 offset = right * offsets[index].x * sideLength.x * widthMod + up * offsets[index].y * sideLength.y;
				Vector3 vert = point + offset;

				verts.Add(transform.InverseTransformPoint(vert));
				normals.Add(transform.InverseTransformVector(offset));
				uvs.Add(new Vector2(1 - (i / 2), dist));

				if (colorVerts)
					colors.Add(color);

				if (numIter == 0)
				{
					if (i > 0)
					{
						indices.Add(0);
						indices.Add(i + 1);
						indices.Add(i);
					}
				}
				else
				{
					if (iter == fillAmount)
					{
						if (i > 0)
						{
							indices.Add(i);
							indices.Add(i + 1);
							indices.Add(startNum + (subdivisions + 1) + 1);
						}
					}

					if (i > 0)
					{
						indices.Add(i + 1);
						indices.Add(i);
						indices.Add(i - (subdivisions + 1));


						indices.Add(i + 1 - (subdivisions + 1));
						indices.Add(i + 1);
						indices.Add(i - (subdivisions + 1));
					}
				}
			}
		}
	}
}
