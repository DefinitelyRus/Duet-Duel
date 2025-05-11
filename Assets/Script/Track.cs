using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Holds details about a track and the timed events that occur in it.
/// Does not contain any real-time logic or information.
/// </summary>
public class Track : MonoBehaviour {

	#region Game Objects and Components

	[Header("Game Objects and Components")]
	public MusicDirector Director;

	#endregion

	#region Displayed Details

	/// <summary>
	/// The name of the track.
	/// </summary>
	[Header("Displayed Details")]
	public string Name = "Untitled Track";

	/// <summary>
	/// The name of the artist.
	/// </summary>
	public string Artist;

	/// <summary>
	/// The creator of the beatmap.
	/// </summary>
	public string BeatMapCreator;

	/// <summary>
	/// The file name of the OGG audio clip or the path to it.
	/// <br/><br/>
	/// This must not include the file extension. <br/>
	/// "Resources/Music" is prepended, so no need to write it here. 
	/// <br/><br/>
	/// Examples: <br/>
	/// - "Phaera - Against All Gravity" <br/>
	/// - "Extras/32ki - Mesmerizer"
	/// </summary>
	public string FilePath;

	/// <summary>
	/// The file name of the JSON file or the path to it.
	/// <br/><br/>
	/// This must include the file extension if the file has one. <br/>
	/// This accepts absolute and relative paths. <br/>
	/// Absolute paths must start with "C:/" <br/>
	/// Relative paths start from "Documents/Duet Duel/Beatmaps". <br/>
	/// <br/><br/>
	/// Examples: <br/>
	/// - "C:/Users/Rus/Desktop/Confessions of a Rotten Girl.json" <br/>
	/// - "DDBeatmap by DefinitelyRus, Phaera - Against All Gravity.json" <br/>
	/// </summary>
	public string BeatmapPath;

	#endregion

	#region Gameplay Details

	/// <summary>
	/// How long the song is in seconds.
	/// <br/><br/>
	/// This value is usually calculated from the length of the audio clip.
	/// There is no need to set this manually, but the option is there if needed.
	/// </summary>
	[Header("Gameplay Details")]
	[Range(10f, 1200f)]
	public float Length = 60f;

	/// <summary>
	/// How fast the song is in beats per minute.
	/// </summary>
	[Range(50f, 300f)]
	public float BPM = 120f;

	/// <summary>
	/// How many beats are in a bar.
	/// </summary>
	[Range(1, 20)]
	public int Numerator = 4;

	/// <summary>
	/// How many subdivisions are in a beat.
	/// </summary>
	[Range(1, 20)]
	public int Denominator = 4;

	/// <summary>
	/// How many seconds to wait before the song starts playing.
	/// <br/><br/>
	/// Negative -> Song will start before the beat counter starts. <br/>
	/// Zero -> Song will start at the same frame as the beat counter. <br/>
	/// Positive -> Song will start after the beat counter starts.
	/// </summary>
	[Range(-60f, 60f)]
	public float StartOffset = 0f;

	#endregion

	#region Loading Events

	/// <summary>
	///		Helps segment the song into chunks,
	///		allowing for more efficient searching for notes.
	/// </summary>
	[Header("Loading Events")]
	public List<StrippedEvents> Events;

	private void LoadAll(bool debug = false) {

		#region Path initialization & validation

		string path;

		//Uses absolute path
		if (BeatmapPath.Length > 2 && BeatmapPath[1] == ':') path = BeatmapPath;

		//Uses relative path
		else {
			string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string beatmap = $"{docs}/{Application.productName}/Beatmaps";
			path = $"{beatmap}/{BeatmapPath}";
		}

		if (debug) Debug.Log($"[Recorder] Loading beatmap from \"{path}\".");

		//Check if the path is valid
		if (!File.Exists(path)) {
			Debug.LogError($"[Recorder] Beatmap path does not exist.");
			return;
		}

		#endregion

		#region Loading stripped events

		if (debug) Debug.Log($"[Recorder] Loading beatmap from {path}.");

		Events = StrippedEvents.FromJsonPath(path);

		if (debug) Debug.Log($"[Recorder] Loaded {Events.Count} events.");

		#endregion

	}

	/// <summary>
	///		Gets all events that are scheduled to occur within the given range.
	///		<br/><br/>
	///		`start` and `end` are inclusive.
	/// </summary>
	/// <param name="start">
	///		The first bar to include in the list. <br/>
	///		Setting this to 0 will include all events before `end`. (start = 1)
	///	</param>
	/// <param name="end">
	///		The last bar to include in the list. <br/>
	///		Setting this to 0 will include all events after `start`. (end = int.MaxValue)
	///	</param>
	/// <param name="debug">
	///		Whether to print debug information.
	/// </param>
	/// <returns>
	///		A list of events that occur within the given range.
	/// </returns>
	public List<TimedEvent> GetEventsInRange(int start, int end, bool debug = false) {
		List<TimedEvent> results = new();

		if (start <= 1) start = 1;
		if (end <= 0 || end < start) end = int.MaxValue;

		if (debug) Debug.Log($"[Track] Getting events from bars {start} to {end}.");

		foreach (var strippedEvent in Events) {
			if (strippedEvent.StartBar < start || strippedEvent.StartBar > end)
				continue;

			// instantiate your GameObject + component exactly as before…
			GameObject spawnedEvent = new($"Event {strippedEvent.StartBar}:{strippedEvent.StartBeat}:{strippedEvent.StartStep} - {strippedEvent.EventType}");
			TimedEvent timedEvent;

			if (strippedEvent.EventType == TimedEvent.EventType.Attack) {
				timedEvent = spawnedEvent.AddComponent<AttackNote>();
				((AttackNote) timedEvent).Attack = strippedEvent.AttackType;
				((AttackNote) timedEvent).PlayerID = strippedEvent.OwnerID;
			}

			else {
				timedEvent = spawnedEvent.AddComponent<TimedEvent>();
			}

			timedEvent.Type = strippedEvent.EventType;
			timedEvent.StartBar = strippedEvent.StartBar;
			timedEvent.StartBeat = strippedEvent.StartBeat;
			timedEvent.StartStep = strippedEvent.StartStep;

			spawnedEvent.transform.SetParent(gameObject.transform);

			results.Add(SetStartTime(timedEvent));
		}

		if (debug) Debug.Log($"[Track] Found {results.Count} events from bar {start} to {end}.");

		return results;
	}

	/// <summary>
	///		Calculates the event's start time,
	///		then returns it with a modified start time.
	/// </summary>
	/// <param name="e">
	///		The event to calculate the start time for.
	/// </param>
	/// <returns>
	///		The same event, now with an auto-calculated start time.
	/// </returns>
	public TimedEvent SetStartTime(TimedEvent e) {

		//Calculate the start time of the event.
		double secondsPerStep = 60.0 / (BPM * Denominator);

		double tickerOffset = secondsPerStep * 2;
		//NOTE: The initial "Director.CurrentStep", as set in the Inspector.
		/*
		 * Since this calculation does not take the MusicDirector's
		 * timings into account, it never processed that it had a 2-step delay--
		 * a delay that should not have been there to begin with.
		 * 
		 * I want to fix it but it's working and we're critically
		 * short on time, so we're leaving this bug here.
		 */

		int barCount = (e.StartBar - 1) * Numerator * Denominator;
		int beatCount = (e.StartBeat - 1) * Denominator;
		int totalSteps = barCount + beatCount + (e.StartStep - 1);

		e.StartTime = totalSteps * secondsPerStep + StartOffset + tickerOffset;
		return e;
	}

	#endregion

	#region Miscellaneous

	/// <summary>
	/// The audio clip that will be played.
	/// </summary>
	[Header("Miscellaneous")]
	public AudioClip AudioClip;

	#endregion

	#region Unity

	/// <summary>
	/// Loads the track and its associated audio clip into memory.
	/// <br/><br/>
	/// This game object must start before `MusicDirector`.
	/// Otherwise, the audio clip will not be loaded.
	/// </summary>
	public void Awake() {
		LoadAll(true);
	}

	#endregion

	#region Unused

	/// <summary>
	/// 	Loads the audio clip from the given path.
	/// 	<br/><br/>
	/// 	Uses `FilePath` if no path is given.
	/// </summary>
	/// <param name="path">
	///		Allows using another audio clip than what is in `FilePath`.
	/// </param>
	/// <param name="overrideExisting">
	///		Allows using `path` as the new `FilePath`.
	/// </param>
	[Obsolete("Please manually assign the audio clip in the inspector.", true)]
	public AudioClip GrabAudioClip(string path = null, bool overrideExisting = false, bool debug = false) {

		if (debug) Debug.Log($"[Track] Loading audio clip from \"{path}\"...");

		//Use existing path if no path is given.
		if (string.IsNullOrEmpty(path)) {
			if (debug) Debug.Log($"[Track] Using existing path: {FilePath}.");
			path = FilePath;
		}

		//If path isn't empty and the existing path is to be overridden...
		else if (overrideExisting) {
			if (debug) Debug.Log($"[Track] Overriding existing path:\n\"{FilePath}\"\n to\n\"{path}\"");
			FilePath = path;
		}

		AudioClip = Resources.Load<AudioClip>(path);

		if (AudioClip == null) {
			Debug.LogError($"[Track] Failed to load audio clip.");
			return null;
		} else {
			Debug.Log($"[Track] Loaded audio clip: {AudioClip.name}.");
			return AudioClip;
		}
	}

	[Obsolete("Please create an instance of a Track prefab instead.", true)]
	public Track(string name, string path, float tempo, int beatsPerBar, int stepsPerBeat, string artist = null, string beatmapper = null, float startOffset = 0) {
		Name = name;
		FilePath = path;
		BPM = tempo;
		Numerator = beatsPerBar;
		Denominator = stepsPerBeat;
		Artist = artist;
		BeatMapCreator = beatmapper;
		StartOffset = startOffset;

		//Estimate the length of the song in seconds.
		float beatsPerMinute = BPM / 60f;
		float beatsPerSecond = beatsPerMinute / Numerator;
		float secondsPerStep = 1f / (beatsPerSecond * Denominator);
		float totalSteps = Length / secondsPerStep;
		Length = (totalSteps * secondsPerStep) + startOffset;

		GrabAudioClip(path, true);
	}

	#endregion
}
