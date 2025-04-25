using UnityEngine;

public class AttackNote : TimedEvent {
	/// <summary>
	///		The player whom this attack is coming from.
	/// </summary>
	public int PlayerID;

	/// <summary>
	///		How many times (in steps) this attack will continue after the initial attack.
	///		<br/><br/>
	///		0 = No tail. No extended attack. <br/>
	///		1+ = Will attack once after every step for X number of times.
	/// </summary>
	public int Duration = 0;

	/// <summary>
	///		How many times left (in steps) this attack will continue.
	/// </summary>
	public int DurationLeft = 0;

	/// <summary>
	///		The types of attacks the player can do.
	/// </summary>
	public enum AttackType { Projectile, Laser }

	/// <summary>
	///		Whether to use a projectile or a laser.
	/// </summary>
	public AttackType Attack = AttackType.Projectile;

	/// <summary>
	///		How much impact this attack has on the player's score.
	///		<br/><br/>
	///		Higher value = higher impact. <br/>
	/// </summary>
	public int Weight = 1;

	/// <summary>
	///		How much impact the tail of this attack has on the player's score.
	///		<br/><br/>
	///		Higher value = higher impact. <br/>
	///		Since this is a repeating attack, I suggest keeping this value low.
	///	</summary>
	public int ExtendedWeight = 1;

	/// <summary>
	///		When the attack was last played.
	///		<br/><br/>
	///		Yes, it's not accompanied by a `LastBeatPlayed`.
	///		All we need to know is whether it's still on the same step or not. <br/>
	///		This is used for tracking long notes.
	/// </summary>
	public int LastStepPlayed = 0;

	public AttackNote(
		EventType type,
		AttackType attack,
		int beatStart,
		int stepStart = 1,
		int duration = 0,
		float offset = 0
		)
		: base(EventType.Attack, beatStart, stepStart, offset) {
		Type = type;
		Attack = attack;
		StartBeat = beatStart;
		StartStep = stepStart;
		Duration = duration;
		DurationLeft = duration;
		Offset = offset;
	}

	public override void Execute(Object obj) {
		Debug.Log($"[AttackNote] Firing {Attack}...");

		//TODO: Change `Player` attribute to be a number, not a reference.
		//      Get player object reference here only.
		//TODO: Ensure that the scores apply.

		Player player = Player.GetPlayerInstance(PlayerID);

		if (Attack == AttackType.Projectile) player.FireProjectile();
		else if (Attack == AttackType.Laser) player.FireLaser();
	}
}
