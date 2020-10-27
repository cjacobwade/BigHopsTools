﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Luckshot.Paths;

public class PathAligner : MonoBehaviour
{
	public enum AlignDirection
	{
		YForward,
		ZForward
	}

	[System.Serializable]
	public struct PathAlignObject
	{
		public Transform transform;
		[Range(0f, 1f)]
		public float alpha;
		[Range(0f, 360f)]
		public float rotAngle;
		public float offset;
		[Range(0f, 360f)]
		public float offsetAngle;
		public AlignDirection alignDirection;
	}

	[SerializeField]
	private PathAlignObject[] pathAlignObjects = null;

	[SerializeField, AutoCache]
	private PathBase path = null;

	private void OnValidate()
	{
		if (path == null)
			return;

		for(int i =0; i < pathAlignObjects.Length; i++)
		{
			PathAlignObject pao = pathAlignObjects[i];
			if(pao.transform != null)
			{
				Vector3 position = path.GetPoint(pao.alpha);
				Vector3 direction = path.GetDirection(pao.alpha);
				Vector3 normal = path.GetNormal(pao.alpha);

				pao.transform.position = position + Quaternion.AngleAxis(pao.offsetAngle, direction) * (normal * pao.offset);

				Quaternion lookRot = Quaternion.identity;
				if (pao.alignDirection == AlignDirection.ZForward)
				{
					Quaternion directionLook = Quaternion.identity;
					if (direction != Vector3.zero)
						directionLook = Quaternion.LookRotation(direction, normal);

					lookRot = Quaternion.AngleAxis(pao.rotAngle, direction) * directionLook;
				}
				else
				{
					Quaternion normalLook = Quaternion.identity;
					if (normal != Vector3.zero)
						normalLook = Quaternion.LookRotation(normal, direction);

					lookRot = Quaternion.AngleAxis(pao.rotAngle, direction) * normalLook;
				}

				pao.transform.rotation = lookRot;
			}
		}
	}
}
