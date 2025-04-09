using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds details about a track and the timed events that occur in it.
/// Does not contain any real-time logic or information.
/// </summary>
public class Track : MonoBehaviour
{
	/// <summary>
	/// The name of the track.
	/// </summary>
	public string Name;

	/// <summary>
	/// Path to the OGG audio file.
	/// </summary>
	public string PathToTrack;

	/// <summary>
	/// The name of the artist.
	/// </summary>
	public string Artist;

	/// <summary>
	/// The creator of the beatmap.
	/// </summary>
	public string BeatMapCreator;

	/// <summary>
	/// How long the song is in seconds.
	/// </summary>
	public float Length;

	/// <summary>
	/// How fast the song is in beats per minute.
	/// </summary>
	public float BPM;

	/// <summary>
	/// How many beats are in a bar.
	/// </summary>
	public int Numerator;

	/// <summary>
	/// How many subdivisions are in a beat.
	/// </summary>
	public int Denominator;

	/// <summary>
	/// How many seconds to wait before the first bar in the beatmap starts.
	/// </summary>
	public float StartOffset;

	/// <summary>
	///		Helps segment the song into chunks,
	///		allowing for more efficient searching for notes.
	/// </summary>
	public List<TimedEvent> Events;

	public Track(string name, string path, float tempo, int beatsPerBar, int stepsPerBeat, string artist = null, string beatmapper = null, float startOffset = 0) {
		Name = name;
		PathToTrack = path;
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
		List<TimedEvent> events = new();

		if (start <= 1) start = 1;
		if (end <= 0) end = int.MaxValue;

		//Binary search
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

		for (int i = pointer; i < Events.Count; i++) {
			e = Events[i];

			//If the event is within the range, add it to the list.
			if (e.Bar >= start && e.Bar <= end) {
				events.Add(e);
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
}
