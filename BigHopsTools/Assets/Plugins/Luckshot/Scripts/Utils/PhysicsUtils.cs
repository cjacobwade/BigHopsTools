using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class PhysicsUtils
{
	private static Collider[] overlapResults = new Collider[100];
	private static HashSet<Collider> uniqueOverlapResults = new HashSet<Collider>();

	private static Vector3[] tempCorners = new Vector3[8];

	public static Bounds EncapsulateColliders(IEnumerable<Collider> colliders, Transform relativeTransform = null)
	{
		Bounds bounds = new Bounds();
		bounds.center = Vector3.zero;
		bounds.size = Vector3.zero;

		bool setupCenter = false;
		bool hasRelativeTransform = relativeTransform != null;

		foreach (var collider in colliders)
		{
			if (collider.isTrigger || !collider.enabled || 
				!collider.gameObject.activeInHierarchy)
				continue;

			Vector3 size = Vector3.zero;
			Vector3 center = Vector3.zero;

			Transform colliderTransform = collider.transform;
			Vector3 lossyScale = colliderTransform.lossyScale;

			BoxCollider boxCollider = collider as BoxCollider;
			if (boxCollider != null)
			{
				size = boxCollider.size;
				size.Scale(lossyScale);

				center = colliderTransform.TransformPoint(boxCollider.center);
			}
			else
			{
				SphereCollider sphereCollider = collider as SphereCollider;
				if (sphereCollider != null)
				{
					float radius = sphereCollider.radius * Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
					size = new Vector3(radius * 2f, radius * 2f, radius * 2f);
					center = colliderTransform.TransformPoint(sphereCollider.center);
				}
				else
				{
					CapsuleCollider capsuleCollider = collider as CapsuleCollider;
					if (capsuleCollider != null)
					{
						float radius = capsuleCollider.radius;
						float height = capsuleCollider.height;
						int direction = capsuleCollider.direction;

						if (capsuleCollider.direction == 0) // X
						{
							radius *= Mathf.Max(lossyScale.y, lossyScale.z);
							height = Mathf.Max(radius, lossyScale.x * height);
						}
						else if (capsuleCollider.direction == 1) // Y
						{
							radius *= Mathf.Max(lossyScale.x, lossyScale.z);
							height = Mathf.Max(radius, lossyScale.y * height);
						}
						else // Z
						{
							radius *= Mathf.Max(lossyScale.x, lossyScale.y);
							height = Mathf.Max(radius, lossyScale.z * height);
						}

						size = Vector3.zero;
						for (int i = 0; i < 3; i++)
							size[i] += (i == direction) ? height : (radius * 2f);

						center = colliderTransform.TransformPoint(capsuleCollider.center);
					}
					else
					{
						MeshCollider meshCollider = collider as MeshCollider;
						if(meshCollider != null && meshCollider.sharedMesh != null)
						{
							Bounds meshBounds = meshCollider.sharedMesh.bounds;

							center = colliderTransform.TransformPoint(meshBounds.center);

							size = meshBounds.size;
							size.Scale(lossyScale);
						}
					}
				}
			}

			Vector3 extents = size / 2f;

			Vector3 forward = colliderTransform.forward * extents.z;
			Vector3 right = colliderTransform.right * extents.x;
			Vector3 up = colliderTransform.up * extents.y;

			tempCorners[0] = center - up - right - forward;
			tempCorners[1] = center - up - right + forward;
			tempCorners[2] = center - up + right - forward;
			tempCorners[3] = center - up + right + forward;

			tempCorners[4] = center + up - right - forward;
			tempCorners[5] = center + up - right + forward;
			tempCorners[6] = center + up + right - forward;
			tempCorners[7] = center + up + right + forward;

			if (hasRelativeTransform)
			{
				for (int i = 0; i < tempCorners.Length; i++)
					bounds.Encapsulate(relativeTransform.InverseTransformPoint(tempCorners[i]));
			}
			else
			{
				if (!setupCenter)
				{
					bounds.center = tempCorners[0];
					setupCenter = true;
				}

				for (int i = 0; i < tempCorners.Length; i++)
					bounds.Encapsulate(tempCorners[i]);
			}
		}

		return bounds;
	}

	public static Bounds EncapsulateMeshFilters(IEnumerable<MeshFilter> meshFilters, Transform relativeTransform = null)
	{
		Bounds bounds = new Bounds();
		bounds.center = Vector3.zero;
		bounds.size = Vector3.zero;

		bool hasRelativeTransform = relativeTransform != null;

		foreach (var meshFilter in meshFilters)
		{
			if (meshFilter.sharedMesh == null)
				continue;

			Transform mfTransform = meshFilter.transform;
			Vector3 lossyScale = mfTransform.lossyScale;

			Bounds meshBounds = meshFilter.sharedMesh.bounds;

			Vector3 center = mfTransform.TransformPoint(meshBounds.center);

			Vector3 size = meshBounds.size;
			size.Scale(lossyScale);

			Vector3 extents = size / 2f;

			Vector3 forward = mfTransform.forward * extents.z;
			Vector3 right = mfTransform.right * extents.x;
			Vector3 up = mfTransform.up * extents.y;

			tempCorners[0] = center - up - right - forward;
			tempCorners[1] = center - up - right + forward;
			tempCorners[2] = center - up + right - forward;
			tempCorners[3] = center - up + right + forward;

			tempCorners[4] = center + up - right - forward;
			tempCorners[5] = center + up - right + forward;
			tempCorners[6] = center + up + right - forward;
			tempCorners[7] = center + up + right + forward;

			if (hasRelativeTransform)
			{
				for (int i = 0; i < tempCorners.Length; i++)
					bounds.Encapsulate(relativeTransform.InverseTransformPoint(tempCorners[i]));
			}
			else
			{
				for (int i = 0; i < tempCorners.Length; i++)
					bounds.Encapsulate(tempCorners[i]);
			}
		}

		return bounds;
	}

	public static Vector3 GetBarycentricCoord(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 v0 = b - a;
		Vector3 v1 = c - a;
		Vector3 v2 = p - a;

		float d00 = Vector3.Dot(v0, v0);
		float d01 = Vector3.Dot(v0, v1);
		float d11 = Vector3.Dot(v1, v1);
		float d20 = Vector3.Dot(v2, v0);
		float d21 = Vector3.Dot(v2, v1);
		float denom = d00 * d11 - d01 * d01;

		float v = (d11 * d20 - d01 * d21) / denom;
		float w = (d00 * d21 - d01 * d20) / denom;
		float u = 1.0f - v - w;

		return new Vector3(u, v, w);
	}

	public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
	{
		Vector3 linePointToPoint = point - linePoint;
		float t = Vector3.Dot(linePointToPoint, lineVec);
		return linePoint + lineVec * t;
	}

	public static Vector3 ProjectPointOnLineSegment(Vector3 a, Vector3 b, Vector3 point)
	{
		Vector3 vector = b - a;
		Vector3 projectedPoint = ProjectPointOnLine(a, vector.normalized, point);

		int side = PointOnWhichSide(a, b, projectedPoint);

		if (side == 0)
			return projectedPoint;

		if (side == 1)
			return a;

		if (side == 2)
			return b;

		return Vector3.zero;
	}

	private static int PointOnWhichSide(Vector3 a, Vector3 b, Vector3 point)
	{
		Vector3 lineVec = b - a;
		Vector3 pointVec = point - a;

		float dot = Vector3.Dot(pointVec, lineVec);

		//point is on side of linePoint2, compared to linePoint1
		if (dot > 0)
		{
			//point is on the line segment
			if (pointVec.magnitude <= lineVec.magnitude)
				return 0;

			//point is not on the line segment and it is on the side of linePoint2
			else
				return 2;
		}

		//Point is not on side of linePoint2, compared to linePoint1.
		//Point is not on the line segment and it is on the side of linePoint1.
		else
			return 1;
	}

#if UNITY_EDITOR
	[MenuItem("Tools/Normalize Colliders In Selection %&#n", validate = true)]
	public static bool NormalizeCollidersInSelectionValidate()
	{ return Selection.gameObjects != null; }

	[MenuItem("Tools/Normalize Colliders In Selection %&#n")]
	public static void NormalizeCollidersInSelection()
	{
		foreach(var go in Selection.gameObjects)
		{
			Collider[] colliders = go.GetComponents<Collider>();
			foreach (var collider in colliders)
			{
				NormalizeCollider(collider);
			}
		}
	}
#endif

	public static void NormalizeCollider(Collider collider)
	{
		Vector3 localScale = collider.transform.localScale;
		collider.transform.localScale = Vector3.one;

		BoxCollider boxCollider = collider as BoxCollider;
		if (boxCollider != null)
		{
			Vector3 size = boxCollider.size;
			size.Scale(localScale);

			boxCollider.size = size;
			return;
		}

		SphereCollider sphereCollider = collider as SphereCollider;
		if (sphereCollider != null)
		{
			sphereCollider.radius *= Mathf.Max(localScale.x, localScale.y, localScale.z);
			return;
		}

		CapsuleCollider capsuleCollider = collider as CapsuleCollider;
		if (capsuleCollider != null)
		{
			float radius = capsuleCollider.radius;
			float height = capsuleCollider.height;

			if(capsuleCollider.direction == 0) // X
			{
				radius = capsuleCollider.radius * Mathf.Max(localScale.y, localScale.z);
				height = Mathf.Max(radius, localScale.x * capsuleCollider.height);
			}
			else if(capsuleCollider.direction == 1) // Y
			{
				radius = capsuleCollider.radius * Mathf.Max(localScale.x, localScale.z);
				height = Mathf.Max(radius, localScale.y * capsuleCollider.height);
			}
			else // Z
			{
				radius = capsuleCollider.radius * Mathf.Max(localScale.x, localScale.y);
				height = Mathf.Max(radius, localScale.z * capsuleCollider.height);
			}

			capsuleCollider.radius = radius;
			capsuleCollider.height = height;
			return;
		}
	}

	public static int GetAllOverlaps(Collider[] colliders, ref List<Collider> overlapColliders, int layerMask = ~0, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal)
	{
		// TODO: Make this non alloc with a pre-initilized array of colliders

		overlapColliders.Clear();
		uniqueOverlapResults.Clear();

		foreach (var collider in colliders)
		{
			BoxCollider boxCollider = collider as BoxCollider;
			if(boxCollider != null)
			{
				Vector3 boxExtents = boxCollider.size / 2f;
				boxExtents.Scale(boxCollider.transform.localScale);

				int numOverlaps = Physics.OverlapBoxNonAlloc(
					boxCollider.transform.position + boxCollider.center, 
					boxExtents, 
					overlapResults,
					boxCollider.transform.rotation, 
					layerMask, 
					queryTriggers);

				for(int i =0; i < numOverlaps; i++)
					uniqueOverlapResults.Add(overlapResults[i]);

				continue;
			}

			SphereCollider sphereCollider = collider as SphereCollider;
			if(sphereCollider != null)
			{
				int numOverlaps = Physics.OverlapSphereNonAlloc(
					sphereCollider.transform.position + sphereCollider.center,
					sphereCollider.radius * sphereCollider.transform.localScale.x,
					overlapResults,
					layerMask,
					queryTriggers);

				for (int i = 0; i < numOverlaps; i++)
					uniqueOverlapResults.Add(overlapResults[i]);

				continue;
			}

			CapsuleCollider capsuleCollider = collider as CapsuleCollider;
			if(capsuleCollider != null)
			{
				Vector3 pointOffset = Vector3.up * (capsuleCollider.height / 2f - capsuleCollider.radius);
				Vector3 p1 = capsuleCollider.transform.TransformPoint(pointOffset);
				Vector3 p2 = capsuleCollider.transform.TransformPoint(-pointOffset);

				int numOverlaps = Physics.OverlapCapsuleNonAlloc(
					p1, p2,
					capsuleCollider.radius,
					overlapResults,
					layerMask,
					queryTriggers);

				for (int i = 0; i < numOverlaps; i++)
					uniqueOverlapResults.Add(overlapResults[i]);

				continue;
			}
		}

		foreach (var overlap in uniqueOverlapResults)
			overlapColliders.Add(overlap);

		return overlapColliders.Count;
	}

	public static Vector3 ComputeMaxPenetration(Collider[] colliders, int layerMask = ~0, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.UseGlobal)
	{
		Vector3 maxResolveVec = Vector3.zero;

		foreach (var collider in colliders)
		{
			BoxCollider boxCollider = collider as BoxCollider;
			if (boxCollider != null)
			{
				Vector3 boxExtents = boxCollider.size / 2f;
				boxExtents.Scale(boxCollider.transform.localScale);

				Vector3 colliderPos = boxCollider.transform.position + boxCollider.center;

				int numOverlaps = Physics.OverlapBoxNonAlloc(
					colliderPos,
					boxExtents,
					overlapResults,
					boxCollider.transform.rotation,
					layerMask,
					queryTriggers);

				for (int i = 0; i < numOverlaps; i++)
				{
					if (System.Array.IndexOf(colliders, overlapResults[i]) != -1)
						continue;

					Vector3 resolveVec = ComputePenetration(collider, colliderPos, collider.transform.rotation, overlapResults[i]);

					if (Mathf.Abs(resolveVec.x) > Mathf.Abs(maxResolveVec.x))
						maxResolveVec.x = resolveVec.x;

					if (Mathf.Abs(resolveVec.y) > Mathf.Abs(maxResolveVec.y))
						maxResolveVec.y = resolveVec.y;

					if (Mathf.Abs(resolveVec.z) > Mathf.Abs(maxResolveVec.z))
						maxResolveVec.z = resolveVec.z;
				}

				continue;
			}

			SphereCollider sphereCollider = collider as SphereCollider;
			if (sphereCollider != null)
			{
				Vector3 colliderPos = sphereCollider.transform.position + sphereCollider.center;

				Vector3 localScale = sphereCollider.transform.localScale;
				float colliderRadius = sphereCollider.radius * Mathf.Max(
					Mathf.Abs(localScale.x), Mathf.Abs(localScale.y), Mathf.Abs(localScale.z));

				int numOverlaps = Physics.OverlapSphereNonAlloc(
					colliderPos,
					colliderRadius,
					overlapResults,
					layerMask,
					queryTriggers);

				for (int i = 0; i < numOverlaps; i++)
				{
					if (System.Array.IndexOf(colliders, overlapResults[i]) != -1)
						continue;

					Vector3 resolveVec = ComputePenetration(collider, colliderPos, collider.transform.rotation, overlapResults[i]);
					if (Mathf.Abs(resolveVec.x) > Mathf.Abs(maxResolveVec.x))
						maxResolveVec.x = resolveVec.x;

					if (Mathf.Abs(resolveVec.y) > Mathf.Abs(maxResolveVec.y))
						maxResolveVec.y = resolveVec.y;

					if (Mathf.Abs(resolveVec.z) > Mathf.Abs(maxResolveVec.z))
						maxResolveVec.z = resolveVec.z;
				}

				continue;
			}

			CapsuleCollider capsuleCollider = collider as CapsuleCollider;
			if (capsuleCollider != null)
			{
				Vector3 colliderPos = capsuleCollider.transform.position + capsuleCollider.center;

				Vector3 pointOffset = Vector3.up * (capsuleCollider.height / 2f - capsuleCollider.radius);
				Vector3 p1 = capsuleCollider.transform.TransformPoint(pointOffset);
				Vector3 p2 = capsuleCollider.transform.TransformPoint(-pointOffset);

				int numOverlaps = Physics.OverlapCapsuleNonAlloc(
					p1, p2,
					capsuleCollider.radius,
					overlapResults,
					layerMask,
					queryTriggers);

				for (int i = 0; i < numOverlaps; i++)
				{
					if (System.Array.IndexOf(colliders, overlapResults[i]) != -1)
						continue;

					Vector3 resolveVec = ComputePenetration(collider, colliderPos, collider.transform.rotation, overlapResults[i]);
					if (Mathf.Abs(resolveVec.x) > Mathf.Abs(maxResolveVec.x))
						maxResolveVec.x = resolveVec.x;

					if (Mathf.Abs(resolveVec.y) > Mathf.Abs(maxResolveVec.y))
						maxResolveVec.y = resolveVec.y;

					if (Mathf.Abs(resolveVec.z) > Mathf.Abs(maxResolveVec.z))
						maxResolveVec.z = resolveVec.z;
				}

				continue;
			}
		}

		return maxResolveVec;
	}

	private static Vector3 ComputePenetration(Collider a, Vector3 positionA, Quaternion rotationA, Collider b)
	{
		Vector3 positionB = b.transform.position;
		BoxCollider boxCollider = b as BoxCollider;
		if (boxCollider != null)
			positionB = boxCollider.transform.position + boxCollider.center;

		SphereCollider sphereCollider = b as SphereCollider;
		if(sphereCollider != null)
			positionB = sphereCollider.transform.position + sphereCollider.center;

		CapsuleCollider capsuleCollider = b as CapsuleCollider;
		if (capsuleCollider != null)
			positionB = capsuleCollider.transform.position = capsuleCollider.center;

		Vector3 direction = Vector3.zero;
		float distance = 0f;

		Physics.ComputePenetration(
			a, positionA, a.transform.rotation,
			b, positionB, b.transform.rotation,
			out direction,
			out distance);

		return direction * distance;
	}
}
