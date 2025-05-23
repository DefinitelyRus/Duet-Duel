using UnityEngine;

public class Player : MonoBehaviour {

	#region Physics

	#region Horizontal Movement

	[Header("Horizontal Movement")]
	public float Acceleration;

	public float MaxVelocity;

	public enum MoveType { Keyboard, Gamepad }

	public MoveType MoveMode = MoveType.Keyboard;

	public void MoveListener(bool debug = false) {
		Vector2 velocity = Vector2.zero;

		//Keyboard Movement
		if (MoveMode == MoveType.Keyboard) {
			int leftInput = Input.GetKey(KeyCode.A) ? 1 : 0;
			int rightInput = Input.GetKey(KeyCode.D) ? 1 : 0;
			if (leftInput == 0 && rightInput == 0) return;

			//Applies the force to the player.
			float multiplier = Acceleration * Time.fixedDeltaTime;
			Vector2 leftVelocity = leftInput * multiplier * Vector2.left;
			Vector2 rightVelocity = rightInput * multiplier * Vector2.right;
			velocity = leftVelocity + rightVelocity;
		}

		//Gamepad Movement
		else if (MoveMode == MoveType.Gamepad) {
			Vector2 input;
			if (ID == 1) input = Controls.P1MovementInput;
			else if (ID == 2) input = Controls.P2MovementInput;
			else {
				if (debug) Debug.LogWarning($"[Player] Invalid ID; cannot retrieve gamepad.");
				return;
			}
			
			velocity = new(input.x * Acceleration * Time.fixedDeltaTime, 0);
		}

		Rigidbody.AddForce(velocity, ForceMode2D.Force);

		Rigidbody.linearVelocityX = Mathf.Clamp(Rigidbody.linearVelocityX, -MaxVelocity, MaxVelocity);
	}

	#endregion

	#region Jump

	[Header("Jump")]
	public float InitialJumpForce;

	public float ExtendJumpForce;

	private float extendJumpTimer;

	public float ExtendMinTime;

	public float ExtendMaxTime;

	private bool isJumpQueued;

	private bool isJumping;

	/// <summary>
	///		If the player is mid-air and inputs a jump a little too early,
	///		this determines how far off the ground the player can be
	///		while the input is still considered valid.
	/// </summary>
	public float BufferDistance;

	/// <summary>
	///		Whether the player is currently touching the ground.
	/// </summary>
	private bool isGrounded;

	/// <summary>
	///		How far away the player can be to be considered "on the ground".
	/// </summary>
	public float GroundedDistance;

	/// <summary>
	///		Listens for jump inputs every frame and hands off the
	///		physics logic to the appropriate methods.
	/// </summary>
	/// <param name="debug">Whether to print logs to console.</param>
	private void JumpListener(bool debug = false) {
		if (Input.GetKeyDown(KeyCode.Space) && MoveMode == MoveType.Keyboard) QueueJump(debug);

		//Gamepad inputs
		bool gamepadJump = false;
		if (MoveMode == MoveType.Gamepad) {
			Vector2 input;
			if (ID == 1) input = Controls.P1MovementInput;
			else if (ID == 2) input = Controls.P2MovementInput;
			else {
				if (debug) Debug.LogWarning($"[Player] Invalid ID; cannot retrieve gamepad.");
				return;
			}

			//Polarized Inputs
			gamepadJump = input.y > 0.9;
		}

		//TODO: Allow coyote time
		bool jumpInput;
		if (MoveMode == MoveType.Keyboard) jumpInput = Input.GetKeyDown(KeyCode.Space);
		else if (MoveMode == MoveType.Gamepad) jumpInput = gamepadJump;
		else jumpInput = false;


		bool doGroundedJump = jumpInput && isGrounded;
		bool jumpOnImpact = isJumpQueued && isGrounded && !isJumping; //Input buffering
		if (doGroundedJump || jumpOnImpact) Jump(debug);

		//Apply markers
		if (Input.GetKeyUp(KeyCode.Space)) {
			if (debug) Debug.Log($"[Player | {name}] Jump released!");
			isJumpQueued = false;
			isJumping = false;
			extendJumpTimer = 0;
		}
	}

	/// <summary>
	///		Checks if the player is close enough to the ground.
	///		If so, it will make the player jump the moment it touches the ground.
	/// </summary>
	/// <param name="debug">Whether to print logs to console.</param>
	private void QueueJump(bool debug = false) {
		if (isJumpQueued) return;

		string log = $"[Player | {name}] ";

		//Perform a raycast to check if the player is close enough to the ground.
		float colliderBaseY = Collider.size.y / 2;
		float verticalAllowance = colliderBaseY + BufferDistance;
		RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, verticalAllowance, LayerMask.GetMask("Environment"));

		if (hit.collider != null) {
			isJumpQueued = true;
			if (debug) log += "Jump queued!";
		}
		
		else if (hit.collider == null && debug) log += "Jump not queued!";
		if (debug) Debug.Log(log);
	}

	/// <summary>
	///		Applies the actual jumping physics logic to the player.
	/// </summary>
	/// <param name="debug">Whether to print logs to console.</param>
	public void Jump(bool debug = false) {
		float delta = Time.fixedDeltaTime;
		string log = $"[Player | {name}] ";

		if (debug) log += $" Queued: {isJumpQueued} | Grounded: {isGrounded} | Jumping: {isJumping}";

		//Initial jump. Cannot reactivate until player touches the ground.
		if (!isJumping) {
			if (debug) log += $"\nInitial jump!";
			Rigidbody.AddForce(Vector2.up * InitialJumpForce, ForceMode2D.Impulse);
			isJumping = true;
			isGrounded = false;
			isJumpQueued = false;
		}

		//Suspended jump.
		//TODO: Separate this into a different method.
		else if (isJumping) {
			//Extend jump time buffer.
			if (extendJumpTimer < ExtendMinTime) extendJumpTimer += delta;

			//Extending jump.
			else if (extendJumpTimer < ExtendMaxTime) {
				extendJumpTimer += delta;
				Rigidbody.AddForce(delta * ExtendJumpForce * Vector2.up, ForceMode2D.Force);

				if (debug) log = $"\nExtending jump!";
			}

			//Extend limit reached.
			else if (extendJumpTimer >= ExtendMaxTime) {
				if (debug) log = $"\nJump limit reached!";
				isJumping = false;
			}
		}

		if (debug) Debug.Log(log);
	}

	#endregion

	#endregion

	#region Combat

	#region Inputs

	private Vector2 AimVector;

	public enum AimType { Mouse, Joystick }

	public AimType AimMode;

	public void AimAttack() {
		//Mouse Input
		if (AimMode == AimType.Mouse) {
			//Get the mouse position.
			Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

			//Get the direction to the mouse.
			AimVector = (mousePosition - (Vector2) transform.position).normalized;
		}

		//Joystick Input
		else if (AimMode == AimType.Joystick) {
			if (ID == 1) AimVector = Controls.P1AimInput;
			else if (ID == 2) AimVector = Controls.P2AimInput;
		}
	}

	public void AttackListener(bool debug = false) {
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			FireProjectile(null, debug);
			FireLaser(null, debug);
			if (debug) Debug.Log($"[Player | {name}] Attack input received.");
		}
	}

	#endregion

	#region Projectiles

	[Header("Attacks")]
	public GameObject ProjectilePrefab;

	public void FireProjectile(AttackNote attackNote, bool debug = false) {

		//Instantiate a hitObject.
		GameObject projectile = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
		Projectile script = projectile.GetComponent<Projectile>();

		if (attackNote != null) script.ScoreWeight = attackNote.Weight;

		script.Parent = gameObject;
		script.Fire(AimVector, 1, 1);

		if (debug) Debug.Log($"[Player | {name}] Projectile fired!");
	}

	#endregion

	#region Raycast

	public float LaserRange = 50;

	public float Lifespan = 1;

	public void FireLaser(AttackNote attackNote, bool debug = false) {

		//Perform a raycast scan
		RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, AimVector, LaserRange);

		//For every object hit by the laser beam...
		foreach (RaycastHit2D hit in hits) {

			GameObject hitObject = hit.collider.gameObject;

			//If the object is not this player...
			if (hitObject != gameObject) {
				if (debug) Debug.Log($"[{gameObject.name}] Hit {hitObject.name} at {hit.distance:F2} units.");

				//TODO: Add explosion effect.
				//Explodes where the laser hits
				//GameObject boom = Instantiate(Explosion, hit.point, transform.rotation);
				//boom.GetComponent<Explosion>().ExplosionSize = 3;

				//Spawns the laser sprite between the shooter and the hit object
				//Vector2 deltaGap = Vector2.Lerp(transform.position, hit.centroid, 0.5f);
				//GameObject laser = Instantiate(LaserSprite, deltaGap, transform.rotation);
				//laser.transform.localScale = new Vector3(1, hit.distance, 1);

				//If the object is a player, kill it.
				if (hitObject.GetComponent<Player>() is Player player) player.DoDamage(attackNote, hit);

				break;
			}

			//If the object is this player, ignore it.
			else continue;
		}

		//If the laser beam reaches its max range...
		if (hits.Length == 1) {
			if (debug) Debug.Log($"[{gameObject.name}] Laser beam reached max range.");

			//TODO: Add laser sprite.
			//Spawns the laser sprite between the shooter and the laser's max range.
			//Vector2 deltaGap = Vector2.Lerp(transform.position, transform.position + transform.up * LaserRange, 0.5f);
			//GameObject laser = Instantiate(LaserSprite, deltaGap, transform.rotation);
			//laser.transform.localScale = new Vector3(1, LaserRange, 1);
		}

		//Draws the laser beam in the Scene view for debugging.
		Debug.DrawRay(transform.position, AimVector * LaserRange, Color.red, 5f);
	}

	#endregion

	#region Receiving Damage

	[Header("Damage")]
	public float DamageCooldown = 1;

	public float KnockbackForceX = 15;

	public float KnockbackForceY = 15;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="projectile">
	///		The object that hit the player, usually a projectile.
	/// </param>
	public void DoDamage(Projectile projectile, bool debug = false) {
		Player parent = projectile.Parent.GetComponent<Player>();

		if (debug) Debug.Log($"[Player | {name}] Hit by {parent.name}!");

		if (projectile.Parent == gameObject) {
			if (debug) Debug.Log($"[Player | {name}] Hit by self! Ignoring.");
			return;
		}

		Vector2 hitFrom = (Vector2) transform.position - (Vector2) projectile.transform.position;
		Vector2 knockback = hitFrom.normalized;

		//Get whether the player was hit from the left or right
		if (knockback.x > 0) knockback = new(KnockbackForceX, KnockbackForceY);
		else if (knockback.x < 0) knockback = new(-KnockbackForceX, KnockbackForceY);

		if (debug) Debug.Log($"[Player | {name}] Hit by {parent.name} from {knockback.normalized}. Applying knockback: {knockback}");

		Rigidbody.linearVelocity = Vector2.zero;
		Rigidbody.AddForce(knockback, ForceMode2D.Impulse);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hit">
	///		The hit point object where the player was hit.
	/// </param>
	public void DoDamage(AttackNote note, RaycastHit2D hit, bool debug = true) {
		Vector2 knockback = hit.point;

		//Get whether the player was hit from the left or right
		if (knockback.x > 0) knockback = new(KnockbackForceX, KnockbackForceY);
		else if (knockback.x < 0) knockback = new(-KnockbackForceX, KnockbackForceY);

		if (debug) Debug.Log($"[Player | {name}] Hit from {knockback.normalized}. Applying knockback: {knockback}");

		Rigidbody.linearVelocity = Vector2.zero;
		Rigidbody.AddForce(knockback, ForceMode2D.Impulse);



		if (note != null) Score += note.Weight;
		GameDirector.UpdateScoreRatio();
	}

	#endregion

	#endregion

	#region Gameplay Stats

	[Header("Gameplay Stats")]
	public int ID;
	public float Score;

	#endregion

	#region Game Objects and Components

	[Header("Game Objects and Components")]
	public Rigidbody2D Rigidbody;

	public CapsuleCollider2D Collider;

	public SpriteRenderer Sprite;

	public Controls Controls;

	public GameDirector GameDirector;

	#endregion

	public static Player GetPlayerInstance(int ID, bool debug = false) {
		if (!GameObject.Find("Music Director").TryGetComponent<MusicDirector>(out var director)) {
			Debug.LogError("[Player] Music Director not found!");
			return null;
		}

		Player player;
		//NOTE: Players have an ID attribute, but it's cheaper to just grab the instances from the director.
		switch (ID) {
			case 1:
				player = director.Player1;

				if (player == null) {
					Debug.LogError($"[Player] Player {ID} not assigned in MusicDirector!");
					return null;
				}

				else {
					if (debug) Debug.Log($"[Player] Player {ID} found!");
					return player;
				}

			case 2:
				player = director.Player2;

				if (player == null) {
					Debug.LogError($"[Player] Player {ID} not assigned in MusicDirector!");
					return null;
				}

				else {
					if (debug) Debug.Log($"[Player] Player {ID} found!");
					return player;
				}

			default:
				Debug.LogError($"[Player] Invalid player ID: {ID}");
				return null;
		}
	}

	#region Unity

	public void Update() {
		#region Non-physics Inputs

		AimAttack();

		AttackListener();

		#endregion
	}

	public void FixedUpdate() {

		#region Checks

		if (!isGrounded) {
			//Perform a raycast to check if the player is grounded.
			float colliderBaseY = Collider.size.y / 2;
			float verticalAllowance = colliderBaseY + GroundedDistance;

			RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, verticalAllowance, LayerMask.GetMask("Environment"));

			if (hit.collider != null) {
				isGrounded = true;
				isJumping = false;
			}
		}

		#endregion

		#region Physics Inputs

		JumpListener();

		MoveListener();

		#endregion
	}

	#endregion

	#region Static

	public static Player GetPlayerInstance(int ID, bool debug = false) {
		Player player = null;

		if (!GameObject.Find("Music Director").TryGetComponent<MusicDirector>(out var director)) {
			Debug.LogError("[Player] Music Director not found!");
			return null;
		}

		//NOTE: Players have an ID attribute, but it's cheaper to just grab the instances from the director.
		switch (ID) {
			case 1:
				player = director.Player1;
				break;
			case 2:
				player = director.Player2;
				break;
			default:
				Debug.LogError($"[Player] Invalid player ID: {ID}");
				break;
		}

		if (player == null) Debug.LogError($"[Player] Player {ID} not found!");
		if (debug) Debug.Log($"[Player] Player {ID} found!");

		return player;

		#endregion
	}
}
