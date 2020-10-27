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
public abstract class PathMesh : MonoBehaviour, ICustomMesh
{
	public event System.Action<ICustomMesh> OnMeshChanged = delegate { };

	[SerializeField, AutoCache]
	protected PathBase path = null;

	[SerializeField]
	protected Mesh mesh = null;

	protected MeshFilter mf = null;
	protected MeshCollider meshCollider = null;

	[SerializeField, Range(0.1f, 3f), OnValueChanged("BuildMesh")]
	protected float spacing = 0.3f;

	[SerializeField, Range(0f, 1f), OnValueChanged("BuildMesh")]
	protected float fillAmount = 1f;
	public float FillAmount
	{ get { return fillAmount; } }

	public void SetFillAmount(float inFillAmount)
	{
		if (fillAmount != inFillAmount)
		{
			fillAmount = inFillAmount;
			BuildMesh();
		}
	}

	[SerializeField]
	protected bool markDynamic = false;

	[SerializeField, OnValueChanged("BuildMesh")]
	protected bool variableWidth = false;

	[SerializeField, ShowIf("variableWidth"), OnValueChanged("BuildMesh")]
	protected AnimationCurve widthCurve = new AnimationCurve();

	[SerializeField, OnValueChanged("BuildMesh")]
	protected bool colorVerts = false;

	[SerializeField, ShowIf("colorVerts"), OnValueChanged("BuildMesh")]
	protected Gradient colorGradient = new Gradient();

	protected float alphaIter = 0.01f;

	protected List<Vector3> verts = new List<Vector3>();
	protected List<int> indices = new List<int>();
	protected List<Vector3> normals = new List<Vector3>();
	protected List<Vector2> uvs = new List<Vector2>();
	protected List<Color> colors = new List<Color>();

	protected float pathLength = 0f;

	private bool HasLinePath()
	{ return path && path is LinePath; }

	[SerializeField, ShowIf("HasLinePath"), OnValueChanged("BuildMesh")]
	private float lineCornerFalloff = 0.05f;

	protected List<Vector3> pointDirs = new List<Vector3>();
	protected List<float> pointIters = new List<float>();

	protected List<float> dists = new List<float>();
	protected List<float> iters = new List<float>();

	private void Awake()
	{
		if (path == null)
			path = GetComponent<PathBase>();

		if (path != null)
		{
			path.OnPathChanged -= Path_OnPathChanged;
			if (!Application.IsPlaying(this))
				BuildMesh();
		}
	}

	private void Path_OnPathChanged(PathBase inPath)
	{
		if (path == inPath)
			BuildMesh();
		else
			inPath.OnPathChanged -= Path_OnPathChanged;
	}

	[Button("Build Mesh")]
	public void BuildMesh()
	{
		if (this == null)
			return;

		if (path == null)
			path = GetComponent<PathBase>();

		mf = GetComponent<MeshFilter>();
		meshCollider = GetComponent<MeshCollider>();

		if (mf && path)
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

			Vector3 lastPos = path.GetPoint(0f);

			Vector3 normal = -path.GetDirection(0f);
			verts.Add(transform.InverseTransformPoint(lastPos + normal * path.Radius / 2f));
			normals.Add(transform.InverseTransformVector(normal));
			uvs.Add(new Vector2(0f, 0f));

			if (colorVerts)
				colors.Add(colorGradient.Evaluate(0f));

			pathLength = path.GetLength() * fillAmount;

			dists.Clear();
			iters.Clear();

			iters.Add(0f);
			dists.Add(0f);

			float totalDist = 0f;

			float iter = 0f;
			while (iter < fillAmount)
			{
				float moveDist = spacing;
				while (moveDist > 0f && iter < fillAmount)
				{
					float prevIter = iter;
					iter += alphaIter;
					iter = Mathf.Clamp(iter, 0f, fillAmount);

					Vector3 pos = path.GetPoint(iter);
					Vector3 toPos = pos - lastPos;
					float toPosDist = toPos.magnitude;

					if (toPosDist < moveDist)
					{
						moveDist -= toPosDist;
						totalDist += toPosDist;

						lastPos = pos;
					}
					else
					{

						float alpha = moveDist / toPosDist;
						iter = Mathf.Lerp(prevIter, iter, alpha);
						lastPos = Vector3.Lerp(lastPos, pos, alpha);

						iters.Add(iter);

						totalDist += moveDist;
						dists.Add(totalDist);

						moveDist = 0f;
					}
				}
			}

			iters.Add(fillAmount);
			dists.Add(totalDist);

			LinePath linePath = path as LinePath;
			if (linePath)
			{
				// THIS IS A BAD ATTEMPT AT MAKING LINEPATHS WORK. NOT OFFICIALLY SUPPORTED
				int numPoints = linePath.Loop ? linePath.Points.Length : (linePath.Points.Length - 1);
				float alphaPerPoint = fillAmount / (float)numPoints;

				pointDirs.Clear();
				pointIters.Clear();
				for (int i = 1; i < numPoints; i++)
				{
					float alpha = alphaPerPoint * i;
					Vector3 preDir = linePath.GetDirection(alpha - SplineUtils.DefaultTraverseAlphaSpeed);
					Vector3 postDir = linePath.GetDirection(alpha + SplineUtils.DefaultTraverseAlphaSpeed);

					Vector3 dir = (preDir + postDir) / 2f;
					pointDirs.Add(dir);
					pointIters.Add(alpha);

					for (int j = 0; j < iters.Count; j++)
					{
						if (iters[j] > alpha)
						{
							iters.Insert(j, alpha);
							dists.Insert(j, alpha); // Cursed
							break;
						}
					}
				}

				for (int i = 0; i < iters.Count; i++)
				{
					Vector3 direction = path.GetDirection(iters[i]);

					float nearestDist = Mathf.Infinity;
					Vector3 nearestDir = direction;
					for (int j = 0; j < pointIters.Count; j++)
					{
						float iterDist = Mathf.Abs(pointIters[j] - iters[i]);
						if (iterDist < nearestDist && iterDist < lineCornerFalloff)
						{
							float alpha = Mathf.InverseLerp(lineCornerFalloff, 0f, iterDist);
							nearestDir = Vector3.Lerp(direction, pointDirs[j], alpha);
							nearestDist = iterDist;
						}
					}

					AddEdgeLoop(iters[i], i, nearestDir);
				}
			}
			else
			{
				for (int i = 0; i < iters.Count; i++)
					AddEdgeLoop(iters[i], i);
			}

			normal = path.GetDirection(fillAmount);
			verts.Add(transform.InverseTransformPoint(lastPos + normal * path.Radius / 2f));
			uvs.Add(new Vector2(1, pathLength));
			normals.Add(transform.InverseTransformVector(normal));

			if (colorVerts)
				colors.Add(colorGradient.Evaluate(1f));

			mesh.SetVertices(verts);
			mesh.SetTriangles(indices, 0);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);

			if (colorVerts)
				mesh.SetColors(colors);

			mf.sharedMesh = mesh;
			
			if(meshCollider != null)
				meshCollider.sharedMesh = mesh;

			OnMeshChanged(this);
		}
	}

	protected abstract void AddEdgeLoop(float iter, int numIter, Vector3? forwardOverride = null);

	private void OnDrawGizmosSelected()
	{
		if (path != null)
		{
			path.OnPathChanged -= Path_OnPathChanged;
			path.OnPathChanged += Path_OnPathChanged;
		}
	}
}
