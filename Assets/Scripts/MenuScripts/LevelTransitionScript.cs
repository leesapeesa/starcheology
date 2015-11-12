﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelTransitionScript : MonoBehaviour {
    // Use this for initialization
    //public Texture emptyProgressBar;
    //public Texture fullProgressBar;
    public Font font;
    public float maxGravity = 2f;
    private RectTransform progressBar;
    private Text progressText;
    private const float PROGRESSBAR_WIDTH = 200f;

    private AsyncOperation async = null;

    void Start() {
        progressBar = GameObject.Find("CurrentProgress").GetComponent<RectTransform>();
        progressText = GameObject.Find ("LoadingProgress").GetComponent<Text> ();
        StartCoroutine (DisplayLoadingScreen ());
    }

    IEnumerator DisplayLoadingScreen() {
        // Drawing the Loading progress:
        async = Application.LoadLevelAsync (1);
        async.allowSceneActivation = false;

        // While there is still part of the level to load, update the progress bar.
        // Since we don't want the scene to immediately activate, we want the user to
        // choose when to continue, the maximum async.progress gives is 0.9.
        while (async.progress < 0.9f) {
            // artificial loading.
            yield return null;
            print(async.progress);
            // The maximum it is going to let us have as progress is 0.9.
            float fracHealth = (float)(async.progress / 0.9);
            float newWidth = fracHealth * PROGRESSBAR_WIDTH;
            progressBar.sizeDelta = new Vector2(newWidth, progressBar.sizeDelta.y);
            progressBar.anchoredPosition = new Vector2(newWidth / 2, 0);
            progressText.text = "Progress: " + Mathf.Round ((float) (async.progress / 0.9 * 100)).ToString() + " %";
        }

        // Once the next level is loaded, let the user press the start button.
        GameObject.Find("Start Level").GetComponent<Button>().interactable = true;
    }

    // Method for button listener to call in Unit.
    public void OnButtonClick() {
        async.allowSceneActivation = true;
    }

    void OnDestroy() {
        print ("switching levels");
        PersistentTerrainSettings.settings.gravityEffect = Random.Range (0.5f, maxGravity);
        print (PersistentTerrainSettings.settings.gravityEffect);
        if (PersistentPlayerSettings.settings == null) {
            // It will be null if we're loading the level from the New Game screen for the
            // first time.
            print ("PersistentPlayerSettings doesn't exist");
            return;
        }
        PersistentPlayerSettings.settings.levelScore = 0;
    }
}
