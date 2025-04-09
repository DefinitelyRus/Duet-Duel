using UnityEngine;

public class MusicDirector : MonoBehaviour {

	public Player Player1;

	public Player Player2;

	public Track Track;

	private float Timer = 0f;

	void Start()
    {
        
    }

    void Update()
    {
        Timer += Time.deltaTime;
	}
}
