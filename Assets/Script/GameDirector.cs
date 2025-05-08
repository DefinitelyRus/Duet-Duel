using System.Collections.Generic;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
	public Player Player;
	public MusicDirector MusicDirector;
	public GameObject[] EnvironmentObjects;

	private void Awake() {
		return;
		Time.timeScale = 0f;
		MusicDirector.MusicPlayer.Pause();

		EnvironmentObjects = GameObject.FindGameObjectsWithTag("Environment");
	}
}
