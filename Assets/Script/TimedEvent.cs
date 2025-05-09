using UnityEngine;

public class TimedEvent : MonoBehaviour {
	/// <summary>
	/// 	When the event starts.
	/// 	<br/><br/>
	/// 	Counts from 1. Values below 1 are invalid.
	/// </summary>
	public int StartBar = 0;

	/// <summary>
	///		When the event starts.
	///		<br/><br/>
	///		Relative to Bar. <br/>
	///		Counts from 1. Values below 1 are invalid.
	/// </summary>
	public int StartBeat = 0;

	/// <summary>
	///		When the event starts. A subdivision of a beat.
	///		<br/><br/>
	///		Relative to Bar. <br/>
	///		Counts from 1. Values below 1 are invalid.
	/// </summary>
	public int StartStep = 0;

	/// <summary>
	///		How long (in seconds) the event will be delayed.
	///		<br/><br/>
	///		Relative to its StartStep. <br/>
	///		Must not be longer than the duration of 1 step. <br/>
	///		This value does not affect AttackNotes' EndBeat or EndStep timings.
	/// </summary>
	public float Offset = 0;

	/// <summary>
	///		When the event starts in seconds.
	///		<br/><br/>
	///		DO NOT SET THIS MANUALLY. <br/>
	///		This is calculated in <see cref="Track"/> by getting the duration
	///		(in seconds) of the event's start step, beat, and bar combined.
	/// </summary>
	public double StartTime;

	/// <summary>
	///		The kinds of events that may be triggered.
	/// </summary>
	public enum EventType { None, Segment, Attack, Custom }

	/// <summary>
	/// 	What kind of event will take place at this time.
	/// </summary>
	public EventType Type = EventType.None;

	//TODO: Make this the same as AttackNote.PlayerID. Make it ID-based OR enum-based.
	/// <summary>
	/// 	Which player this event will apply its effects to.
	/// </summary>
	public enum TargetPlayer { None, Player1, Player2, Both }

	/// <summary>
	/// 	Which player this event will apply its effects to.
	/// </summary>
	public TargetPlayer Target = TargetPlayer.None;

	public TimedEvent(EventType type, int beat, int step, float offset = 0) {
		Type = type;
		StartBeat = beat;
		StartStep = step;
		Offset = offset;
	}

	/// <summary>
	/// Executes the event and may apply effects or read info from the given object.
	/// <br/><br/>
	/// The given object is usually a <see cref="Player"/>.
	/// </summary>
	/// <param name="obj"></param>
	public virtual void Execute(Object obj) {
		Debug.Log($"[TimedEvent] Executing {Type} event by doing nothing. Override me!");
	}
}
