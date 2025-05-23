using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameDirector : MonoBehaviour
{
	public Player Player1;
	public Player Player2;
	public MusicDirector MusicDirector;
	public GameObject[] EnvironmentObjects;

	public TextMeshProUGUI P1Score;
	public TextMeshProUGUI P2Score;

	public bool AllowStart;
	public float BootupDelay = 5f;

	public List<Projectile> Projectiles = new();
	public float ScoreRatio = 0.5f;

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

	public void AddProjectile(Projectile projectile) {
		if (projectile != null) {
			Projectiles.Add(projectile);
			Debug.Log($"[GameDirector] Added projectile: {projectile.name}");
		}
		else {
			Debug.LogWarning("[GameDirector] Attempted to add a null projectile.");
		}
	}

	public void UpdateScore() {
		float p1Score = Player1.Score;
		float p2Score = Player2.Score;

		float scoreRatio;
		if (p1Score == 0 && p2Score == 0) scoreRatio = 0.5f;
		else if (p1Score > 0 && p2Score == 0) scoreRatio = 1f;
		else scoreRatio = Player1.Score / Player2.Score;

		ScoreRatio = Mathf.Clamp(scoreRatio, 0, 1);

		P1Score.text = $"P1\n{p1Score:F0}";
		P2Score.text = $"P2\n{p2Score:F0}";
	}
}
