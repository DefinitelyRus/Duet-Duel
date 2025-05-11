using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class StrippedEvents {

	#region Event Details

	/// <summary>
	///		The player whom this event is coming from.
	///		<br/><br/>
	///		This is only used for <see cref="AttackNote"/> events.
	/// </summary>
	public int OwnerID;

	/// <summary>
	///		The player whom this event will apply effects to, if any.
	/// </summary>
	public int TargetID;

	/// <summary>
	///		The type of event that will take place.
	/// </summary>
	public TimedEvent.EventType EventType;

	/// <summary>
	///		The type of attack this event will produce.
	///		<br/><br/>
	///		This is only used for <see cref="AttackNote"/> events.
	/// </summary>
	public AttackNote.AttackType AttackType;

	/// <summary>
	/// 	How much impact this attack has on the owner player's score.
	/// 	<br/><br/>
	/// 	This is only used for <see cref="AttackNote"/> events.
	/// </summary>
	public int Weight;

	/// <summary>
	///		How much impact this extended attack has on the owner player's score.
	///		<br/><br/>
	///		This is only used for <see cref="AttackNote"/> events.
	/// </summary>
	public int ExtendedWeight;

	#endregion

	#region Timings

	/// <summary>
	///		The bar count at which this event will start.
	/// </summary>
	public int StartBar;

	/// <summary>
	///		The beat count at which this event will start.
	/// </summary>
	public int StartBeat;

	/// <summary>
	/// 	The step count at which this event will start.
	/// </summary>
	public int StartStep;

	/// <summary>
	/// 	The time (in seconds) the event will be delayed.
	/// </summary>
	public float Offset;

	/// <summary>
	///		How many times (in steps) this attack will
	///		continue after the initial attack.
	///		<br/><br/>
	///		This is only used for <see cref="AttackNote"/> events.
	/// </summary>
	public int Duration;

	#endregion

	#region Conversion from TimedEvent/AttackNote to NoteJson object

	/// <summary>
	/// 	Constructor for the <see cref="StrippedEvents"/> class.
	/// </summary>
	public StrippedEvents(
		int owner,
		int target,
		int startBar,
		int startBeat,
		int startStep,
		int duration,
		float offset,
		int weight,
		int extendedWeight,
		TimedEvent.EventType eventType,
		AttackNote.AttackType attackType
		) {
		OwnerID = owner;
		TargetID = target;
		StartBar = startBar;
		StartBeat = startBeat;
		StartStep = startStep;
		Offset = offset;
		Duration = duration;
		EventType = eventType;
		AttackType = attackType;
		Weight = weight;
		ExtendedWeight = extendedWeight;
	}

	#endregion

	#region Conversion to/from JSON

	/// <summary>
	/// 	Converts a list of <see cref="StrippedEvents"/> objects to a JSON string.
	/// </summary>
	/// <returns>
	///		A JSON string representation of all the <see cref="StrippedEvents"/> objects.
	/// </returns>
	public static string ToJson(List<StrippedEvents> notes) {
		return JsonConvert.SerializeObject(notes, Formatting.Indented);
	}

	/// <summary>
	/// 	Converts a list of <see cref="TimedEvent"/> objects to a JSON string.
	/// </summary>
	/// <returns>
	///		A JSON string representation of all the <see cref="TimedEvent"/> objects.
	/// </returns>
	public static string ToJson(List<TimedEvent> events) {
		List<StrippedEvents> notes = new();

		foreach (TimedEvent timedEvent in events) {
			AttackNote attackNote = timedEvent as AttackNote;
			notes.Add( new(
				attackNote == null ? 0 : attackNote.PlayerID,
				(int) timedEvent.Target, //TODO: Make this ID-based
				timedEvent.StartBar,
				timedEvent.StartBeat,
				timedEvent.StartStep,
				attackNote == null ? 0 : attackNote.Duration,
				timedEvent.Offset,
				attackNote == null ? 0 : attackNote.Weight,
				attackNote == null ? 0 : attackNote.ExtendedWeight,
				timedEvent.Type,
				attackNote == null ? 0 : attackNote.Attack
				)
			);
		}

		return JsonConvert.SerializeObject(notes, Formatting.Indented);
	}

	/// <summary>
	///		Converts a JSON string to a list of <see cref="StrippedEvents"/> objects.
	/// </summary>
	/// <param name="json">
	///		A list of <see cref="StrippedEvents"/> objects as a JSON string. <br/>
	///		This is created using the <see cref="ToJson"/> method.
	/// </param>
	/// <returns>
	///		The parsed list of <see cref="StrippedEvents"/> objects. <br/>
	///		This is not yet ready to use; you have to convert it to
	///		<see cref="TimedEvent"/> objects first.
	/// </returns>
	public static List<StrippedEvents> FromJson(string json) {
		return JsonConvert.DeserializeObject<List<StrippedEvents>>(json);
	}

	public static List<StrippedEvents> FromJsonPath(string path) {
		//Check if the BeatmapPath is valid
		if (!File.Exists(path)) {
			Debug.LogError($"[StrippedEvents] Path does not exist: {path}");
			return null;
		}

		string json = File.ReadAllText(path);

		return FromJson(json);
	}

	#endregion
}