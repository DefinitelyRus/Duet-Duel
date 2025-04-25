using UnityEngine;

public class Player : MonoBehaviour {

	#region Physics

	#region Horizontal Movement

	[Header("Horizontal Movement")]
	public float Acceleration;

	public float MaxVelocity;

	public void MoveListener(bool debug = false) {
		bool leftInput = Input.GetKey(KeyCode.A);
		bool rightInput = Input.GetKey(KeyCode.D);
		if (!leftInput && !rightInput) return;

		string log = "";
		if (debug) {
			if (leftInput) log += "L";
			if (rightInput) log += "R";
		}

		//Applies the force to the player.
		float multiplier = Acceleration * Time.fixedDeltaTime;
		Vector2 leftVelocity = (leftInput ? 1 : 0) * multiplier * Vector2.left;
		Vector2 rightVelocity = (rightInput ? 1 : 0) * multiplier * Vector2.right;
		Vector2 velocity = leftVelocity + rightVelocity;
		Rigidbody.AddForce(velocity, ForceMode2D.Force);

		//Clamps the speed and logs it if debug is enabled.
		if (debug) {
			log += $" Velocity: {Rigidbody.linearVelocityX}";
			bool isClamped = Rigidbody.linearVelocityX > MaxVelocity || Rigidbody.linearVelocityX < -MaxVelocity;

			Rigidbody.linearVelocityX = Mathf.Clamp(Rigidbody.linearVelocityX, -MaxVelocity, MaxVelocity);

			if (isClamped) Debug.Log($"{log} (Clamped to {Rigidbody.linearVelocityX})");
			else Debug.Log(log);
		}

		//Clamps the speed without logging it.
		else Rigidbody.linearVelocityX = Mathf.Clamp(Rigidbody.linearVelocityX, -MaxVelocity, MaxVelocity);
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
		if (Input.GetKeyDown(KeyCode.Space)) QueueJump(debug);

		//TODO: Allow coyote time
		bool doGroundedJump = Input.GetKey(KeyCode.Space) && isGrounded;
		bool jumpOnImpact = isJumpQueued && isGrounded && !isJumping; //Input buffering
		if (doGroundedJump || jumpOnImpact) Jump(debug);

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
			//Use InputSystemPackage 
			//AimVector = new Vector2(Gamepad.rightStick.x.ReadValue(), Gamepad.rightStick.y.ReadValue()).normalized;
			//TODO: Handle input modes via a centralized class.
		}
	}

	public void AttackListener(bool debug = false) {
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			FireProjectile(debug);
			FireLaser(debug);
			if (debug) Debug.Log($"[Player | {name}] Attack input received.");
		}
	}

	#endregion

	#region Projectiles

	[Header("Attacks")]
	public GameObject ProjectilePrefab;

	public void FireProjectile(bool debug = false) {
		//Instantiate a hitObject.
		GameObject projectile = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
		Projectile script = projectile.GetComponent<Projectile>();
		script.Parent = gameObject;
		script.Fire(AimVector, 1, 1);

		if (debug) Debug.Log($"[Player | {name}] Projectile fired!");
	}

	#endregion

	#region Raycast

	public float LaserRange = 50;

	public float Lifespan = 1;

	public void FireLaser(bool debug = false) {

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
				if (hitObject.GetComponent<Player>() is Player player) player.DoDamage(hit);

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

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hitObject">
	///		The object that hit the player, usually a projectile.
	/// </param>
	public void DoDamage(GameObject hitObject) {
		//TODO: Implement damage logic.
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="hit">
	///		The hit point object where the player was hit.
	/// </param>
	public void DoDamage(RaycastHit2D hit) {
		//TODO: Implement damage logic.
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

	#endregion

	#region Inputs

	//TODO: Receive input keys from a centralized class.

	#endregion

	public static Player GetPlayerInstance(int ID, bool debug = false) {
		Player player = null;

		MusicDirector director = GameObject.Find("Music Director").GetComponent<MusicDirector>();

		if (director == null) {
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
}
