﻿using UnityEngine;
using UnityEngine.SceneManagement;

public class Start : MonoBehaviour
{


    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.Space)) {
			SceneManager.LoadScene(1);
		}
	}
}
