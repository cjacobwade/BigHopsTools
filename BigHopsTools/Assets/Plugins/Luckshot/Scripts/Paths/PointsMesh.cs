using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointsMesh : MonoBehaviour, ICustomMesh
{
	public event System.Action<ICustomMesh> OnMeshChanged = delegate { };

	[SerializeField, OnValueChanged("BuildMesh")]
	private int subdivisions = 12;

	[SerializeField, OnValueChanged("BuildMesh")]
	private bool variableWidth = false;

	[SerializeField, ShowIf("variableWidth"), OnValueChanged("BuildMesh")]
	private AnimationCurve widthCurve = new AnimationCurve();

	[SerializeField, OnValueChanged("BuildMesh")]
	private bool colorVerts = false;

	[SerializeField, ShowIf("colorVerts"), OnValueChanged("BuildMesh")]
	private Gradient colorGradient = new Gradient();

	[SerializeField, OnValueChanged("BuildMesh")]
	private float radius = 0.1f;
	public void SetRadius(float inRadius, bool buildMesh = true)
	{
		radius = inRadius;
		if (buildMesh)
			BuildMesh();
	}

	private List<Vector3> points = new List<Vector3>();
	public void SetPoints(List<Vector3> inPoints, bool buildMesh = true)
	{
		points = inPoints;
		if (buildMesh)
			BuildMesh();
	}

	private List<Vector3> verts = new List<Vector3>();
	private List<int> indices = new List<int>();
	private List<Vector3> normals = new List<Vector3>();
	private List<Vector2> uvs = new List<Vector2>();
	private List<Color> colors = new List<Color>();

	private MeshFilter mf = null;

	[SerializeField]
	private bool markDynamic = false;
	private Mesh mesh = null;

	[Button("Build Mesh")]
	public void BuildMesh()
	{
		mf = GetComponent<MeshFilter>();
		if (mf != null && points != null && points.Count > 1)
		{
			if (mf.sharedMesh != null)
				DestroyImmediate(mf.sharedMesh);

			if (mesh == null)
				mesh = new Mesh();
			else
				mesh.Clear();

			if (markDynamic)
				mesh.MarkDynamic();

			verts.Clear();
			indices.Clear();
			normals.Clear();
			uvs.Clear();
			colors.Clear();

			Vector3 prevPos = points[0];
			Vector3 normal = (points[0] - points[1]).normalized;

			verts.Add(transform.InverseTransformPoint(prevPos + normal * radius / 2f));
			normals.Add(transform.InverseTransformVector(normal));
			uvs.Add(new Vector2(0f, 0f));

			if(colorVerts)
				colors.Add(colorGradient.Evaluate(0f));

			float pathLength = 0f;

			for (int i = 1; i < points.Count; i++)
				pathLength += (points[i] - points[i - 1]).magnitude;

			for (int i = 0; i < points.Count; i++)
				AddEdgeLoop(i);

			prevPos = points[points.Count - 2];
			normal = points[points.Count - 1] - prevPos;

			verts.Add(transform.InverseTransformPoint(prevPos + normal * radius / 2f));
			uvs.Add(new Vector2(1, pathLength));
			normals.Add(transform.InverseTransformVector(normal));

			if(colorVerts)
				colors.Add(colorGradient.Evaluate(1f));

			mesh.SetVertices(verts);
			mesh.SetTriangles(indices, 0);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);

			if(colorVerts)
				mesh.SetColors(colors);

			mf.sharedMesh = mesh;

			OnMeshChanged(this);
		}
	}

	private void AddEdgeLoop(int numIter)
	{
		Vector3 point = points[numIter];

		Vector3 forward = numIter > 0 ? (points[numIter] - points[numIter - 1]) : (points[1] - points[0]);
		forward.Normalize();

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 up = Vector3.Cross(forward, right).normalized;
		right = Vector3.Cross(up, forward).normalized;

		float yAlpha = numIter / (float)points.Count;
		if (float.IsNaN(yAlpha))
			yAlpha = 0f;

		float widthMod = 1f;
		if(variableWidth)
			widthMod = widthCurve.Evaluate(yAlpha);

		Color color = Color.white;
		if (colorVerts)
			color = colorGradient.Evaluate(yAlpha);

		int numVerts = verts.Count;
		int startNum = (subdivisions + 1) * numIter;
		for (int i = startNum; i <= startNum + subdivisions; i++)
		{
			float xAlpha = (i - startNum) / (float)subdivisions;
			
			Vector3 offset = right * Mathf.Cos(xAlpha * LuckshotMath.TAU) + up * Mathf.Sin(xAlpha * LuckshotMath.TAU);
			Vector3 vert = point + offset.normalized * radius * widthMod;

			verts.Add(transform.InverseTransformPoint(vert));
			normals.Add(transform.InverseTransformVector(offset));
			uvs.Add(new Vector2(xAlpha, yAlpha));

			if(colorVerts)
				colors.Add(color);

			if (numIter == 0)
			{
				if (i > startNum)
				{
					indices.Add(0);
					indices.Add(i + 1);
					indices.Add(i);
				}
			}
			else
			{
				if (i == points.Count - 1)
				{
					if (i > startNum)
					{
						indices.Add(i);
						indices.Add(i + 1);
						indices.Add(startNum + (subdivisions + 1) + 1);
					}
				}

				if (i > startNum)
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
