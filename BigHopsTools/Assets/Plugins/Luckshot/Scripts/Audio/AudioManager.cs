using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
	[SerializeField]
	private int sourcePoolSize = 50;
	private int poolSize = 0;

	private List<AudioSource> sourcePool = new List<AudioSource>();

	private void RegisterSource(AudioSource source)
	{ sourcePool.Add(source); }

	private void DeregisterSource(AudioSource source)
	{ sourcePool.Remove(source); }

	protected override void Awake()
	{
		base.Awake();
		for(int i = 0; i < sourcePoolSize; i++)
			AddSource();
	}

	void AddSource()
	{
		AudioSource source = new GameObject("AudioSource-" + poolSize, typeof(AudioSource)).GetComponent<AudioSource>();
		source.spatialBlend = 1f;
		source.dopplerLevel = 0f;
		source.transform.SetParent(transform);
		source.gameObject.SetActive(false);

		sourcePool.Add(source);
		poolSize++;
	}

	public AudioSource PlaySound(AudioClip clip, Transform parent = null, float volume = 1f, float pitch = 1f)
	{
		if (sourcePool.Count == 0)
		{
			Debug.LogWarning("No pooled sources available. Add new one.");
			AddSource();
		}

		AudioSource source = sourcePool[0];
		source.volume = volume;
		source.pitch = pitch;

		DeregisterSource(source);

		source.gameObject.SetActive(true);
		
		if(parent)
		{
			source.transform.SetParent(parent);
			source.transform.ResetLocals();
		}
		else
		{
			source.transform.SetParent(transform);
			source.transform.ResetLocals();
		}

		source.clip = clip;
		source.Play();

		InvokeManager.Invoke(ReturnToPool, source, clip.length);

		return source;
	}

	public AudioSource PlaySound(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
	{
		if (sourcePool.Count == 0)
		{
			Debug.LogWarning("No pooled sources available. Add new one.");
			AddSource();
		}

		AudioSource source = sourcePool[0];
		source.volume = volume;
		source.pitch = pitch;

		DeregisterSource(source);

		source.gameObject.SetActive(true);
		source.transform.position = position;

		source.clip = clip;
		source.Play();

		InvokeManager.Invoke(ReturnToPool, source, clip.length);

		return source;
	}

	void ReturnToPool(AudioSource source)
	{
		if(source != null)
		{
			source.gameObject.SetActive(false);
			source.transform.SetParent(transform);

			RegisterSource(source);
		}
	}
}
