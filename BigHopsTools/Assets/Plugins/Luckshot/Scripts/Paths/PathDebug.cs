using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Luckshot.Paths
{

	public class PathDebug : MonoBehaviour
	{
		[SerializeField, AutoCache]
		private PathBase path = null;

		[SerializeField]
		private float alpha = 0f;

		private void OnDrawGizmosSelected()
		{
			if (path == null)
				return;

			if (path.Loop)
				alpha = Mathf.Repeat(alpha, 1f);
			else
				alpha = Mathf.Clamp01(alpha);

			Vector3 pos = path.GetPoint(alpha);
			Vector3 normal = path.GetNormal(alpha);

			Gizmos.DrawSphere(pos, 0.1f);

			Debug.DrawLine(pos, pos + normal, Color.red);
		}
	}
}