using UnityEditor.AnimatedValues;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	#region GameObjects and Components

	[Header("GameObjects and Components")]
	public GameObject Parent;

	private Player ParentScript;

	public Rigidbody2D Body;

	public CircleCollider2D Trigger;

	public CircleCollider2D Collider;

	public TrailRenderer Trail;

	#endregion

	#region Physics

	[Header("Physics")]
	[Range(0f, 50f)]
	public float BaseVelocity = 25;

	[Range(0f, 10f)]
	public float BaseTorque = 5;

	public void Fire(Vector2 direction, float velocityMultiplier = 1, float torqueMultiplier = 1, bool debug = false) {
		Vector2 forceToAdd = BaseVelocity * velocityMultiplier * direction;
		float torqueToAdd = BaseTorque * torqueMultiplier;

		Body.AddForce(forceToAdd, ForceMode2D.Impulse);
		Body.AddTorque(torqueToAdd, ForceMode2D.Impulse);

		if (debug) Debug.Log($"[Projectile] Force: {forceToAdd.magnitude} | Torque: {torqueToAdd}");
	}

	#endregion

	#region Gameplay Stats

	/// <summary>
	///		How much impact hitting or missing this projectile
	///		will have on the player's score.
	/// </summary>
	[Header("Gameplay Stats")]
	public float ScoreWeight;

	public enum ScoreType { Expired, PlayerContact, Environment }

	public void AddScore(ScoreType type, bool debug = false) {
		float score = 0;

		switch (type) {

			//Projectile despawns itself
			case ScoreType.Expired:
				score = ScoreWeight * ExpiredMultiplier;
				if (debug) Debug.Log($"[Projectile] Expired! Score: {score}");
				break;

			//Projectile hits player
			case ScoreType.PlayerContact:
				score = ScoreWeight * PlayerContactMultiplier;
				if (debug) Debug.Log($"[Projectile] Player contact! Score: {score}");
				break;
			
			//Projectile hits environment (usually also subsequently destroyed)
			case ScoreType.Environment:
				score = ScoreWeight * EnvironmentMultiplier;
				if (debug) Debug.Log($"[Projectile] Environment contact! Score: {score}");
				break;
		}

		ParentScript.Score += score;
	}

	#endregion

	#region Miscellaneous

	[Header("Miscellaneous")]
	[Range(0f, 10f)]
	public float Length;

	[Range(0f, 10f)]
	public float Lifespan;

	[Range(-10f, 10f)]
	public float PlayerContactMultiplier = 1;

	[Range(-10f, 10f)]
	public float ExpiredMultiplier = -0.1f;

	[Range(-10f, 10f)]
	public float EnvironmentMultiplier = -0.1f;

	public enum CollisionType { PassThrough, Bounce, DestroyOnImpact }

	//UNUSED. Will be limited to DestroyOnImpact for now.
	public CollisionType playerCollision = CollisionType.DestroyOnImpact;

	public CollisionType environmentCollision = CollisionType.Bounce;

	public void InitializeLongNote(bool debug = false) {
		//Disable trail when firing short notes.
		if (Length <= 0) {
			Trail.enabled = false;
			if (debug) Debug.Log($"[Projectile] Trail length is 0. Disabling trail.");
			return;
		}

		//TODO: Adjust trails n hitboxes n stuff here
	}

	public void DestroySelf(ScoreType type) {
		//TODO: Add particle effect.
		//TODO: Add sound effect.

		AddScore(type);
		Destroy(gameObject);
	}

	#endregion

	#region Unity

	void Start()
    {
		ParentScript = Parent.GetComponent<Player>();
		
		InitializeLongNote();

		//Auto-configure environment collision type.
		bool shallPassThroughEnvironment = environmentCollision == CollisionType.PassThrough;
		bool shallBounceOffEnvironment = environmentCollision == CollisionType.Bounce;
		bool shallImpactWithEnvironment = environmentCollision == CollisionType.DestroyOnImpact;

		if (shallPassThroughEnvironment) Collider.excludeLayers = LayerMask.GetMask("Environment");
		else if (shallBounceOffEnvironment || shallImpactWithEnvironment) Collider.includeLayers = LayerMask.GetMask("Environment");
	}

	//For tracking expiration.
	private void Update() {
		Lifespan -= Time.deltaTime;
		if (Lifespan <= 0) DestroySelf(ScoreType.Expired);
	}

	//For detecting player contact.
	private void OnTriggerEnter2D(Collider2D collision) {
		if (collision.gameObject == Parent) return;

		//If collided with a player...
		if (collision.gameObject.CompareTag("Player")) {
			Player player = collision.gameObject.GetComponent<Player>();

			//Add score and maybe destroy self
			if (playerCollision == CollisionType.DestroyOnImpact) DestroySelf(ScoreType.PlayerContact);
			else AddScore(ScoreType.PlayerContact);

			//Apply effects to player
			player.DoDamage(collision.gameObject);
		}
	}

	//For detecting environment contact.
	private void OnCollisionEnter2D(Collision2D collision) {
		bool isEnvironment = collision.gameObject.CompareTag("Environment");
		bool destroyOnImpact = environmentCollision == CollisionType.DestroyOnImpact;
		if (isEnvironment && destroyOnImpact) DestroySelf(ScoreType.Environment);
	}

	#endregion
}
