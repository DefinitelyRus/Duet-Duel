using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MusicDirector : MonoBehaviour {

	public Player Player1;

	public Player Player2;

	public Track Track;

	private List<TimedEvent> Events;

	private List<AttackNote> LongAttacks;

	public AudioSource MusicPlayer;

	private double Timer = 0d;

	/// <summary>
	/// How many bars to load at once.
	/// <br/><br/>
	/// 0 = Load all events at once. <br/>
	/// For really short songs or if you have a lot of memory. <br/>
	/// Good for avoiding lag spikes when loading a large number of events.
	/// <br/><br/>
	/// 1-64 = Load X number of bars at once once all loaded events have been played. <br/>
	/// 1-4 = Good for high-density beat maps. <br/>
	/// 5-16 = Good for long songs. <br/>
	/// 17-64 = Only use if the song is too long and/or dense for 0. <br/>
	/// 65+ = You should just use 0.
	/// </summary>
	[Range(0, 64)]
	public int LoadedBarCount = 16;

	/// <summary>
	/// How many bars have been played so far since the start of the song.
	/// </summary>
	public int CurrentBar = 0;

	/// <summary>
	/// What step the player is currently on. Resets to 1 after every beat.
	/// </summary>
	public int CurrentStep = 0;

	/// <summary>
	/// What beat the player is currently on. Resets to 1 after every bar.
	/// </summary>
	public int CurrentBeat = 0;

	TimedEvent LastPlayedEvent;

	bool delayedStart = false;

	private double GetStepDuration() {
		return 60d / Track.BPM / Track.Denominator;
	}

	void Start() {
		MusicPlayer.clip = Track.AudioClip;

		Events = Track.GetEventsInRange(0, LoadedBarCount, true);

		//Beat counter before audio
		if (Track.StartOffset > 0) {
			Debug.Log($"[MusicDirector] Song starts after the beat counter. Delaying song start.");

			/*
			 * Having a negative start offset means the song will start
			 * playing after the beat counter has started counting.
			 * This is prone to audio desync if the frame it
			 * starts on lasts significantly longer than usual.
			 * 
			 * On the first frame that the Timer exceeds
			 * the start offset, the audio will start playing.
			 */

			delayedStart = true;
		}
		
		//Audio before beat counter
		else if (Track.StartOffset < 0) {
			Debug.Log($"[MusicDirector] Song starts before the beat counter. Delaying beat counter start.");
			/*
			 * Having a positive start offset means the song will start
			 * playing before the beat counter has started counting.
			 * This is prone to audio desync if the frame it
			 * starts on lasts significantly longer than usual.
			 * 
			 * On the first frame that the Timer exceeds 0,
			 * the beat counter will start counting.
			 */

			Timer = -Track.StartOffset;
			delayedStart = true;
			MusicPlayer.Play();
		}
	}

    void Update()
    {
        Timer += Time.deltaTime;

		#region Audio Offset

		//This will no longer run once both the beatmap and audio have started.
		if (delayedStart) {
			double offset = Track.StartOffset;

			//+offset: Audio starts after the beat counter.
			//Song starts at `Timer >= offset`.
			//Timer starts at 0.
			//Beat counter starts at 0.
			if (offset > 0 && Timer >= offset) {
				Debug.Log("[MusicDirector] Music started after timed delay.");
				delayedStart = false;
				MusicPlayer.Play();
			}

			//-offset: Audio starts before the beat counter.
			//Song starts at `offset`.
			//Timer starts at `offset`.
			//Beat counter starts at 0.
			if (offset < 0 && Timer >= 0) {
				Debug.Log("[MusicDirector] Beat counter started after timed delay.");
				delayedStart = false;
			}
			
			//Return if the beat counter hasn't started yet. (Timer < 0)
			else return;
		}

		#endregion

		#region Loading Events

		//If all queued events have been played, load next batch.
		if (LoadedBarCount > 0 && Events.Count == 0) {
			int start = CurrentBar + 1;
			int end = start + LoadedBarCount;
			Events = Track.GetEventsInRange(start, end, true);

			if (Events.Count == 0) {
				Debug.Log("[MusicDirector] No more events to load.");
				//TODO: Do something...? idk
			}
		}

		#endregion

		#region Event Playback

		foreach (TimedEvent e in Events) {
			double lastEventTime = 0d;
			if (LastPlayedEvent != null) lastEventTime = LastPlayedEvent.StartTime;

			bool isBeforeTimer = e.StartTime <= Timer;
			bool isAfterLast = lastEventTime < e.StartTime;

			if (isAfterLast && isBeforeTimer) {
				ExecuteEvent(e);
				LastPlayedEvent = e;
				CurrentBar = e.Bar;
				CurrentBeat = e.StartBeat;
				CurrentStep = e.StartStep;

				Debug.Log($"[MusicDirector] Executing event: {e.Type} at {e.StartTime}.");

				if (e.Type == TimedEvent.EventType.Attack) {
					AttackNote attack = (AttackNote) e;

					if (attack.Duration > 0) {
						Debug.Log("[MusicDirector] Event identified as a long attack.");
						LongAttacks.Add(attack);
					}
				}
			}
		}

		//Chip away at the long events.
		foreach (AttackNote attack in LongAttacks) {
			if (attack.LastStepPlayed == CurrentStep) continue;
			attack.LastStepPlayed = CurrentStep;

			//Create a dummy event to execute once.
			AttackNote tail = new(
				TimedEvent.EventType.Attack,
				attack.Attack,
				CurrentBeat,
				CurrentStep
			);
			ExecuteEvent(tail);

			attack.DurationLeft--;
			if (attack.DurationLeft <= 0) LongAttacks.Remove(attack);
		}

		#endregion
	}

	private void ExecuteEvent(TimedEvent e) {

	}
}
