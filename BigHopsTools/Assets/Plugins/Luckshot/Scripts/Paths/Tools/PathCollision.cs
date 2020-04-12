using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PathCollision : MonoBehaviour
{
	[SerializeField, AutoCache]
	private PathBase path = null;
	public PathBase Path
	{ get { return path; } }

	[SerializeField, OnValueChanged("BuildCollision")]
	private bool useCapsules = true;

	[SerializeField]
	private SingleLayer colliderLayer = null;

	[SerializeField, ShowIf("useCapsules"), OnValueChanged("BuildCollision")]
	private float capsulesPerUnit = 0.8f;

	[SerializeField, Range(0f, 1f), ShowIf("useCapsules"), OnValueChanged("BuildCollision")]
	private float fillAmount = 1f;
	public float FillAmount
	{ get { return fillAmount; } }

	[SerializeField]
	private AnimationCurve thicknessCurve = new AnimationCurve(new []{ new Keyframe(0f, 1f), new Keyframe(1f, 1f) }); 
	public float GetThickness(float alpha)
	{ return Path.Radius * thicknessCurve.Evaluate(alpha); }

	public void SetFillAmount(float inFillAmount)
	{
		if (fillAmount != inFillAmount)
		{
			fillAmount = inFillAmount;

			RefreshActiveColliders();

			OnFillAmountChanged(this, fillAmount);
		}
	}

	[SerializeField, HideInInspector]
	private List<CapsuleCollider> colliders = new List<CapsuleCollider>();
	public List<CapsuleCollider> Colliders
	{ get { return colliders; } }

	public System.Action<PathCollision> OnCollisionChanged = delegate { };
	public System.Action<PathCollision, float> OnFillAmountChanged = delegate { };

	private void Awake()
	{
		if(path == null)
		{
			Debug.LogError("No PathBase found. Trying to find one now.", this);
			path = GetComponent<PathBase>();
		}

		path.OnPathChanged -= Path_OnPathChanged;
	}

	private void Path_OnPathChanged(PathBase inPath)
	{
		if (path == inPath)
			BuildCollision();
		else
			inPath.OnPathChanged -= Path_OnPathChanged;
	}

	[Button("Build Collision")]
	public void BuildCollision()
	{
		if (this == null)
			path.OnPathChanged -= Path_OnPathChanged;

		if (path != null)
		{
			Transform capsuleRoot = transform.Find("CapsuleRoot");
			if (capsuleRoot != null)
			{
				if (Application.IsPlaying(this))
					Destroy(capsuleRoot.gameObject);
				else
					DestroyImmediate(capsuleRoot.gameObject);
			}

			if (useCapsules)
			{
				MeshCollider mc = GetComponent<MeshCollider>();
				if (mc != null)
				{
					if (Application.IsPlaying(this))
						Destroy(mc);
					else
						DestroyImmediate(mc);
				}

				capsuleRoot = new GameObject("CapsuleRoot").transform;
				capsuleRoot.SetParent(transform);
				capsuleRoot.ResetLocals();

				colliders.Clear();

				int numCapsules = Mathf.CeilToInt(path.GetLength() * capsulesPerUnit);
				for (int i = 1; i <= numCapsules; i++)
				{
					float prevAlpha = (i - 1) / (float)numCapsules;
					float alpha = i / (float)numCapsules;

					Vector3 prevPos = path.GetPoint(prevAlpha);
					Vector3 pos = path.GetPoint(alpha);

					GameObject capsuleGo = new GameObject("CapsuleCollider", typeof(CapsuleCollider));

					CapsuleCollider capsule = capsuleGo.GetComponent<CapsuleCollider>();
					capsule.transform.SetParent(capsuleRoot);
					capsule.transform.position = (prevPos + pos) / 2f;

					Vector3 lookDir = pos - prevPos;
					if (lookDir != Vector3.zero)
						capsule.transform.forward = lookDir.normalized;

					capsule.direction = 2; // Z

					float prevThickness = thicknessCurve.Evaluate(prevAlpha);
					float thickness = thicknessCurve.Evaluate(alpha);

					capsule.radius = path.Radius * (prevThickness + thickness)/2f;
					capsule.height = (pos - prevPos).magnitude + capsule.radius;
					capsule.isTrigger = false;
					capsule.gameObject.layer = colliderLayer.layer;

					colliders.Add(capsule);
				}

				RefreshActiveColliders();
			}
			else
			{
				MeshFilter mf = GetComponent<MeshFilter>();
				if (mf == null)
					mf = path.GetComponent<MeshFilter>();

				if (mf != null)
				{
					MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
					if(mc == null)
						mc = mf.gameObject.AddComponent<MeshCollider>();

					mc.sharedMesh = mf.sharedMesh;
				}
			}

#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
			OnCollisionChanged(this);
		}
	}

	private void RefreshActiveColliders()
	{
		for (int i = 1; i < colliders.Count; i++)
		{
			float prevAlpha = (i + 1) / (float)colliders.Count;
			colliders[i].gameObject.SetActive(prevAlpha < fillAmount);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if(path != null)
		{
			path.OnPathChanged -= Path_OnPathChanged;
			path.OnPathChanged += Path_OnPathChanged;
		}
	}
}
