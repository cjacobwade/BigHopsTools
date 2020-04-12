using UnityEngine;
using System;

namespace Luckshot.Paths
{
	public abstract class AdvSpline<T> : SplinePath where T : SplineNodeData, new()
	{
		// Array of node data
		// Access node data of specific main control point
		// Get interp'd node data at whatever alpha

		[SerializeField]
		T[] nodeData = null;
		protected T[] NodeData
		{
			get
			{
				if (nodeData == null || nodeData.Length == 0)
					Reset();

				return nodeData;
			}
		}

		public T GetNodeData(int i)
		{
			return NodeData[i];
		}

		public void InterpNodeData(T inNodeData, float a)
		{
			a = Mathf.Clamp01(a);

			a *= (NodeData.Length - 1);
			int prev = Mathf.FloorToInt(a);
			int next = Mathf.CeilToInt(a);

			if (prev < NodeData.Length - 1)
				a -= (float)prev;

			inNodeData.Lerp(NodeData[prev], NodeData[next], Mathf.Clamp01(a));
		}

		public void SetNodeData(int i, T inNodeData)
		{
			NodeData[i] = inNodeData;
		}

		public override void AddCurve()
		{
			base.AddCurve();

			Array.Resize(ref nodeData, NodeData.Length + 1);
			NodeData[NodeData.Length - 1] = new T();

			if (loop)
				NodeData[NodeData.Length - 1] = NodeData[0];
		}

		public override void RemoveCurve()
		{
			base.RemoveCurve();

			Array.Resize(ref nodeData, NodeData.Length - 1);

			if (loop)
				NodeData[NodeData.Length - 1] = NodeData[0];
		}

		public override void Reset()
		{
			base.Reset();

			nodeData = new T[]
			{
			new T(),
			new T()
			};
		}
	}
}