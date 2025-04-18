using UnityEngine;
using UnityEngine.InputSystem.Controls;
using static TimedEvent;

public class Recorder : MonoBehaviour
{
	[Header("Input Keys")]
	public KeyCode InputKey1 = KeyCode.Z;
	public KeyCode InputKey2 = KeyCode.X;
	public KeyCode InputKey3 = KeyCode.C;

	public KeyCode P1Projectile = KeyCode.Alpha1;
	public KeyCode P1Laser = KeyCode.Alpha2;
	public KeyCode P2Projectile = KeyCode.Alpha3;
	public KeyCode P2Laser = KeyCode.Alpha4;
	public KeyCode NoneEvent = KeyCode.Alpha5;
	public KeyCode SegmentEvent = KeyCode.Alpha6;
	public KeyCode CustomEvent = KeyCode.Alpha7;

	public KeyCode SaveKey = KeyCode.Return;

	public AttackNote CurrentAttack;

	public TimedEvent CurrentEvent;

	public MusicDirector Director;

	private AttackNote.AttackType AttackType;

	private TimedEvent.EventType EventType;

	Player AttackingPlayer;

	public int LastStepPlayed = 0;

	/// <summary>
	///		How much (%) of a step can be played through before an
	///		input will be considered an early input for the step after it.
	///		<br/><br/>
	///		1.0: No input buffering. <br/>
	///		0.5: Input will be recorded for the next step
	///			 if 50% of the step's duration has already passed. <br/>
	///		0.0: Input will be recorded for the next step, never the current.
	///		<br/><br/>
	///		This allows the beat mapper to input slightly early and
	///		still have it be correctly identified as the intended step.
	/// </summary>
	public float InputAllowance = 0.7f;

	#region Input Handling

	public int InputType = 0;

	/// <summary>
	///		If enabled, all inputs will automatically be
	///		mapped to the first step of the current beat.
	///		<br/><br/>
	///		This is enabled only when <see cref="InputKey3"/> is used OR
	///		if <see cref="InputKey1"/> and <see cref="InputKey2"/> are
	///		both NOT pressed, though the latter it won't have any effect.
	/// </summary>
	public bool LowPrecisionInput = false;

	public void NewEvent(bool debug = false) {

		if (debug) Debug.Log("[Recorder] Creating new event...");

		#region Early input handling

		int AssignedStep = LowPrecisionInput ? 1 : Director.CurrentStep;
		int AssignedBeat = Director.CurrentBeat;
		int AssignedBar = Director.CurrentBar;

		if (Director.TimeSinceLastStep > Director.StepDuration * InputAllowance && !LowPrecisionInput) {
			AssignedStep++;
			if (debug) Debug.Log($"[Recorder] Early input detected. Assigning to next step: {Director.CurrentStep} -> {AssignedStep}");

			if (AssignedStep > Director.Track.Denominator) {
				AssignedStep -= Director.Track.Denominator;
				AssignedBeat++;
				if (debug) Debug.Log($"[Recorder] Exceeded denominator. Assigning to next beat: {Director.CurrentBeat} -> {AssignedBeat}");
			}

			if (AssignedBeat > Director.Track.Numerator) {
				AssignedBeat -= Director.Track.Numerator;
				AssignedBar++;
				if (debug) Debug.Log($"[Recorder] Exceeded numerator. Assigning to next bar: {Director.CurrentBar} -> {AssignedBar}");
			}
		}

		#endregion

		string tickAt = $"{AssignedBar}:{AssignedBeat}:{AssignedStep}";
		GameObject obj = new($"Event {tickAt} - {EventType}");

		TimedEvent note = obj.AddComponent<TimedEvent>();

		note.Type = EventType;
		note.StartBar = AssignedBar;
		note.StartBeat = AssignedBeat;
		note.StartStep = AssignedStep;

		obj.transform.SetParent(gameObject.transform);

		CurrentEvent = note;

		if (debug) Debug.Log($"[Recorder] New {note.Type} event at {tickAt}.");
	}

	/// <summary>
	///		Creates a new <see cref="AttackNote"/> object assigned to the current tick.
	/// </summary>
	/// <param name="debug">
	/// </param>
	private void NewAttack(bool debug = false) {

		if (debug) Debug.Log("[Recorder] Creating new attack...");

		#region Early input handling

		int AssignedStep = LowPrecisionInput ? 1 : Director.CurrentStep;
		int AssignedBeat = Director.CurrentBeat;
		int AssignedBar = Director.CurrentBar;

		if (Director.TimeSinceLastStep > Director.StepDuration * InputAllowance && !LowPrecisionInput) {
			AssignedStep++;
			if (debug) Debug.Log($"[Recorder] Early input detected. Assigning to next step: {Director.CurrentStep} -> {AssignedStep}");

			if (AssignedStep > Director.Track.Denominator) {
				AssignedStep -= Director.Track.Denominator;
				AssignedBeat++;
				if (debug) Debug.Log($"[Recorder] Exceeded denominator. Assigning to next beat: {Director.CurrentBeat} -> {AssignedBeat}");
			}

			if (AssignedBeat > Director.Track.Numerator) {
				AssignedBeat -= Director.Track.Numerator;
				AssignedBar++;
				if (debug) Debug.Log($"[Recorder] Exceeded numerator. Assigning to next bar: {Director.CurrentBar} -> {AssignedBar}");
			}
		}

		#endregion

		string tickAt = $"{AssignedBar}:{AssignedBeat}:{AssignedStep}";
		GameObject obj = new($"Attack {tickAt} - {AttackingPlayer.name} {AttackType}");

		AttackNote note = obj.AddComponent<AttackNote>();

		note.Type = EventType;
		note.Attack = AttackType;
		note.StartBar = AssignedBar;
		note.StartBeat = AssignedBeat;
		note.StartStep = AssignedStep;
		note.Player = AttackingPlayer;

		obj.transform.SetParent(gameObject.transform);

		CurrentAttack = note;

		if (debug) Debug.Log($"[Recorder] New {note.Attack} attack at {tickAt}.");
	}

	private void ExtendAttack(bool debug = false) {
		int steps = Director.StepsAccumulated();

		if (steps == 0) return;

		CurrentAttack.Duration += steps;

		if (debug) Debug.Log($"[Recorder] Increased {CurrentAttack.name} duration to {CurrentAttack.Duration}");

		//BUG: For some reason this doesn't work. I'm not sure why.
	}

	void Save() {
		//Find all objects that has name that starts with "Attack" and has a script "AttackNote".
		//Save them all into a list then convert to JSON.
	}

	/// <summary>
	///		Contains all the checker logic for handling user inputs.
	/// </summary>
	/// <param name="debug">
	///		Whether to print logs into the console.
	/// </param>
	private void InputListener(bool debug = false) {

		#region Mode switching

		if (Input.GetKeyDown(P1Projectile)) {
			if (debug) Debug.Log("[Recorder] Inputting as Player 1 Projectile.");
			AttackingPlayer = Director.Player1;
			EventType = TimedEvent.EventType.Attack;
			AttackType = AttackNote.AttackType.Projectile;
			InputType = 1;
		}

		else if (Input.GetKeyDown(P1Laser)) {
			if (debug) Debug.Log("[Recorder] Inputting as Player 1 Laser.");
			AttackingPlayer = Director.Player1;
			EventType = TimedEvent.EventType.Attack;
			AttackType = AttackNote.AttackType.Laser;
			InputType = 1;
		}

		else if (Input.GetKeyDown(P2Projectile)) {
			if (debug) Debug.Log("[Recorder] Inputting as Player 2 Projectile.");
			AttackingPlayer = Director.Player2;
			EventType = TimedEvent.EventType.Attack;
			AttackType = AttackNote.AttackType.Projectile;
			InputType = 1;
		}

		else if (Input.GetKeyDown(P2Laser)) {
			if (debug) Debug.Log("[Recorder] Inputting as Player 2 Laser.");
			AttackingPlayer = Director.Player2;
			EventType = TimedEvent.EventType.Attack;
			AttackType = AttackNote.AttackType.Laser;
			InputType = 1;
		}

		else if (Input.GetKeyDown(NoneEvent)) {
			if (debug) Debug.Log("[Recorder] Inputting as None Event.");
			EventType = TimedEvent.EventType.None;
			InputType = 2;
		}

		else if (Input.GetKeyDown(SegmentEvent)) {
			if (debug) Debug.Log("[Recorder] Inputting as Segment Event.");
			EventType = TimedEvent.EventType.Segment;
			InputType = 2;
		}

		else if (Input.GetKeyDown(CustomEvent)) {
			if (debug) Debug.Log("[Recorder] Inputting as Custom Event.");
			EventType = TimedEvent.EventType.Custom;
			InputType = 2;
		}

		#endregion

		#region Timed Inputs

		LowPrecisionInput = Input.GetKeyDown(InputKey3);
		bool isPressed =
			Input.GetKeyDown(InputKey1) ||
			Input.GetKeyDown(InputKey2) ||
			Input.GetKeyDown(InputKey3);
		bool stillPressed =
			Input.GetKey(InputKey1) ||
			Input.GetKey(InputKey2) ||
			Input.GetKey(InputKey3);
		bool isReleased =
			Input.GetKeyUp(InputKey1) ||
			Input.GetKeyUp(InputKey2) ||
			Input.GetKey(InputKey3);

		//Initial press
		if (isPressed) {
			if (InputType == 1) NewAttack(true);
			else if (InputType == 2) NewEvent(true);
			else Debug.LogWarning("[Recorder] Invalid or no input type selected.");
		}

		//Hold
		else if (stillPressed && InputType == 1) ExtendAttack(true);

		//Release
		if (isReleased && !stillPressed) CurrentAttack = null;

		#endregion

		if (Input.GetKeyDown(SaveKey)) Save();

	}

	#endregion

	void Update() {
		InputListener();
	}
}
