using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace Luckshot.Paths
{
	public enum NormalType
	{
		LocalUp,
		Perpendicular
	}

	public abstract class PathBase : MonoBehaviour
	{
		public abstract Vector3 GetPoint(float alpha);
		public abstract float GetNearestAlpha(Vector3 position, int iterations = 10);
		public abstract Vector3 GetDirection(float alpha);
		
		public abstract float GetLength();

		public abstract Vector3 GetVelocity(float alpha);
		public abstract Vector3 GetNormal(float t);

		[SerializeField, OnValueChanged("ChangePath")]
		protected bool loop = false;
		public virtual bool Loop
		{ get { return loop; } }

		public void SetLoop(bool inLoop)
		{ loop = inLoop; }

		[SerializeField, OnValueChanged("ChangePath")]
		private float radius = 0.1f;
		public float Radius
		{ get { return radius * transform.lossyScale.x; } }

		public void SetRadius(float inRadius)
		{ radius = inRadius; }

		[SerializeField]
		private NormalType normalType = NormalType.LocalUp;
		public NormalType NormalType
		{ get { return normalType; } }

		private void ChangePath()
		{ OnPathChanged(this); }

		public System.Action<PathBase> OnPathChanged = delegate {};
	}
}
