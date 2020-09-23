using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

// Based on https://assetstore.unity.com/packages/tools/particles-effects/simple-decal-system-13889
// and http://blog.wolfire.com/2009/06/how-to-project-decals/

[ExecuteInEditMode()]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Decal : MonoBehaviour
{
	private MeshRenderer meshRenderer = null;
	public MeshRenderer MeshRenderer
	{
		get
		{
			if (meshRenderer == null)
				meshRenderer = GetComponent<MeshRenderer>();

			return meshRenderer;
		}
	}

	private MeshFilter meshFilter = null;
	public MeshFilter MeshFilter
	{
		get
		{
			if (meshFilter == null)
				meshFilter = GetComponent<MeshFilter>();

			return meshFilter;
		}
	}

	[SerializeField]
	private Mesh serializedMesh = null;

	[SerializeField]
	private Material material = null;

	[SerializeField]
	private Sprite sprite = null;

	private static Vector2[] spriteUVs = new Vector2[4];

	private static Vector3[] cornerOffsets = new Vector3[]
	{
		new Vector3(-0.5f, -0.5f, -0.5f), // left bottom back
		new Vector3(0.5f, -0.5f, -0.5f), // right bottom back
		new Vector3(-0.5f, -0.5f, 0.5f), // left bottom front
		new Vector3(0.5f, -0.5f, 0.5f), // right bottom front
		new Vector3(-0.5f, 0.5f, -0.5f), // left top back
		new Vector3(0.5f, 0.5f, -0.5f), // right top back
		new Vector3(-0.5f, 0.5f, 0.5f), // left top front
		new Vector3(0.5f, 0.5f, 0.5f), // right top front
	};

	private static Plane[] planes = new Plane[]
	{
		new Plane(Vector3.right, 0.5f),
		new Plane(Vector3.left, 0.5f),
		new Plane(Vector3.up, 0.5f),
		new Plane(Vector3.down, 0.5f),
		new Plane(Vector3.forward, 0.5f),
		new Plane(Vector3.back, 0.5f),
	};

	private static float maxAngle = 70f;
	private static float surfaceOffset = 0.005f;

	private static List<Vector3> tempCorners = new List<Vector3>();

	private static List<Vector3> checkVerts = new List<Vector3>();
	private static List<Vector3> clippedVerts = new List<Vector3>();

	private static List<int> tris = new List<int>();
	private static List<Vector3> verts = new List<Vector3>();

	private static List<int> decalTris = new List<int>();
	private static List<Vector3> decalVerts = new List<Vector3>();
	private static List<Vector3> decalNormals = new List<Vector3>();
	private static List<Vector2> decalUVs = new List<Vector2>();

	private static List<Vector3> localVerts = new List<Vector3>();
	private static Dictionary<Vector3, int> vertToIndexMap = new Dictionary<Vector3, int>();

	private void Awake()
	{
		if (serializedMesh != null)
			MeshFilter.sharedMesh = serializedMesh;
		else
			BuildDecal();
	}

	public void BuildDecal()
	{
		vertToIndexMap.Clear();
		tempCorners.Clear();

		Vector3 center = Vector3.zero;
		for(int i = 0; i < cornerOffsets.Length; i++)
		{
			Vector3 corner = transform.TransformPoint(cornerOffsets[i]);
			center += corner;

			tempCorners.Add(corner);
		}

		Bounds objectBounds = new Bounds();
		objectBounds.center = center / (float)tempCorners.Count;
		for (int i = 0; i < tempCorners.Count; i++)
			objectBounds.Encapsulate(tempCorners[i]);

		decalTris.Clear();
		decalVerts.Clear();

		MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
		for(int i =0; i < meshFilters.Length; i++)
		{
			MeshFilter mf = meshFilters[i];
			if (mf.sharedMesh == null)
				continue;

			MeshRenderer mr = mf.GetComponent<MeshRenderer>();
			if (mr == null || !mr.enabled)
				continue;

			Decal decal = mf.GetComponent<Decal>();
			if (decal != null)
				continue;

			Rigidbody rigidbody = mf.GetComponentInParent<Rigidbody>();
			if (rigidbody && !rigidbody.isKinematic)
				continue;

			Bounds otherBounds = mr.bounds;
			if (!objectBounds.Intersects(otherBounds) && !objectBounds.Contains(otherBounds.center))
				continue;

			tempCorners.Clear();

			Vector3 otherCenter = Vector3.zero;
			Bounds otherLocalBounds = mf.sharedMesh.bounds;
			for(int j =0; j < cornerOffsets.Length; j++)
			{
				Vector3 scaledCornerOffset =  cornerOffsets[j];
				scaledCornerOffset.Scale(otherLocalBounds.size);

				Vector3 otherCorner = mf.transform.TransformPoint(scaledCornerOffset);
				otherCorner += mf.transform.TransformPoint(otherLocalBounds.center) - mf.transform.position;
				tempCorners.Add(otherCorner);

				otherCenter += otherCorner;
			}

			Bounds otherObjectBounds = new Bounds();
			otherObjectBounds.center = otherCenter / (float)cornerOffsets.Length;
			for (int j = 0; j < tempCorners.Count; j++)
				otherObjectBounds.Encapsulate(tempCorners[j]);

			if (!objectBounds.Intersects(otherObjectBounds) && !objectBounds.Contains(otherObjectBounds.center))
				continue;

			tris.Clear();
			verts.Clear();
			localVerts.Clear();

			if(!mf.sharedMesh.isReadable)
			{
				Debug.LogWarning("Mesh not marked readable: " + mf.sharedMesh.name, mf);
				continue;
			}

			for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
			{
				mf.sharedMesh.GetTriangles(tris, s);
				mf.sharedMesh.GetVertices(verts);

				for (int j = 0; j < verts.Count; j++)
				{
					Vector3 worldPos = mf.transform.TransformPoint(verts[j]);
					Vector3 decalPos = transform.InverseTransformPoint(worldPos);
					localVerts.Add(decalPos);
				}

				for (int j = 0; j < tris.Count; j += 3)
				{
					Vector3 a = localVerts[tris[j]];
					Vector3 b = localVerts[tris[j + 1]];
					Vector3 c = localVerts[tris[j + 2]];

					Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
					if (Vector3.Angle(normal, Vector3.up) > maxAngle)
						continue;

					checkVerts.Clear();
					checkVerts.Add(a);
					checkVerts.Add(b);
					checkVerts.Add(c);

					clippedVerts.Clear();

					for (int k = 0; k < planes.Length; k++)
					{
						for (int l = 0; l < checkVerts.Count; l++)
						{
							Vector3 pos = checkVerts[l];
							Vector3 next = checkVerts[(l + 1) % checkVerts.Count];

							if (planes[k].GetSide(pos))
								clippedVerts.Add(pos);

							if (planes[k].GetSide(pos) != planes[k].GetSide(next))
							{
								Ray ray = new Ray(pos, next - pos);

								float dist = 0f;
								planes[k].Raycast(ray, out dist);
								clippedVerts.Add(ray.GetPoint(dist));
							}
						}

						checkVerts.Clear();
						checkVerts.AddRange(clippedVerts);
						clippedVerts.Clear();
					}

					if (checkVerts.Count >= 3)
					{
						int index = GetIndex(checkVerts[0], normal);

						for (int k = 1; k < checkVerts.Count - 1; k++)
						{
							decalTris.Add(index);

							int index2 = GetIndex(checkVerts[k], normal);
							decalTris.Add(index2);

							int index3 = GetIndex(checkVerts[k + 1], normal);
							decalTris.Add(index3);
						}
					}
				}
			}
		}

		Mesh mesh = new Mesh();
		mesh.name = "DecalMesh";
		mesh.hideFlags = HideFlags.HideAndDontSave;

		if (decalVerts.Count > 0 && decalTris.Count > 0)
		{
			if (sprite != null && sprite.texture != null)
			{
				Vector2 min = sprite.textureRect.min;
				min.x /= sprite.texture.width;
				min.y /= sprite.texture.height;

				Vector2 max = sprite.textureRect.max;
				max.x /= sprite.texture.width;
				max.y /= sprite.texture.height;

				spriteUVs[0] = new Vector2(min.x, max.y);
				spriteUVs[1] = new Vector2(max.x, max.y);
				spriteUVs[2] = new Vector2(max.x, min.y);
				spriteUVs[3] = new Vector2(min.x, min.y);
			}

			decalUVs.Clear();
			decalNormals.Clear();
			for (int k = 0; k < decalVerts.Count; k++)
			{
				Vector3 vert = decalVerts[k];

				Vector2 uv = new Vector2(vert.x + 0.5f, vert.z + 0.5f);
				uv = BilinearInterpolation(spriteUVs[0], spriteUVs[1], spriteUVs[2], spriteUVs[3], uv.x, 1f - uv.y);
				decalUVs.Add(uv);

				decalNormals.Add(Vector3.zero);
			}

			for(int k = 0; k < decalTris.Count; k+=3)
			{
				int index = decalTris[k];
				int index2 = decalTris[k+1];
				int index3 = decalTris[k+2];

				Vector3 a = decalVerts[index];
				Vector3 b = decalVerts[index2];
				Vector3 c = decalVerts[index3];

				Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

				decalNormals[index] += normal;
				decalNormals[index2] += normal;
				decalNormals[index3] += normal;
			}

			for(int k = 0; k < decalNormals.Count; k++)
				decalNormals[k] = decalNormals[k].normalized;

			float worldOffset = surfaceOffset / transform.lossyScale.y;

			for (int k = 0; k < decalNormals.Count; k++)
				decalVerts[k] += decalNormals[k] * worldOffset;

			mesh.SetVertices(decalVerts);
			mesh.SetNormals(decalNormals);
			mesh.SetTriangles(decalTris, 0);
			mesh.SetUVs(0, decalUVs);
		}

		MeshFilter.sharedMesh = serializedMesh = mesh;
		MeshRenderer.sharedMaterial = material;
	}

	public Vector2 BilinearInterpolation(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u, float v)
	{
		Vector2 abu = Vector2.Lerp(a, b, u);
		Vector2 dcu = Vector2.Lerp(d, c, u);
		return Vector2.Lerp(abu, dcu, v);
	}

	private int GetIndex(Vector3 point, Vector3 normal)
	{
		Vector3 lowPrecisionVert = point * 100f;
		lowPrecisionVert.x = Mathf.Round(lowPrecisionVert.x);
		lowPrecisionVert.y = Mathf.Round(lowPrecisionVert.y);
		lowPrecisionVert.z = Mathf.Round(lowPrecisionVert.z);
		lowPrecisionVert /= 100f;

		if(vertToIndexMap.TryGetValue(lowPrecisionVert, out int index))
			return index;
		
		decalVerts.Add(point);

		int newIndex = decalVerts.Count - 1;
		vertToIndexMap[lowPrecisionVert] = newIndex;
		return newIndex;
	}

	private void OnDrawGizmosSelected()
	{
		Matrix4x4 prevMatrix = Gizmos.matrix;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
		Gizmos.matrix = prevMatrix;
	}
}
