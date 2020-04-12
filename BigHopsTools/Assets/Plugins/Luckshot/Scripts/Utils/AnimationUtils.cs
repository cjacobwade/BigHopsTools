using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnimationUtils
{
	public static void PlayForwards(this Animation animation, float normalizedTime = 0f, float speed = 1f)
	{
		animation[animation.clip.name].normalizedTime = normalizedTime;
		animation[animation.clip.name].speed = speed;
		animation.Play();
	}

	public static void PlayBackwards(this Animation animation)
	{
		animation[animation.clip.name].normalizedTime = 1f;
		animation[animation.clip.name].speed = -1f;
		animation.Play();
	}

	public static void SetNormalizedTime(this Animation animation, float normalizedTime)
	{
		animation[animation.clip.name].normalizedTime = normalizedTime;
		animation.Sample();
	}
}
