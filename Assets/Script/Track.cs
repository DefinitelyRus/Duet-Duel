using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds details about a track and the timed events that occur in it.
/// Does not contain any real-time logic or information.
/// </summary>
public class Track : MonoBehaviour {

	#region Displayed Details

	/// <summary>
	/// The name of the track.
	/// </summary>
	[Header("Displayed Details")]
	public string Name = "Untitled Track";

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
	/// The name of the artist.
	/// </summary>
	public string Artist;

	/// <summary>
	/// The creator of the beatmap.
	/// </summary>
	public string BeatMapCreator;

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

	#region Timed Events

	/// <summary>
	///		Helps segment the song into chunks,
	///		allowing for more efficient searching for notes.
	/// </summary>
	public List<TimedEvent> Events;

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
		List<TimedEvent> events = new();

		if (start <= 1) start = 1;
		if (end <= 0) end = int.MaxValue;

		//Binary search: Find the first event that is within the range.
		int lowIndex = 0, highIndex = Events.Count - 1, pointer = 0;
		int pointerBar = 0;
		while (lowIndex <= highIndex) {
			if (debug) Debug.Log($"[Track] Searching for bar {start}: " +
				$"Low: {lowIndex} -> High: {highIndex} | At {pointer} (Bar {pointerBar})");

			pointer = (lowIndex + highIndex) / 2;
			pointerBar = Events[pointer].Bar;

			//Closes in on the closest point to the start point.
			if (pointerBar < start) lowIndex = pointer + 1;
			else if (pointerBar > start) highIndex = pointer - 1;
			else {
				if (debug) Debug.Log($"[Track] Ending search at bar {pointerBar}.");

				//If pointing at the start with matching bar count, break.
				if (pointerBar == start) {
					if (debug) Debug.Log($"[Track] Starting list at bar {pointerBar}.");
					break;
				}

				//If pointing beyond start... 
				else {
					if (debug) Debug.Log($"[Track] Search ended with no matches...");

					//Point to the next event.
					pointer++;

					//If reached the end of the list, return empty.
					if (pointer >= Events.Count) {
						if (debug) Debug.Log($"[Track] No events found in range.");
						return new();
					}

					pointerBar = Events[pointer].Bar;

					//If pointing at or before `end`...
					if (pointerBar <= end) {
						start = pointerBar;
						if (debug) Debug.Log($"[Track] Substituted start at bar {pointerBar}.");
						break;
					}

					//If pointing past `end`, return empty.
					else {
						if (debug) Debug.Log($"[Track] No events found in range.");
						return new();
					}
				}
			}
		}

		TimedEvent e;
		if (debug) Debug.Log($"[Track] Checking event starting at bar {start}.");

		//Go through all items from the starting point...
		for (int i = pointer; i < Events.Count; i++) {
			e = Events[i];

			//If the event is within the range, add it to the list.
			if (e.Bar >= start && e.Bar <= end) {
				events.Add(SetStartTime(e));
				if (debug) Debug.Log($"[Track] Added event at bar {e.Bar}.");
			}

			//If the event is past the end, break.
			else if (e.Bar > end) {
				if (debug) Debug.Log($"[Track] No more events in range.");
				break;
			}
		}

		return events;
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
		double beatsPerMinute = BPM / 60d;
		double beatsPerSecond = beatsPerMinute / Numerator;
		double secondsPerStep = 1d / (beatsPerSecond * Denominator);

		int barCount = (e.Bar - 1) * Numerator * Denominator;
		int beatCount = (e.StartBeat - 1) * Denominator;
		int totalSteps = barCount + beatCount + (e.StartStep - 1);

		e.StartTime = totalSteps * secondsPerStep + StartOffset;
		return e;
	}

	#endregion

	/// <summary>
	/// The audio clip that will be played.
	/// </summary>
	[Header("Audio Clip")]
	public AudioClip AudioClip;

	#region Unity

	/// <summary>
	/// Loads the track and its associated audio clip into memory.
	/// <br/><br/>
	/// This game object must start before `MusicDirector`.
	/// Otherwise, the audio clip will not be loaded.
	/// </summary>
	public void Start() {
		//Debug.Log($"[Track] Loading track...");
		//GrabAudioClip();
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
