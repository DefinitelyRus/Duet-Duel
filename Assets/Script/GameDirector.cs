using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
	public Player Player;
	public MusicDirector MusicDirector;
	public GameObject[] EnvironmentObjects;

	public bool AllowStart;
	public float BootupDelay = 5f;

	private void Awake() {
		StartCoroutine(DelayBootup(BootupDelay));
	}

	IEnumerator DelayBootup(float delay) {
		Debug.Log($"[GameDirector] Delaying bootup by {BootupDelay} seconds.");

		AllowStart = false;
		Time.timeScale = 0f;
		MusicDirector.MusicPlayer.Pause();

		yield return new WaitForSecondsRealtime(delay);

		AllowStart = true;
		Time.timeScale = 1f;
		MusicDirector.MusicPlayer.UnPause();
	}
}
