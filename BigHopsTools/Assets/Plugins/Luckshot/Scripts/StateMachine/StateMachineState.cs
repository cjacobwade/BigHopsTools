using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Luckshot.FSM
{
	[DefaultExecutionOrder(300)]
	public class StateMachineState : MonoBehaviour
	{
		protected StateMachine stateMachine = null;
		protected StateMachine StateMachine
		{ get { return stateMachine; } }

		public K GetState<K>() where K : StateMachineState
		{ return stateMachine.GetState<K>(); }

		public void ChangeState<K>(StateParams stateParams = null) where K : StateMachineState
		{ stateMachine.ChangeState<K>(stateParams); }

		public void SetStateMachine(StateMachine inStateMachine)
		{ stateMachine = inStateMachine; }

		private float enterStateTime = 0f;
		public float EnterStateTime
		{ get { return enterStateTime; } }

		protected float lastTimeInState = 0f;
		public float LastTimeInState
		{ get { return lastTimeInState; } }

		protected bool hasAwoken = false;
		public bool HasAwoken
		{ get { return hasAwoken; } }

		public void Awake()
		{
			if (hasAwoken)
				return;

			AwakeIfNeeded();
			hasAwoken = true;
		}

		public virtual void AwakeIfNeeded() { }
		public virtual void Enter(StateParams stateParams)
		{
			enterStateTime = Time.time;
			lastTimeInState = Time.time;
		}

		public virtual void Exit()
		{
			lastTimeInState = Time.time;
		}

		public virtual void Tick()
		{
			lastTimeInState = Time.time;
		}
		public virtual void FixedTick() { }

		public virtual void CollisionEnter(Collision collision) { }
		public virtual void CollisionExit(Collision collision) { }
		public virtual void TriggerEnter(Collider collider) { }
		public virtual void TriggerExit(Collider collider) { }
	}

	public abstract class StateMachineState<T> : StateMachineState where T : MonoBehaviour
	{
		public T Owner
		{ get { return stateMachine.Owner as T; } }
	}

	public class StateParams
	{
	}
}