using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SplinePath), typeof(MeshFilter), typeof(MeshRenderer))]
public class SlideMesh : MonoBehaviour, ICustomMesh
{
	public event System.Action<ICustomMesh> OnMeshChanged = delegate {};

	[SerializeField, OnValueChanged("BuildMesh")]
	private float radius = 0.1f;
	public float Radius
	{ get { return radius; } }

	[SerializeField, OnValueChanged("BuildMesh")]
	private float thickness = 0.1f;

	[SerializeField, Range(3, 50), OnValueChanged("BuildMesh")]
	private int subdivisions = 10;

	[SerializeField, Range(0.1f, 20f), OnValueChanged("BuildMesh")]
	private float spacing = 0.7f;

	[SerializeField, Range(0f, 1f), OnValueChanged("BuildMesh")]
	private float arcAmount = 0.5f;

	private float alphaIter = 0.01f;

	[SerializeField, AutoCache]
	private SplinePath spline = null;

	private MeshFilter mf = null;
	private MeshCollider meshCollider = null;

	private List<Vector3> verts = new List<Vector3>();
	private List<int> indices = new List<int>();
	private List<Vector3> normals = new List<Vector3>();

	private Mesh mesh = null;

	private void Awake()
	{
		if(spline == null)
			spline = GetComponent<SplinePath>();

		spline.OnPathChanged -= Path_OnPathChanged;
	}

	private void Path_OnPathChanged(PathBase inPath)
	{
		if (spline == inPath)
			BuildMesh();
		else
			inPath.OnPathChanged -= Path_OnPathChanged;
	}

	private void OnValidate()
	{
		if (thickness > radius)
			thickness = radius;
	}

	[Button("Build Mesh")]
	public void BuildMesh()
	{
		if (spline == null)
			spline = GetComponent<SplinePath>();

		mf = GetComponent<MeshFilter>();

		meshCollider = GetComponent<MeshCollider>();
		if (meshCollider == null)
			meshCollider = gameObject.AddComponent<MeshCollider>();

		if(mf && spline)
		{
			if (mf.sharedMesh != null)
				DestroyImmediate(mf.sharedMesh);

			if(mesh == null)
				mesh = new Mesh();

			verts.Clear();
			indices.Clear();
			normals.Clear();

			Vector3 lastPos = spline.GetPoint(0f);

			List<float> iters = new List<float>();
			iters.Add(0f);

			float iter = 0f;
			while(iter < 1f)
			{
				float moveDist = spacing;
				while(moveDist > 0f && iter < 1f)
				{
					float prevIter = iter;
					iter += alphaIter;
					iter = Mathf.Clamp01(iter);

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

						iters.Add(iter);

						moveDist = 0f;
					}
				}
			}

			iters[iters.Count - 1] = 1f;

			for(int i =0; i < iters.Count; i++)
				AddSideFaces(iters[i], i);

			if (arcAmount < 1f)
			{
				for (int i = 0; i < iters.Count; i++)
					AddTopFace(iters[i], i);
			}

			AddFrontBackFaces(iters[0], 0);
			AddFrontBackFaces(iters[iters.Count - 1], iters.Count - 1);

			mesh.SetVertices(verts);
			mesh.SetTriangles(indices, 0);
			mesh.SetNormals(normals);

			mf.sharedMesh = mesh;
			meshCollider.sharedMesh = mesh;

			OnMeshChanged(this);
		}
	}

	private void AddSideFaces(float iter, int numIter)
	{
		Vector3 point = spline.GetPoint(iter);
		Vector3 forward = spline.GetVelocity(iter).normalized;
		Vector3 right = Vector3.Cross(transform.up, forward).normalized;
		Vector3 up = Vector3.Cross(forward, right).normalized;
		right = Vector3.Cross(up, forward).normalized;

		int numVerts = verts.Count;
		int startNum = subdivisions * numIter;
		for (int i = startNum; i < startNum + subdivisions; i++)
		{
			float alpha = ((i - startNum)/ (float)(subdivisions - 1)) - 0.5f;
			float angle = Mathf.PI * 2f * arcAmount;

			Vector3 offset = right * Mathf.Sin(alpha * angle) + -up * Mathf.Cos(alpha * angle);
			Vector3 outerVert = point + offset * radius;
			Vector3 innerVert = point + offset * (radius - thickness);

			verts.Add(transform.InverseTransformPoint(outerVert));
			normals.Add(transform.InverseTransformVector(offset.normalized));

			verts.Add(transform.InverseTransformPoint(innerVert));
			normals.Add(transform.InverseTransformVector(-offset.normalized));

			if (numIter > 0 && i > startNum)
			{
				// Outer face
				indices.Add(i * 2);
				indices.Add((i - 1) * 2);
				indices.Add((i - 1 - subdivisions) * 2);

				indices.Add(i * 2);
				indices.Add((i - 1 - subdivisions) * 2);
				indices.Add((i - subdivisions) * 2);

				// Inner face
				indices.Add((i - 1 - subdivisions) * 2 + 1);
				indices.Add((i - 1) * 2 + 1);
				indices.Add(i * 2 + 1);

				indices.Add((i - subdivisions) * 2 + 1);
				indices.Add((i - 1 - subdivisions) * 2 + 1);
				indices.Add(i * 2 + 1);
			}
		}
	}

	private void AddTopFace(float iter, int numIter)
	{
		Vector3 point = spline.GetPoint(iter);
		Vector3 forward = spline.GetVelocity(iter).normalized;
		Vector3 right = Vector3.Cross(transform.up, forward).normalized;
		Vector3 up = Vector3.Cross(forward, right).normalized;
		right = Vector3.Cross(up, forward).normalized;

		int startNum = subdivisions * numIter;
		for (int i = startNum; i < startNum + subdivisions; i++)
		{
			if (i > startNum && i < startNum + subdivisions - 1)
				continue;

			float alpha = ((i - startNum) / (float)(subdivisions - 1)) - 0.5f;
			float angle = Mathf.PI * 2f * arcAmount;

			Vector3 offset = right * Mathf.Sin(alpha * angle) + -up * Mathf.Cos(alpha * angle);
			Vector3 outerVert = point + offset * radius;
			Vector3 innerVert = point + offset * (radius - thickness);

			verts.Add(transform.InverseTransformPoint(outerVert));
			normals.Add(transform.InverseTransformVector(up));

			verts.Add(transform.InverseTransformPoint(innerVert));
			normals.Add(transform.InverseTransformVector(up));

			if (numIter > 0)
			{
				int index = verts.Count - 2;

				if (i == startNum)
				{
					// Left Top
					indices.Add(index);
					indices.Add(index + 1);
					indices.Add(index - 4);

					indices.Add(index - 4);
					indices.Add(index + 1);
					indices.Add(index - 3);
				}

				if (i == startNum + subdivisions - 1)
				{
					// Right Top
					indices.Add(index - 4);
					indices.Add(index + 1);
					indices.Add(index);

					indices.Add(index - 3);
					indices.Add(index + 1);
					indices.Add(index - 4);
				}
			}
		}
	}

	private void AddFrontBackFaces(float iter, int numIter)
	{
		Vector3 point = spline.GetPoint(iter);
		Vector3 forward = spline.GetVelocity(iter).normalized;
		Vector3 right = Vector3.Cross(transform.up, forward).normalized;
		Vector3 up = Vector3.Cross(forward, right).normalized;
		right = Vector3.Cross(up, forward).normalized;

		int numVerts = verts.Count;
		int startNum = subdivisions * numIter;
		for (int i = startNum; i < startNum + subdivisions; i++)
		{
			float alpha = ((i - startNum) / (float)(subdivisions - 1)) - 0.5f;
			float angle = Mathf.PI * 2f * arcAmount;

			Vector3 offset = right * Mathf.Sin(alpha * angle) + -up * Mathf.Cos(alpha * angle);
			Vector3 outerVert = point + offset * radius;
			Vector3 innerVert = point + offset * (radius - thickness);

			verts.Add(transform.InverseTransformPoint(outerVert));
			verts.Add(transform.InverseTransformPoint(innerVert));

			int index = verts.Count - 2;

			if (numIter == 0)
			{
				normals.Add(transform.InverseTransformVector(-forward));
				normals.Add(transform.InverseTransformVector(-forward));

				if (i > startNum)
				{
					indices.Add(index + 1);
					indices.Add(index);
					indices.Add(index - 2);

					indices.Add(index -1);
					indices.Add(index + 1);
					indices.Add(index - 2);
				}
			}
			else if (iter == 1f)
			{
				normals.Add(transform.InverseTransformVector(forward));
				normals.Add(transform.InverseTransformVector(forward));

				if (i > startNum)
				{
					indices.Add(index - 2);
					indices.Add(index);
					indices.Add(index + 1);

					indices.Add(index - 2);
					indices.Add(index + 1);
					indices.Add(index - 1);
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (spline != null)
		{
			spline.OnPathChanged -= Path_OnPathChanged;
			spline.OnPathChanged += Path_OnPathChanged;
		}
	}
}
