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

			Gizmos.DrawSphere(path.GetPoint(alpha), 0.1f);
		}
	}
}