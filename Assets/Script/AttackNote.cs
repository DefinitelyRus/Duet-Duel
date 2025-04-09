public class AttackNote : TimedEvent {
	/// <summary>
	/// The player whom this attack is coming from.
	/// </summary>
	public Player Player;

	/// <summary>
	/// When the last projectile is spawned in a long attack.
	/// <br/><br/>
	/// Relative to Bar. <br/>
	/// Must be greater than StartBeat. <br/>
	/// Counts from 2. Values below 2 are considered equal to StartBeat.
	/// </summary>
	public int EndBeat = 0;

	/// <summary>
	/// When the last projectile is spawned in a long attack.
	/// Subdivision of a beat.
	/// <br/><br/>
	/// Relative to Bar. <br/>
	/// Must be greater than StartStep. <br/>
	/// Counts from 2. Values below 2 are considered equal to StartStep.
	/// </summary>
	public int EndStep = 0;

	/// <summary>
	/// The types of attacks the player can do.
	/// </summary>
	public enum AttackType { Projectile, Laser }

	/// <summary>
	/// Whether to use a projectile or a laser.
	/// </summary>
	public AttackType Attack = AttackType.Projectile;

	/// <summary>
	/// How much impact this attack has on the player's score.
	/// <br/><br/>
	/// Higher value = higher impact. <br/>
	/// </summary>
	public int Weight = 1;

	public AttackNote(
		EventType type,
		AttackType attack,
		int beatStart,
		int stepStart = 1,
		int beatEnd = 0,
		int stepEnd = 0,
		float offset = 0
		)
		: base(EventType.Attack, beatStart, stepStart, offset) {
		Type = type;
		Attack = attack;
		StartBeat = beatStart;
		EndBeat = beatEnd;
		StartStep = stepStart;
		EndStep = stepEnd;
		Offset = offset;
	}
}
