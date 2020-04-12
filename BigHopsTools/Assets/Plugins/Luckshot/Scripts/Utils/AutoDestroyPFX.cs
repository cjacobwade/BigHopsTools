using UnityEngine;
using System.Collections;

public class AutoDestroyPFX : MonoBehaviour
{
	[SerializeField, AutoCache(searchChildren = true)]
	ParticleSystem _ps = null;
	WaitForSeconds _aliveCheckWait = new WaitForSeconds(1f);

	void OnEnable()
	{
		StartCoroutine(WaitForVFXEnd());
	}

	IEnumerator WaitForVFXEnd()
	{
		while (_ps.IsAlive(true))
			yield return _aliveCheckWait;

		Destroy(gameObject);
	}
}