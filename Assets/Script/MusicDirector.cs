using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicDirector : MonoBehaviour {

	#region Game Objects and Components

	[Header("Game Objects and Components")]
	public Player Player1;

	public Player Player2;

	public Track Track;

	private List<TimedEvent> EventsNow;

	private List<AttackNote> LongAttacks = new();

	public AudioSource MusicPlayer;

	private TimedEvent LastPlayedEvent;

	public AudioSource MetronomeHigh;

	public AudioSource MetronomeLow;

	#endregion

	#region Ticker

	/// <summary>
	///		How many bars have been played so far since the start of the song.
	///		<br/><br/>
	///		This must be initialized as 1. <br/>
	/// </summary>
	[Header("Ticker")]
	public int CurrentBar = 1;

	/// <summary>
	///		What step the player is currently on. Resets to 1 after every beat.
	///		<br/><br/>
	///		This must be initialized as 0. <br/>
	/// </summary>
	public int CurrentStep = 0;

	/// <summary>
	///		What beat the player is currently on. Resets to 1 after every bar.
	///		<br/><br/>
	///		This must be initialized as 2. <br/>
	/// </summary>
	public int CurrentBeat = 2;

	/// <summary>
	///		How long (in seconds) each step takes.
	///		This is used to check if a step has been completed since the previous frame.
	///		<br/><br/>
	///		Update this when changing the <see cref="Track.BPM"/>.
	/// </summary>
	public double StepDuration = 0;

	/// <summary>
	///		How many seconds passed since the last step.
	/// </summary>
	public double TimeSinceLastStep { get; private set; }

	/// <summary>
	///		Counts how many steps have passed since the last check.
	///		It will continue to accumulate time until
	///		1x (or higher) <see cref="StepDuration"/> has passed.
	///		At which point, the ticker increses by 1 step and the
	///		<see cref="TimeSinceLastStep"/> is reduced by <see cref="StepDuration"/>.
	/// </summary>
	/// <returns>
	///		The number of steps that have passed since you last checked.
	/// </returns>
	public int StepsAccumulated() {
		return (int) (TimeSinceLastStep / StepDuration);
	}

	/// <summary>
	///		Updates the CurrentStep, CurrentBeat, and CurrentBar
	///		with respect to their time signature and limits.
	/// </summary>
	/// <param name="stepCount">
	///		How many steps to move the ticker forward.
	/// </param>
	private void Tick(int stepCount, bool debug = false) {

		#region Checks

		if (stepCount == 0) {
			return;
		} else if (stepCount < 0) {
			Debug.LogError("[MusicDirector] stepCount cannot be a negative number.");
			return;
		}

		#endregion

		#region Calculation & Sounds

		//Next step
		CurrentStep += stepCount;
		TimeSinceLastStep -= StepDuration * stepCount;

		//Next beat
		while (CurrentStep > Track.Denominator) {
			CurrentBeat++;
			CurrentStep -= Track.Denominator;
			MetronomeLow.Stop();
			MetronomeLow.Play();

			if (debug) Debug.Log("[MusicDirector] Next beat!");

			Player1.FireProjectile(); //TODO: Remove!
		}

		//Next bar
		while (CurrentBeat > Track.Numerator) {
			CurrentBar++;
			CurrentBeat -= Track.Numerator;
			MetronomeHigh.Stop();
			MetronomeHigh.Play();

			if (debug) Debug.Log("[MusicDirector] Next bar!");

			Player1.FireLaser(); //TODO: Remove!
		}

		if (debug) Debug.Log($"[MusicDirector] {CurrentStep}:{CurrentBeat}:{CurrentBar}");

		#endregion

	}

	/// <summary>
	///		Acts as the gate to allow the ticker to begin ticking.
	///		<br/><br/>
	///		If <see cref="delayedStart"/> is true, this will update the
	///		<see cref="Timer"/> return false until the delayed time has passed. <br/>
	///		Once the ticker starts, this will no longer do anything.
	/// </summary>
	/// <returns>
	///		A clearance to start ticking away.
	/// </returns>
	bool HasTickerStarted() {

		//This will no longer run once both the beatmap and audio have started.
		if (delayedStart) {
			double offset = Track.StartOffset;

			//Music starts after timed delay.
			//+offset: Audio starts after the ticker.
			//Song starts at `Timer >= offset`.
			//Timer starts at 0.
			//Ticker starts at 0.
			if (offset > 0 && Timer >= offset) {
				Debug.Log("[MusicDirector] Music started after timed delay.");
				delayedStart = false;
				MusicPlayer.Play();
			}

			//Ticker starts after timed delay.
			//-offset: Audio starts before the ticker.
			//Song starts at `offset`.
			//Timer starts at `offset`.
			//Ticker starts at 0.
			else if (offset < 0 && Timer >= 0) {
				Debug.Log("[MusicDirector] Ticker started after timed delay.");
				delayedStart = false;
			}

			//Return false if the ticker hasn't started yet. (Timer < 0)
			else return false;
		}

		return true;
	}

	#endregion

	#region Miscellaneous

	/// <summary>
	/// How many bars to load at once.
	/// <br/><br/>
	/// 0 = Load all events at once. <br/>
	/// For really short songs or if you have a lot of memory. <br/>
	/// Good for avoiding lag spikes when loading a large number of events.
	/// <br/><br/>
	/// 1-64 = Load X number of bars when all loaded events have been played. <br/>
	/// 1-4 = Good for high-density beat maps. <br/>
	/// 5-16 = Good for long songs. <br/>
	/// 17-64 = Only use if the song is too long and/or dense for 0. <br/>
	/// 65+ = You should just use 0.
	/// </summary>
	[Header("Miscellaneous")]
	[Range(0, 64)]
	public int LoadedBarCount = 16;

	/// <summary>
	///		How many seconds has passed since the ticker started.
	/// </summary>
	private double Timer = 0d;

	#endregion

	#region Flags

	/// <summary>
	///		Whether to make a sound whenever it ticks to a new beat/bar.
	/// </summary>
	[Header("Flags")]
	public bool EnableMetronome = false;

	/// <summary>
	///		Marks whether the song has no more <see cref="TimedEvent"/>s.
	///		<br/><br/>
	///		This prevents further event loading attempts when all
	///		<see cref="TimedEvent"/> objects have already been loaded.
	/// </summary>
	public bool HasSongEnded = false;

	/// <summary>
	///		Marks whether the audio starts before or after when the ticker starts.
	/// </summary>
	private bool delayedStart = false;

	#endregion

	#region Unity

	void Start() {

		#region Loading audio and events

		try {
			Debug.Log("[MusicDirector] Loading audio...");
			MusicPlayer.clip = Track.AudioClip;
		}
		
		catch (NullReferenceException e) {
			Debug.LogError($"[MusicDirector] Failed to load audio clip: {e.Message}");
		}

		Debug.Log("[MusicDirector] Loading events...");
		EventsNow = Track.GetEventsInRange(0, LoadedBarCount, true);

		#endregion

		#region Audio/Ticker start delay

		Debug.Log("[MusicDirector] Loading configuring start delay...");

		//Audio starts after delay
		if (Track.StartOffset > 0) {
			Debug.Log($"[MusicDirector] Audio will start after a timed delay.");

			delayedStart = true;
		}

		//Ticker starts after delay
		else if (Track.StartOffset < 0) {
			Debug.Log($"[MusicDirector] Ticker will start after a timed delay.");

			Timer = -Track.StartOffset;
			delayedStart = true;
			MusicPlayer.Play();
		}

		StepDuration = 60d / Track.BPM / Track.Denominator;

		#endregion

	}

	/// <summary>
	/// How long has passed since the last tick.
	/// <br/><br/>
	/// This is a substitute for <see cref="Time.deltaTime"/>.
	/// </summary>
	double MusicDeltaTime = 0d;

	private void FixedUpdate() {

		MusicDeltaTime = MusicPlayer.time - Timer;

		//Timer += Time.fixedDeltaTime;
		Timer = MusicPlayer.time;

		//Waits until ticker starts.
		if (!HasTickerStarted()) return;

		TimeSinceLastStep += MusicDeltaTime;

		Tick(StepsAccumulated());
	}

	void Update() {

		#region Checks

		//Waits until ticker starts.
		if (HasTickerStarted()) return;

		#endregion

		#region Loading Events

		//If all queued events have been played, load next batch.
		//Does so only if LoadedBarCount is positive.
		if (EventsNow.Count == 0 && LoadedBarCount > 0 && !HasSongEnded) {
			int start = CurrentBar + 1;
			int end = start + LoadedBarCount;
			EventsNow = Track.GetEventsInRange(start, end, true);

			if (EventsNow.Count == 0) {
				Debug.Log("[MusicDirector] No more events to load.");
				HasSongEnded = true;

				//TODO: Do something...? idk
			}
		}
		//NOTE: Can be optimized by loading ahead of the playback.
		//NOTE: Can be optimized by loading events in another thread.

		#endregion

		#region Event Playback

		foreach (TimedEvent e in EventsNow) {
			double lastEventTime = 0d;
			if (LastPlayedEvent != null) lastEventTime = LastPlayedEvent.StartTime;

			bool isBeforeTimer = e.StartTime <= Timer;
			bool isAfterLast = lastEventTime < e.StartTime;

			if (isAfterLast && isBeforeTimer) {
				e.Execute(null); //TODO: FIX
				LastPlayedEvent = e;

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
			tail.Execute(null); //TODO: FIX

			attack.DurationLeft--;
			if (attack.DurationLeft <= 0) LongAttacks.Remove(attack);
		}

		#endregion

	}

	#endregion

}
