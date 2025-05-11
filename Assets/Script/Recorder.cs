using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Recorder : MonoBehaviour {

	#region Game Objects and Components

	[Header("Game Objects and Components")]
	public AttackNote CurrentAttack;

	public TimedEvent CurrentEvent;

	public MusicDirector Director;

	private AttackNote.AttackType AttackType;

	private TimedEvent.EventType EventType;

	Player AttackingPlayer;

	#endregion

	#region Input Handling

	[Header("Input Keys")]
	public KeyCode StepInput1 = KeyCode.Z;
	public KeyCode StepInput2 = KeyCode.X;
	public KeyCode BeatInput = KeyCode.C;

	public KeyCode SingleStepInput = KeyCode.LeftShift;
	public KeyCode OffsetInput = KeyCode.LeftControl;

	public KeyCode P1Projectile = KeyCode.Alpha1;
	public KeyCode P1Laser = KeyCode.Alpha2;
	public KeyCode P2Projectile = KeyCode.Alpha3;
	public KeyCode P2Laser = KeyCode.Alpha4;
	public KeyCode NoneEvent = KeyCode.Alpha5;
	public KeyCode SegmentEvent = KeyCode.Alpha6;
	public KeyCode CustomEvent = KeyCode.Alpha7;

	public KeyCode SaveKey = KeyCode.Return;
	public KeyCode LoadKey = KeyCode.L;

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

		bool isPressed =
			Input.GetKeyDown(StepInput1) ||
			Input.GetKeyDown(StepInput2) ||
			Input.GetKeyDown(BeatInput);
		bool stillPressed =
			Input.GetKey(StepInput1) ||
			Input.GetKey(StepInput2) ||
			Input.GetKey(BeatInput);
		bool isReleased =
			Input.GetKeyUp(StepInput1) ||
			Input.GetKeyUp(StepInput2) ||
			Input.GetKey(BeatInput);


		// Disables input snapping.
		// Assigns based on StepInputAllowance by default.
		bool usesExactStep = Input.GetKey(SingleStepInput); // Assigns to the exact step

		int currentStep = Director.CurrentStep;
		int assignedStep = 0;



		//Input snapping
		//Calculates where to snap to. Will snap to step 1 or 3 in a 4-step measure.
		if (!usesExactStep) {

			for (int i = 0; i < SnapSteps.Length; i++) {

				int currentSnap = SnapSteps[i];
				int nextSnap = (i + 1 == SnapSteps.Length) ? int.MaxValue : SnapSteps[i + 1];

				//1 or 3. equal to currentSnap (1)
				if (currentStep == currentSnap) {
					assignedStep = currentStep;
					break;
				}

				//2 or 4. more than currentSnap (1). less than nextSnap (3).
				else if (currentStep > currentSnap && currentStep < nextSnap) {
					assignedStep = currentSnap;
					break;
				}
			}

			if (assignedStep == 0) {
				if (!usesExactStep) Debug.LogError("[Recorder] Assigned step is 0. Falling back to currentStep.");
				assignedStep = currentStep;
			}
		}



		//Initial press
		if (isPressed) {
			if (InputType == 1) NewAttack(assignedStep, true);
			else if (InputType == 2) NewEvent(assignedStep, true);
			else Debug.LogWarning("[Recorder] Invalid or no input type selected.");
		}

		//Hold
		else if (stillPressed && InputType == 1) ExtendAttack(true);

		//Release
		if (isReleased && !stillPressed) CurrentAttack = null;



		#endregion

		#region Data Handling Inputs

		if (Input.GetKeyDown(SaveKey)) Save(true);

		if (Input.GetKeyDown(LoadKey)) Load(true);

		#endregion
	}

	#endregion

	#region Recorder Controls

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
	[Header("Recorder Controls")]
	public float InputAllowance = 0.7f;

	public string LoadFilePath = string.Empty;

	public int InputType = 0;

	/// <summary>
	///		The steps in which inputs will snap to unless overridden.
	///		<br/><br/>
	///		Snaps to the largest value equal to or less than the current step.
	///		For example, if the current step is 2, it will snap to 1.
	/// </summary>
	public int[] SnapSteps = { 1, 3 };

	#endregion

	#region Miscellaneous

	[Header("Ignore")]
	public int LastStepPlayed = 0;

	#endregion

	#region Event Creation

	public void NewEvent(int stepOverride = 0, bool debug = false) {

		if (debug) Debug.Log("[Recorder] Creating new event...");

		#region Early input handling

		int assignedStep = (stepOverride == 0) ? Director.CurrentStep : stepOverride;
		int assignedBeat = Director.CurrentBeat;
		int assignedBar = Director.CurrentBar;

		if (Director.TimeSinceLastStep > Director.StepDuration * InputAllowance) {
			assignedStep++;
			if (debug) Debug.Log($"[Recorder] Early input detected. Assigning to next step: {Director.CurrentStep} -> {assignedStep}");

			if (assignedStep > Director.Track.Denominator) {
				assignedStep -= Director.Track.Denominator;
				assignedBeat++;
				if (debug) Debug.Log($"[Recorder] Exceeded denominator. Assigning to next beat: {Director.CurrentBeat} -> {assignedBeat}");
			}

			if (assignedBeat > Director.Track.Numerator) {
				assignedBeat -= Director.Track.Numerator;
				assignedBar++;
				if (debug) Debug.Log($"[Recorder] Exceeded numerator. Assigning to next bar: {Director.CurrentBar} -> {assignedBar}");
			}
		}

		#endregion

		string tickAt = $"{assignedBar}:{assignedBeat}:{assignedStep}";
		GameObject obj = new($"Event {tickAt} - {EventType}");

		TimedEvent note = obj.AddComponent<TimedEvent>();

		note.Type = EventType;
		note.StartBar = assignedBar;
		note.StartBeat = assignedBeat;
		note.StartStep = assignedStep;

		obj.transform.SetParent(gameObject.transform);

		CurrentEvent = note;

		if (debug) Debug.Log($"[Recorder] New {note.Type} event at {tickAt}.");
	}

	/// <summary>
	///		Creates a new <see cref="AttackNote"/> object assigned to the current tick.
	/// </summary>
	/// <param name="debug">
	/// </param>
	private void NewAttack(int stepOverride = 0, bool debug = false) {

		if (debug) Debug.Log("[Recorder] Creating new attack...");

		#region Early input handling

		int assignedStep = (stepOverride == 0) ? Director.CurrentStep : stepOverride;
		int assignedBeat = Director.CurrentBeat;
		int assignedBar = Director.CurrentBar;

		bool inNextStep = Director.TimeSinceLastStep > Director.StepDuration * InputAllowance;

		if (inNextStep) {

			//Override
			if (stepOverride > 0) {
				for (int i = 1; i < SnapSteps.Length; i++) {
					//2 -> 3
					if (assignedStep + 1 == SnapSteps[i]) assignedStep++;

					//1 -/> 2, 3 -/> 4
					else break;
				}
			}

			//No override
			else assignedStep++;


			if (debug) Debug.Log($"[Recorder] Early input detected. Assigning to next step: {Director.CurrentStep} -> {assignedStep}");

			if (assignedStep > Director.Track.Denominator) {
				assignedStep -= Director.Track.Denominator;
				assignedBeat++;
				if (debug) Debug.Log($"[Recorder] Exceeded denominator. Assigning to next beat: {Director.CurrentBeat} -> {assignedBeat}");
			}

			if (assignedBeat > Director.Track.Numerator) {
				assignedBeat -= Director.Track.Numerator;
				assignedBar++;
				if (debug) Debug.Log($"[Recorder] Exceeded numerator. Assigning to next bar: {Director.CurrentBar} -> {assignedBar}");
			}
		}

		#endregion

		string tickAt = $"{assignedBar}:{assignedBeat}:{assignedStep}";
		GameObject obj = new($"Attack {tickAt} - {AttackingPlayer.name} {AttackType}");

		AttackNote note = obj.AddComponent<AttackNote>();

		note.Type = EventType;
		note.Attack = AttackType;
		note.StartBar = assignedBar;
		note.StartBeat = assignedBeat;
		note.StartStep = assignedStep;
		note.PlayerID = AttackingPlayer.ID;

		obj.transform.SetParent(gameObject.transform);

		CurrentAttack = note;

		if (debug) Debug.Log($"[Recorder] New {note.Attack} attack at {tickAt}.");

		if (note.Attack == AttackNote.AttackType.Projectile) AttackingPlayer.FireProjectile();
		else if (note.Attack == AttackNote.AttackType.Laser) AttackingPlayer.FireLaser();
	}

	//BUG: For some reason this doesn't work. I'm not sure why.
	private void ExtendAttack(bool debug = false) {
		int stepsToAdd = Director.StepsAccumulated();

		if (stepsToAdd == 0) return;

		CurrentAttack.Duration += stepsToAdd;

		if (debug) Debug.Log($"[Recorder] Increased {CurrentAttack.name} duration to {CurrentAttack.Duration}");
	}

	#endregion

	#region Data Handling

	void Save(bool debug = false) {

		List<TimedEvent> eventObjects = new();

		foreach (Transform child in gameObject.transform) {

			if (child.gameObject.TryGetComponent<TimedEvent>(out var timedEvent)) {
				eventObjects.Add(timedEvent);

				if (debug) Debug.Log($"[Recorder] Saved {timedEvent.Type} event at {timedEvent.StartStep}:{timedEvent.StartBeat}:{timedEvent.StartBar}.");

				Destroy(child.gameObject);
			}
		}

		string json = StrippedEvents.ToJson(eventObjects);

		if (debug) Debug.Log($"Recorded notes into JSON:\n{json}");

		string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		string game = $"{docs}/{Application.productName}";
		string beatmap = $"{game}/Beatmaps";
		string file = $"DDBeatmap by {Director.Track.BeatMapCreator}, {Director.Track.Artist} - {Director.Track.Name}.json";
		string path = $"{beatmap}/{file}";

		if (!Directory.Exists(beatmap)) {
			Directory.CreateDirectory(beatmap);
			if (debug) Debug.Log($"[Recorder] Created {path}.");
		}

		File.WriteAllText(path, json);

		if (debug) Debug.Log($"[Recorder] Saved beatmap to {path}.");
	}

	void Load(bool debug = false) {
		string path;

		//Uses absolute path
		if (LoadFilePath.Length > 2 && LoadFilePath[1] == ':') {
			path = LoadFilePath;

			if (debug) Debug.Log("[Recorder] Loading beatmap from the Load File Path.");
		}

		//Uses info from the Track object
		else {
			string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string game = $"{docs}/{Application.productName}";
			string beatmap = $"{game}/Beatmaps";
			string file = $"DDBeatmap by {Director.Track.BeatMapCreator}, {Director.Track.Artist} - {Director.Track.Name}.json";
			path = $"{beatmap}/{file}";

			if (debug) Debug.Log("[Recorder] Loading beatmap from existing Track data.");
		}

		//Check if the path is valid
		if (!File.Exists(path)) {
			Debug.LogError($"[Recorder] Beatmap path does not exist: {path}");
			return;
		}

		if (debug) Debug.Log($"[Recorder] Loading beatmap from {path}.");
		
		List<StrippedEvents> strippedEvents = StrippedEvents.FromJsonPath(path);

		foreach (StrippedEvents note in strippedEvents) {
			string tickAt = $"{note.StartBar}:{note.StartBeat}:{note.StartStep}";
			GameObject obj = new($"Event {tickAt} - {note.EventType}");

			//Attack event
			if (note.EventType == TimedEvent.EventType.Attack) {
				AttackNote attackNote = obj.AddComponent<AttackNote>();
				attackNote.Type = note.EventType;
				attackNote.Attack = note.AttackType;
				attackNote.StartBar = note.StartBar;
				attackNote.StartBeat = note.StartBeat;
				attackNote.StartStep = note.StartStep;
				attackNote.PlayerID = note.OwnerID;
			}

			//Other events
			else {
				TimedEvent timedEvent = obj.AddComponent<TimedEvent>();
				timedEvent.Type = note.EventType;
				timedEvent.StartBar = note.StartBar;
				timedEvent.StartBeat = note.StartBeat;
				timedEvent.StartStep = note.StartStep;
			}

			obj.transform.SetParent(gameObject.transform);

			if (debug) Debug.Log($"[Recorder] Loaded {note.EventType} event at {tickAt}.");
		}
	}

	#endregion

	#region Unity

	void Update() {
		InputListener();
	}

	#endregion
}
