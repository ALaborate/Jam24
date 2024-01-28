using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Android;


public class NetworkCharacterController : NetworkBehaviour
{

    private const RigidbodyConstraints RB_ROT_CONSTR = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    public Transform hand;
    public ParticleSystem ticklingParticles;
    public TrailRenderer pushingTrail;
    [Space]
    public float moveAcceleration = 556;
    public float maxRunningSpeed = 10;
    public float jumpForce = 10f;
    public float roflJumpForce = 1;
    public float roflRandomTorqueMultiplier = 3.14f;
    [Space]
    public float camRotationSpeed = 1f;
    public float bodyYRotationTorque = 1f;
    public float minCamAngle = -30;
    public float maxCamAngle = 60;
    public Vector3 camOffset = new Vector3(1, 0, 0);
    [Space]
    public float maxFloatingForce = 500;
    public AnimationCurve floatingForceCurve = AnimationCurve.Linear(0.5f, 1, 1.5f, 0);
    public float floatingForceReductionDenominator = 10;
    public float groundCastDistance = 1.1f;
    public LayerMask groundLayer = Physics.DefaultRaycastLayers;
    [Header("Interactions")]
    public float maxVelocityDamage = .8f;
    public float pushMaxForce = 600f;
    public float pushRadius = 2f;
    public float pushCooldown = 1f;
    [Space]
    public float ticklingGainCoef = 2f;
    public float ticklingCooling = 1.5f;
    public float ticklingRadius = 3.3f;
    public float ticklingDamage = 0.7f;
    [Header("Visual")]
    public float tpMinEmision = 3;
    public float tpMaxEmision = 11;
    public float tpVisualThreshold = 0.4f;
    public float pushVisualizationMvtDuration = .5f;



    private Rigidbody rb;
    private CapsuleCollider col;
    private Camera cam;
    private PlayerHealth health;
    private HealthVisualizer healthVisualizer;
    private float height;


    public bool HasFeather => inventoryIds.Count > 0;

    public UnityEngine.Events.UnityEvent<float> OnCollision = new();

    public void TakeRandomDamage(float ammount, uint srcNetId)
    {
        health.TakeDamage(ammount, srcNetId);
    }

    private void Awake()
    {

    }
    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        foreach (var item in inventoryIds)
        {
            OnInventoryChange(SyncSet<uint>.Operation.OP_ADD, item);
        }
        inventoryIds.Callback += OnInventoryChange;
    }

    private void Initialize()
    {
        if (col == null)
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponentInChildren<CapsuleCollider>();
            health = GetComponent<PlayerHealth>();
            height = col.center.y + col.height / 2;
            healthVisualizer = GetComponent<HealthVisualizer>();

            if (isServer)
            {
                health.OnRofl.AddListener(OnRofl);
                health.OnRoflOver.AddListener(OnRoflOver);
            }

            DisableControlAndCamera();
            pushingTrail.gameObject.SetActive(false);

            if (isClient) //pick up all objects picked earlier than connectioxn
            {
                foreach (var item in inventoryIds)
                {
                    //OnInventoryChange(SyncSortedSet<NetworkIdentity>.Operation.OP_ADD, item);
                }
            }

            EventManager.Instance.AddPlayer(this);
        }
    }
    private void DisableControlAndCamera()
    {
        if (!isServer)
        {
            Destroy(rb);
        }
        else
        {
            rb.constraints = RB_ROT_CONSTR;
        }
        if (isLocalPlayer)
        {
            cam = Camera.main;
        }
    }


    private float targetLookAngleY = 0f;
    private float ticklingIntensity = 0f;

    [SyncVar]
    private float ticklingIntensityVisual = 0f;
    private void Update()
    {
        if (isLocalPlayer)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            var mouseSpeed = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")).magnitude;
            ticklingIntensity -= Time.deltaTime * ticklingCooling;
            ticklingIntensity += mouseSpeed * ticklingGainCoef;
            ticklingIntensity = Mathf.Clamp01(ticklingIntensity);

            if (Input.GetKey(KeyCode.Space))
                userInput = userInput | UserInput.Jump;
            else
                userInput = userInput & ~UserInput.Jump;

            if (Input.GetAxisRaw("Fire1") > 0)
                userInput = userInput | UserInput.Push;
            else
                userInput = userInput & ~UserInput.Push;

            CmdMove(vertical, horizontal, targetLookAngleY, ticklingIntensity, userInput);
        }


        if (isClient)
        {
            ShowTicklingParticles();
        }
    }

    private void ShowTicklingParticles()
    {
        var em = ticklingParticles.emission;
        em.rateOverTime = Mathf.Lerp(tpMinEmision, tpMaxEmision, ticklingIntensityVisual);

        if (!ticklingParticles.isPlaying && ticklingIntensityVisual > tpVisualThreshold)
            ticklingParticles.Play();
        if (ticklingParticles.isPlaying && ticklingIntensityVisual == 0)
            ticklingParticles.Stop();
    }


    private double lastCmdMoveTime = 0;
    private RaycastHit[] groundHits;
    /// <summary>
    /// Distance between the ground and body center (not equal to center of collider)
    /// </summary>
    [SerializeField][ReadOnly] private float minGroundDistance = float.MaxValue;
    [SerializeField][ReadOnly] private float footGrip = 0f;
    [SerializeField][ReadOnly] private bool isGrounded;
    [Command]
    private void CmdMove(float run, float strafe, float targetRotationY, float ticklingIntensity, UserInput incoming)
    {
        var dt = NetworkTime.time - lastCmdMoveTime;
        lastCmdMoveTime = NetworkTime.time;
        Vector3 moveDirection = new Vector3(strafe, 0, run);
        moveDirection = transform.TransformDirection(moveDirection);
        var groundVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        var speed = groundVelocity.magnitude;
        var forceSpeedReduction = Mathf.Clamp01(1 - speed / maxRunningSpeed);
        forceSpeedReduction = Mathf.Lerp(1, forceSpeedReduction, Vector3.Dot(groundVelocity.normalized, moveDirection.normalized));
        forceSpeedReduction *= health.IsRofled ? 0.34f : 1;
        rb.AddForce(moveDirection * moveAcceleration * (float)dt * forceSpeedReduction * footGrip, ForceMode.Acceleration);

        if (incoming.HasFlag(UserInput.Jump))
            userInput = userInput | UserInput.Jump;

        targetRotation = new Vector3(0, targetRotationY);

        if (HasFeather)
            this.ticklingIntensity = ticklingIntensity;
        else
            this.ticklingIntensity = 0;

        if (strafe != 0 || run != 0)
            userInput = userInput | UserInput.ReceivedUserInput;
        else
            userInput = userInput & ~UserInput.ReceivedUserInput;

        if (incoming.HasFlag(UserInput.Push))
            userInput = userInput | UserInput.Push;
        else
            userInput = userInput & ~UserInput.Push;
    }

    [System.Flags]
    private enum UserInput
    {
        None = 0,
        ReceivedUserInput = 1 << 0,
        Jump = 1 << 1,
        Push = 1 << 2,
    }



    UserInput userInput = UserInput.None;
    private Vector3 targetRotation;
    private float nextTimeToPush = 0;
    private void FixedUpdate()
    {
        if (rb != null)
        {//server
            const int GROUND_RAY_COUNT = 4;
            if (groundHits == null) groundHits = new RaycastHit[GROUND_RAY_COUNT];

            minGroundDistance = float.MaxValue;
            List<GameObject> toRemove = null;
            foreach (var item in groundCollisionHashes)
            {
                if (item == null || item.gameObject == null)
                {
                    if (toRemove == null) toRemove = new();
                    toRemove.Add(item);
                }
            }
            if (toRemove != null) foreach (var item in toRemove) groundCollisionHashes.Remove(item);
            if (groundCollisionHashes.Count > 0)
            {
                minGroundDistance = col.height / 2 - col.center.y;
            }
            for (int i = 0; i < groundHits.Length; i++)
            {
                var ray = new Ray(transform.position + Quaternion.Euler(0, i * (360 / groundHits.Length), 0) * (Vector3.forward * col.radius), Vector3.down);
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * groundCastDistance, Color.green, 0f, true);
                var hit = Physics.Raycast(ray, out groundHits[i], groundCastDistance, groundLayer);
                if (!hit) groundHits[i].distance = float.PositiveInfinity;
                if (groundHits[i].distance < minGroundDistance)
                    minGroundDistance = groundHits[i].distance;
                if (hit)
                    footGrip = (footGrip * i + groundHits[i].collider.material.dynamicFriction) / (i + 1);
            }
            isGrounded = minGroundDistance <= 1;

            var jump = userInput.HasFlag(UserInput.Jump);
            userInput = userInput & ~UserInput.Jump;
            if ((isGrounded || health.IsRofled) && rb.velocity.y < 0.1f && jump && rb.velocity.sqrMagnitude < maxRunningSpeed * maxRunningSpeed * 10)
            {
                // Add an upward force to the rigidbody to make the character jump
                rb.AddForce(transform.up * (health.IsRofled ? roflJumpForce : jumpForce), ForceMode.VelocityChange);
                //rb.AddTorque(Vector3.one * jumpForce, ForceMode.VelocityChange);
            }

            AddFloatingForce();

            ApplyStoppingForce();

            RotateToCameraDirection();

            PushOpponents();

            TickleOpponents();

            float roflVelocitySqrThreshold = 4;
            if (health.IsRofled && rb.angularVelocity.sqrMagnitude < roflVelocitySqrThreshold * roflRandomTorqueMultiplier)
                rb.AddTorque(Random.onUnitSphere * roflRandomTorqueMultiplier, ForceMode.VelocityChange);


            if (transform.position.y < -100) // respawn
            {
                transform.position = Vector3.up * 10;
            }
        }
    }

    private void RotateToCameraDirection()
    {
        if (health.IsRofled) return;
        Vector3 torqueVector = Vector3.zero;
        ///this vertical stabilization does not work well in conjunction with Y rotation. That sucks.
        //var predictedUp = Quaternion.AngleAxis(
        //     rb.angularVelocity.magnitude * Mathf.Rad2Deg * bodyStability / bodyStabilizationSpeed,
        //     rb.angularVelocity) * transform.up;
        //torqueVector = Vector3.Cross(predictedUp, Vector3.up);
        //if (torqueVector.sqrMagnitude > 0.001f)
        //{
        //    rb.AddTorque(torqueVector * bodyStabilizationSpeed * bodyStabilizationSpeed, ForceMode.Acceleration);
        //}
        //else

        {
            var yVelocity = rb.angularVelocity.y * Mathf.Rad2Deg;
            var yDelta = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetRotation.y);
            var breakingDistance = yVelocity * yVelocity / (2 * bodyYRotationTorque);
            if (Mathf.Abs(yDelta) < 2 * bodyYRotationTorque * Time.fixedDeltaTime && Mathf.Abs(yVelocity) < 2 * bodyYRotationTorque * Time.fixedDeltaTime)
            {
                rb.angularVelocity = new Vector3(rb.angularVelocity.x, 0, rb.angularVelocity.z);
                rb.rotation = Quaternion.Euler(rb.rotation.eulerAngles.x, targetRotation.y, rb.rotation.eulerAngles.z);
            }
            else if (Mathf.Abs(yDelta) > breakingDistance)
            {
                torqueVector.y = bodyYRotationTorque * Mathf.Sign(yDelta);
                //torqueVector.y *= Mathf.Clamp01(Mathf.Abs(yDelta) / bodyYRotationSpeed * Time.fixedDeltaTime * Time.fixedDeltaTime * 0.5f); //dont speed up more than necessary
            }
            else
            {
                torqueVector.y = -Mathf.Min(Mathf.Abs(yVelocity) / Time.fixedDeltaTime, bodyYRotationTorque) * Mathf.Sign(yVelocity);
            }
            rb.AddTorque(torqueVector * Time.fixedDeltaTime, ForceMode.Acceleration);
        }
    }

    private void ApplyStoppingForce()
    {
        var receivedUserInput = userInput.HasFlag(UserInput.ReceivedUserInput);
        if (!receivedUserInput && !health.IsRofled)
        {
            //apply counterforce
            var moveDirection = new Vector3(-rb.velocity.x, 0, -rb.velocity.z);
            rb.AddForce(moveDirection.normalized * moveAcceleration * Time.fixedDeltaTime * footGrip, ForceMode.Acceleration);
        }
    }

    private void AddFloatingForce()
    {
        if (health.IsRofled)
            return;
        var floatingForceValue = floatingForceCurve.Evaluate(minGroundDistance) * maxFloatingForce;
        if (rb.velocity.y > 0)
            floatingForceValue *= Mathf.Clamp01(1 - rb.velocity.y / floatingForceReductionDenominator);
        rb.AddForce(Vector3.up * floatingForceValue * Time.fixedDeltaTime, ForceMode.Acceleration);
    }

    private void PushOpponents()
    {
        var push = userInput.HasFlag(UserInput.Push);
        userInput = userInput & ~UserInput.Push;

        if (isServer && push && Time.fixedTime >= nextTimeToPush)
        {
            nextTimeToPush = Time.fixedTime + pushCooldown;
            var ray = new Ray(transform.position, transform.forward);
            RaycastHit[] hits;
            RpcVisualizePush();
            if (!health.IsRofled)
                hits = Physics.RaycastAll(ray, pushRadius);
            else
                hits = Physics.SphereCastAll(ray, pushRadius, pushRadius * 0.3f);
            foreach (var rhi in hits)
            {
                if (rhi.rigidbody != null)
                {
                    var direction = (rhi.transform.position - transform.position).normalized;
                    var force = direction * pushMaxForce;
                    rhi.rigidbody.AddForce(force, ForceMode.Impulse);
                    var ph = rhi.rigidbody.GetComponent<PlayerHealth>();
                    ph?.SetSourceOfDamage(netId);
                }
            }

        }
    }
    [ClientRpc]
    private void RpcVisualizePush()
    {
        StartCoroutine(VisualizePush());
    }
    [Client]
    private IEnumerator VisualizePush()
    {
        pushingTrail.transform.SetParent(null);
        pushingTrail.transform.position = transform.position + Vector3.left * col.radius * .8f;
        pushingTrail.startColor = healthVisualizer.CurrentColor;
        pushingTrail.Clear();
        pushingTrail.gameObject.SetActive(true);
        var t0 = Time.time;
        while (Time.time - t0 < pushVisualizationMvtDuration)
        {
            yield return null;
            var distance = Mathf.Lerp(0, pushRadius, (Time.time - t0) / pushVisualizationMvtDuration);
            if (health.IsRofled)
            {
                var angle = Mathf.Lerp(0, 360 * 3, (Time.time - t0) / pushVisualizationMvtDuration);
                pushingTrail.transform.position = transform.position + Quaternion.Euler(0, angle % 360, 0) * Vector3.forward * distance;
            }
            else
            {
                pushingTrail.transform.position = transform.position + transform.forward * distance;
            }
        }
        yield return new WaitForSeconds(pushCooldown - pushVisualizationMvtDuration);
        pushingTrail.gameObject.SetActive(false);
    }

    private void TickleOpponents()
    {
        ticklingIntensityVisual = ticklingIntensity;
        var distance = Mathf.Lerp(ticklingRadius * .5f, ticklingRadius, ticklingIntensity);
        if (ticklingIntensity == 0 || !HasFeather) return;

        var hits = Physics.OverlapSphere(transform.position + transform.forward * 0.5f * distance, distance * 0.5f);
        foreach (var hit in hits)
        {
            var other = hit.attachedRigidbody?.GetComponent<PlayerHealth>();
            if (other != null && other.gameObject.GetInstanceID() != this.gameObject.GetInstanceID())
            {
                other.TakeDamage(ticklingIntensity * ticklingDamage * Time.fixedDeltaTime, netId);
            }
        }
    }



    private SortedSet<GameObject> groundCollisionHashes = new();
    private readonly SyncHashSet<uint> inventoryIds = new();
    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            if (IsColisionWithGround(collision))
            {
                try
                {
                    groundCollisionHashes.Add(collision.gameObject);
                }
                catch (System.ArgumentException)
                {
                    //For an exception "At least one object must implement IComparable"
                }
            }


            if (!health.IsRofled && TryPick(collision.rigidbody?.gameObject ?? collision.gameObject))
            {
                //do nothing
            }
            else
            {
                CalculateSelfDamage(collision);
            }
        }
    }

    ///<param name="rbGo">Game object on which rigidbody sits, if present. Otherwise, collision gameobject</param>
    private bool TryPick(GameObject rbGo)
    {
        if (rbGo == null || !isServer) return false;

        var pickable = rbGo.GetComponent<IPickable>();
        var pickableNetId = rbGo.GetComponent<NetworkIdentity>();
        var collectable = rbGo.GetComponent<ICollectable>();

        if (pickable != null || collectable != null)
        {
            if (pickable != null && pickableNetId != null)
            {
                inventoryIds.Add(pickableNetId.netId); ///for actual picking code <see cref="OnInventoryChange(SyncSet{uint}.Operation, uint)"/>
            }
            else if (collectable != null)
            {
                collectable.Collect(netId);
            }
            return true;
        }
        return false;
    }

    private void OnInventoryChange(SyncSortedSet<uint>.Operation op, uint itemNetId)
    {
        StartCoroutine(OnInventoryChangeDelayed(op, itemNetId));

    }

    private IEnumerator OnInventoryChangeDelayed(SyncSet<uint>.Operation op, uint itemNetId)
    {
        if (op == SyncSet<uint>.Operation.OP_CLEAR)
        {
            for (int i = hand.childCount - 1; i >= 0; i--)
            {
                var pickable = hand.GetChild(i).GetComponent<IPickable>();
                pickable?.Drop(gameObject);
            }
        }
        else
        {
            NetworkIdentity item = null;
            const float DELAY = .5f;
            const int TRIES = 4;
            YieldInstruction delay = new WaitForEndOfFrame();
            var spawned = isServerOnly ? NetworkServer.spawned : NetworkClient.spawned;
            for (int i = 0; i < TRIES; i++)
            {
                if (spawned.ContainsKey(itemNetId))
                {
                    item = spawned[itemNetId];
                    break;
                }
                else
                {
                    yield return delay;
                    if (i == 0)
                        delay = new WaitForSeconds(DELAY);
                }
            }

            if (item == null)
            {
                Debug.LogError($"Networked item with ID {itemNetId} not found for {DELAY * TRIES} seconds, {nameof(OnInventoryChangeDelayed)} fails.");
                yield break;
            }

            if (op == SyncSortedSet<uint>.Operation.OP_ADD)
            {
                var pickable = item.GetComponent<IPickable>();
                pickable?.PickUp(gameObject, hand);
            }
            else if (op == SyncSortedSet<uint>.Operation.OP_REMOVE)
            {
                var pickable = item.GetComponent<IPickable>();
                pickable.Drop(gameObject);
            }
        }
    }

    private void CalculateSelfDamage(Collision collision)
    {
        float asymmetryCoef = 1;
        //float averageHeight = 0;
        //Vector3 averageContactPoint = Vector3.zero;
        //for (int i = 0; i < collision.contacts.Length; i++)
        //{
        //    var lp = transform.InverseTransformPoint(collision.contacts[i].point);
        //    averageHeight = (averageHeight * i + lp.y) / (i + 1);
        //    averageContactPoint = (averageContactPoint * i + collision.contacts[i].point) / (i + 1);
        //}
        //asymmetryCoef = Mathf.Lerp(1, maxAsymmetryMultiplier, Mathf.Abs(averageHeight / height));


        var mag = Vector3.Dot(collision.contacts[0].normal.normalized, collision.relativeVelocity.normalized) * collision.relativeVelocity.magnitude;
        var fallDamageReductionCoef = Mathf.Lerp(1, .05f, Mathf.Clamp01(Vector3.Dot(Vector3.up, collision.contacts[0].normal)));
        var damageReduction = health.IsRofled ? 0.1f : 1;
        //roflDamageReductionCoef *= hand.childCount > 0 && collision.rigidbody?.GetComponent<NetworkCharacterController>() != null ? 0.3f : 1;
        var otherPlayer = (collision.rigidbody?.gameObject ?? collision.gameObject).GetComponent<NetworkCharacterController>();
        if (otherPlayer != null)
        {
            damageReduction *= 0f; //no damage if no feather
        }
        else
        {
            damageReduction *= 1f; //full damage if wall
        }
        var damage = (mag / maxRunningSpeed) * maxVelocityDamage * asymmetryCoef * fallDamageReductionCoef * damageReduction;
        health.TakeDamage(Mathf.Clamp01(damage), otherPlayer == null ? PlayerHealth.INVALID_SRC : otherPlayer.netId);
        RpcOnCollision(mag / maxRunningSpeed);
    }
    [ClientRpc]
    ///<param name="severity">0 - no damage, 1 - collision on maximum speed, >1 - collision with more relative velocity than max speed</param>
    private void RpcOnCollision(float severity)
    {
        OnCollision?.Invoke(severity);
    }



    private void OnCollisionExit(Collision collision)
    {
        if (isServer)
        {
            try
            {
                groundCollisionHashes.Remove(collision.gameObject);
            }
            catch (System.ArgumentException)
            {
                //For an exception "At least one object must implement IComparable"
            }
        }
    }
    private bool IsColisionWithGround(Collision collision)
    {
        var isGroundLayer = ((1 << collision.gameObject.layer) & groundLayer) > 0;
        var averageColinearity = 0f;
        if (isGroundLayer)
            for (int i = 0; i < collision.contactCount; i++)
            {
                var colinearity = Vector3.Dot(collision.contacts[i].normal, Vector3.up);
                averageColinearity = (averageColinearity * i + colinearity) / (i + 1);
            }
        return averageColinearity > 0.4f;
    }


    GUIStyle scoreLabelGuiStyle;
    Rect scoreLabelRect;
    private void OnGUI()
    {
        if (cam != null)
        {
            if (scoreLabelGuiStyle == null)
            {
                scoreLabelGuiStyle = new GUIStyle();
                scoreLabelGuiStyle.fontSize = 24;
                //scoreLabelGuiStyle.font = Resources.Load<Font>("Fonts/Roboto-Regular");
                scoreLabelRect = new Rect(cam.pixelWidth - 160, 20, 120, 20);
            }
            GUI.Label(scoreLabelRect, $"Score : {EventManager.GetScore(netId):f1}", scoreLabelGuiStyle);

        }
    }

    private void OnRofl()
    {
        if (!isServer) return;

        inventoryIds.Clear();

        rb.constraints = RigidbodyConstraints.None;
    }

    private void OnRoflOver()
    {
        if (!isServer) return;

        //rb.AddTorque(-rb.angularVelocity * Mathf.Rad2Deg, ForceMode.VelocityChange);
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RB_ROT_CONSTR;
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }

    private void OnDisable()
    {
        if (isServer)
        {
            OnInventoryChange(SyncSet<uint>.Operation.OP_CLEAR, 0);
        }
        EventManager.Instance.RemovePlayer(this);
    }


    private void LateUpdate()
    {
        RotateCamera();

    }

    private void RotateCamera()
    {
        if (cam == null)
            return;
        var container = cam.transform.parent;
        container.position = transform.position + container.TransformDirection(camOffset);
        var rot = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * (camRotationSpeed);
        var newEuler = container.rotation.eulerAngles + rot;
        newEuler.z = 0;
        while (newEuler.x > 180)
            newEuler.x -= 360;
        newEuler.x = Mathf.Clamp(newEuler.x, minCamAngle, maxCamAngle);
        while (newEuler.x < 0)
            newEuler.x += 360;
        container.rotation = Quaternion.Euler(newEuler);

        //container.localRotation = ClampRotationAroundXAxis(container.localRotation);
        targetLookAngleY = container.rotation.eulerAngles.y;
    }
}
