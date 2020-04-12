using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class MeshCopy : MonoBehaviour
{
	[SerializeField]
	private Transform testSource = null;

	[SerializeField]
	private bool copyColliders = true;

	[SerializeField, ShowIf("copyColliders")]
	private bool copyTriggers = true;

	[SerializeField]
	private bool shrinkToFit = true;

	[SerializeField, ShowIf("shrinkToFit")]
	private Vector3 fitBoundsSize = Vector3.one;

	[SerializeField]
	private bool overrideLayer = true;

	private Transform root = null;
	private Transform meshRoot = null;
	private Transform colliderRoot = null;

	private List<MeshFilter> copiedMeshFilters = new List<MeshFilter>();
	private List<Collider> copiedColliders = new List<Collider>();

	private static MaterialPropertyBlock propertyBlock = null;

	[Button("Build MeshCopy")]
	private void CopyFrom()
	{
		CopyFrom(testSource);
	}

	[Button("Clear MeshCopy")]
	public void Clear()
	{
		if (root != null)
		{
			if (Application.IsPlaying(this))
				Destroy(root.gameObject);
			else
				DestroyImmediate(root.gameObject);
		}
	}

	public void CopyFrom(Transform sourceRoot)
	{
		Clear();

		Transform prevParent = transform.parent;
		Vector3 prevLocalPos = transform.localPosition;
		Quaternion prevLocalRot = transform.localRotation;
		Vector3 prevLocalScale = transform.localScale;

		transform.SetParent(null, true);
		transform.localScale = Vector3.one;

		root = new GameObject("Root").transform; 
		root.SetParent(transform);
		root.ResetLocals();
		root.gameObject.hideFlags = HideFlags.DontSave;
		
		meshRoot = new GameObject("MeshRoot").transform;
		meshRoot.SetParent(root);
		meshRoot.ResetLocals();

		copiedMeshFilters.Clear();

		Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>();
		for(int i = 0; i < renderers.Length; i++)
		{
			Renderer sourceRend = renderers[i];

			Mesh sourceMesh = null;

			MeshRenderer sourceMR = sourceRend as MeshRenderer;
			if (sourceMR != null)
			{
				if (!sourceMR.enabled)
					continue;

				MeshFilter sourceMF = sourceMR.GetComponent<MeshFilter>();
				if (sourceMF != null)
					sourceMesh = sourceMF.sharedMesh;
			}
			else
			{
				SkinnedMeshRenderer smr = sourceRend as SkinnedMeshRenderer;
				if (smr != null)
				{
					if (!smr.enabled)
						continue;

					smr.BakeMesh(sourceMesh);
				}
			}

			if (sourceMesh == null)
				continue;

			GameObject copyGO = new GameObject(sourceRend.name, typeof(MeshFilter), typeof(MeshRenderer));

			copyGO.transform.SetParent(meshRoot.transform);
			copyGO.transform.localScale = sourceRend.transform.lossyScale;
			 
			Vector3 posOffset = sourceRoot.transform.InverseTransformPoint(sourceRend.transform.TransformPoint(-sourceMesh.bounds.center));
			Quaternion rotOffset = Quaternion.Inverse(sourceRoot.rotation) * sourceRend.transform.rotation;

			copyGO.transform.SetPositionAndRotation( 
				meshRoot.TransformPoint(posOffset),
				meshRoot.rotation * rotOffset);

			copyGO.layer = overrideLayer ? gameObject.layer : sourceRend.gameObject.layer;
			copyGO.tag = sourceRend.gameObject.tag;

			MeshFilter copyMF = copyGO.GetComponent<MeshFilter>();
			copyMF.sharedMesh = sourceMesh;

			MeshRenderer copyMR = copyGO.GetComponent<MeshRenderer>();
			copyMR.sharedMaterials = sourceRend.sharedMaterials;
			copyMR.shadowCastingMode = sourceRend.shadowCastingMode;
			copyMR.receiveShadows = sourceRend.receiveShadows;
			copyMR.sortingLayerID = sourceRend.sortingLayerID;
			copyMR.sortingOrder = sourceRend.sortingOrder;

			if (propertyBlock == null)
				propertyBlock = new MaterialPropertyBlock();

			sourceRend.GetPropertyBlock(propertyBlock);

			propertyBlock.SetColor("_SColor", Color.gray);

			copyMR.SetPropertyBlock(propertyBlock);

			copiedMeshFilters.Add(copyMF);
		}

		Bounds renderBounds = PhysicsUtils.EncapsulateMeshFilters(copiedMeshFilters, root);
		Vector3 boundsCenter = renderBounds.center;
		Vector3 boundsSize = renderBounds.size;

		if (copyColliders)
		{
			colliderRoot = new GameObject("ColliderRoot").transform;
			colliderRoot.SetParent(root);
			colliderRoot.ResetLocals();

			copiedColliders.Clear();

			Collider[] colliders = sourceRoot.GetComponentsInChildren<Collider>();
			for (int i = 0; i < colliders.Length; i++)
			{
				Collider sourceCollider = colliders[i];

				if (!sourceCollider.enabled)
					continue;

				if (!copyTriggers && sourceCollider.isTrigger)
					continue;

				GameObject copyGO = new GameObject(sourceCollider.name, sourceCollider.GetType());

				copyGO.transform.SetParent(colliderRoot, true);
				copyGO.transform.localScale = sourceCollider.transform.lossyScale;

				Vector3 posOffset = sourceRoot.InverseTransformPoint(sourceCollider.transform.position);
				Quaternion rotOffset = Quaternion.Inverse(sourceRoot.rotation) * sourceCollider.transform.rotation;

				copyGO.transform.SetPositionAndRotation(
					root.TransformPoint(posOffset),
					root.rotation * rotOffset);

				copyGO.layer = overrideLayer ? gameObject.layer : sourceCollider.gameObject.layer;
				copyGO.tag = sourceCollider.gameObject.tag;

				Collider copyCollider = copyGO.GetComponent<Collider>();

				copyCollider.sharedMaterial = sourceCollider.sharedMaterial;
				copyCollider.isTrigger = sourceCollider.isTrigger;
				copyCollider.contactOffset = sourceCollider.contactOffset;

				BoxCollider sourceBox = sourceCollider as BoxCollider;
				if (sourceBox != null)
				{
					BoxCollider copyBox = copyCollider as BoxCollider;
					copyBox.size = sourceBox.size;
					copyBox.center = sourceBox.center;
				}
				else
				{
					SphereCollider sourceSphere = sourceCollider as SphereCollider;
					if (sourceSphere != null)
					{
						SphereCollider copySphere = copyCollider as SphereCollider;
						copySphere.radius = sourceSphere.radius;
						copySphere.center = sourceSphere.center;
					}
					else
					{
						CapsuleCollider sourceCapsule = sourceCollider as CapsuleCollider;
						if (sourceCapsule != null)
						{
							CapsuleCollider copyCapsule = copyCollider as CapsuleCollider;
							copyCapsule.radius = sourceCapsule.radius;
							copyCapsule.height = sourceCapsule.height;
							copyCapsule.direction = sourceCapsule.direction;
							copyCapsule.center = sourceCapsule.center;
						}
					}
				}

				copiedColliders.Add(copyCollider);
			}

			Bounds colliderBounds = PhysicsUtils.EncapsulateColliders(copiedColliders, root);
			boundsCenter = Vector3.Max(boundsCenter, colliderBounds.center);
			boundsSize = Vector3.Max(boundsSize, colliderBounds.size);
		}

		if (shrinkToFit)
		{
			float maxRelativeSize = 0f;
			for (int i = 0; i < 3; i++)
			{
				float relativeSize = boundsSize[i] / fitBoundsSize[i];
				maxRelativeSize = Mathf.Max(maxRelativeSize, relativeSize);
			}


			if(maxRelativeSize > 1f)
				root.localScale = Vector3.one / maxRelativeSize;
		}

		root.localPosition = -boundsCenter * root.localScale.x;

		transform.SetParent(prevParent, true); 
		 
		transform.localPosition = prevLocalPos;
		transform.localRotation = prevLocalRot;
		transform.localScale = prevLocalScale;
	}

	private void OnDrawGizmosSelected()
	{
		if (shrinkToFit)
		{ 
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(Vector3.zero, fitBoundsSize);
		}
	}
}
