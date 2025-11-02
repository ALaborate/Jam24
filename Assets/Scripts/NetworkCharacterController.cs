using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Profiling;

public class NetworkCharacterController : NetworkBehaviour
{
    private const RigidbodyConstraints RB_ROT_CONSTR = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    [System.NonSerialized]
    public string nickname = "JamPlayer98";
    [SerializeField] Transform hand;
    [SerializeField] ParticleSystem ticklingParticles;
    [SerializeField] TrailRenderer pushingTrail;
    [Space]
    [SerializeField] float moveAcceleration = 556;
    [SerializeField] float maxRunningSpeed = 10;
    [SerializeField] float jumpForce = 10f;
    [SerializeField] float roflJumpForce = 1;
    [SerializeField] float roflRandomTorqueMultiplier = 3.14f;
    [Space]
    [SerializeField] float camRotationSpeed = 1f;
    [SerializeField] float bodyYRotationTorque = 1f;
    [SerializeField] float touchRotationSensitivity = 5f;
    [SerializeField] float bodyRotationDrag = 10;
    [SerializeField] float bodyRoflDrag = 0;
    [SerializeField] float minCamAngle = -30;
    [SerializeField] float maxCamAngle = 60;
    [SerializeField] Vector3 camOffset = new Vector3(1, 0, 0);
    [Space]
    [SerializeField] float maxFloatingForce = 500;
    [SerializeField] AnimationCurve floatingForceCurve = AnimationCurve.Linear(0.5f, 1, 1.5f, 0);
    [SerializeField] float floatingForceReductionDenominator = 10;
    [SerializeField] float groundCastDistance = 1.1f;
    [SerializeField] LayerMask groundLayer = Physics.DefaultRaycastLayers;
    [Header("Interactions")]
    [SerializeField] float maxVelocityDamage = .8f;
    public float pushMaxForce = 600f;
    [SerializeField] float pushRadius = 2f;
    [SerializeField] float pushCooldown = 1f;
    [Space]
    [SerializeField] float ticklingGainCoef = 2f;
    [SerializeField] float ticklingCooling = 1.5f;
    [SerializeField] float ticklingRadius = 3.3f;
    [SerializeField] float ticklingDamage = 0.7f;
    [Header("Visual")]
    [SerializeField] float tpMinEmision = 3;
    [SerializeField] float tpMaxEmision = 11;
    [SerializeField] float tpVisualThreshold = 0.4f;
    [SerializeField] float pushVisualizationMvtDuration = .5f;



    private Rigidbody rb;
    private CapsuleCollider col;
    private Camera cam;
    private PlayerHealth health;
    private HealthVisualizer healthVisualizer;
    private float height;
    private JoysticVisualizer JoysticVisualizer => Bootstrap.Instance.joysticVisualizer;


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
        inventoryIds.OnChange += OnInventoryChange;
        Bootstrap.Instance.accelShake.OnShake.AddListener(delta => accelJump = true);
        nickname = Bootstrap.Instance.playerNameField.text;
        
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
            isTouchPresent = false;
            rb.angularDamping = bodyRotationDrag;

            if (isServer)
            {
                health.OnRofl.AddListener(OnRofl);
                health.OnRoflOver.AddListener(OnRoflOver);
            }

            if (isServer)
                rb.constraints = RB_ROT_CONSTR;

            if (isLocalPlayer)
                cam = Camera.main;

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


    private float targetLookAngleY = 0f;
    private float ticklingIntensity = 0f;
    private Vector2 touchMvtInitialPos = Vector2.zero;

    [SyncVar]
    private float ticklingIntensityVisual = 0f;
    bool isTouchPresent;
    float horizontal;
    float vertical;
    bool accelJump = false; ///jump from accelerometer
    private void Update()
    {
        if (isLocalPlayer)
        {
            var mouseSpeed = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")).magnitude;
            ticklingIntensity -= Time.deltaTime * ticklingCooling;
            if (!isTouchPresent)
                ticklingIntensity += mouseSpeed * ticklingGainCoef;


            if (!isTouchPresent)
            {
                if (Input.GetAxisRaw("Fire1") > 0)
                    userInput = userInput | UserInput.Push;
                else
                    userInput = userInput & ~UserInput.Push;
            }
            else
                userInput = userInput & ~UserInput.Push; //see raising of push below in iteration over touches

            if (Input.GetKey(KeyCode.Space))
                userInput = userInput | UserInput.Jump;
            else
                userInput = userInput & ~UserInput.Jump;

            if (accelJump)
            {
                userInput = userInput | UserInput.Jump;
                accelJump = false;
            }

            var touchMvt = Vector2.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                isTouchPresent = true;
                var t = Input.GetTouch(i);
                if (t.position.x < Screen.width / 2)
                {
                    var prevInput = new Vector2(horizontal, vertical); //rudimentary, just in case

                    touchMvt = t.position - touchMvtInitialPos;

                    if (t.phase == TouchPhase.Began && t.tapCount != 2)
                    {
                        touchMvtInitialPos = t.position;
                    }
                    if (health.IsRofled)
                        userInput |= UserInput.Jump; //any touch considered jump in rofled state
                }
                else
                {
                    if (t.phase == TouchPhase.Began && t.tapCount > 1)
                        userInput |= UserInput.Push;
                    ticklingIntensity += t.deltaPosition.magnitude * ticklingGainCoef;
                }
            }

            const float TOUCH_SENS_BOOST = 3.7f;
            if (isTouchPresent)
            {
                vertical = TOUCH_SENS_BOOST * touchMvt.y / cam.pixelHeight;
                horizontal = 2 * TOUCH_SENS_BOOST * touchMvt.x / cam.pixelWidth;
            }
            else
            {
                horizontal = Input.GetAxis("Horizontal");
                vertical = Input.GetAxis("Vertical");
            }

            JoysticVisualizer.direction = new Vector2(horizontal, vertical);

            ticklingIntensity = Mathf.Clamp01(ticklingIntensity);
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
        Profiler.BeginSample(nameof(CmdMove));
        var dt = NetworkTime.time - lastCmdMoveTime;
        lastCmdMoveTime = NetworkTime.time;
        Vector3 moveDirection = new Vector3(strafe, 0, run);
        moveDirection.Normalize();
        moveDirection = transform.TransformDirection(moveDirection);
        var groundVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
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

        Profiler.EndSample();
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
        if (isServer)
        {
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
                if (groundHits[i].distance < minGroundDistance && !groundHits[i].collider.isTrigger)
                    minGroundDistance = groundHits[i].distance;
                if (hit)
                    footGrip = (footGrip * i + groundHits[i].collider.material.dynamicFriction) / (i + 1);
            }
            isGrounded = minGroundDistance <= 1;

            var jump = userInput.HasFlag(UserInput.Jump);
            userInput = userInput & ~UserInput.Jump;
            if ((isGrounded || health.IsRofled) && rb.linearVelocity.y < 0.1f && jump && rb.linearVelocity.sqrMagnitude < maxRunningSpeed * maxRunningSpeed * 10)
            {
                // Add an upward force to the rigidbody to make the character jump
                rb.AddForce(transform.up * (health.IsRofled ? roflJumpForce : jumpForce), ForceMode.VelocityChange);
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

    public bool rotate = true;
    ///<summary>Saving rb linear velocity between rofled and unrofled state (while discarding angular)</summary>
    Vector3 tempRbVelocity = Vector3.zero;

    [Server]
    private void RotateToCameraDirection()
    {
        if (!health.IsRofled)
        {
            Vector3 torqueVector = Vector3.zero;

            if (rotate)
            {
                if (rb.isKinematic)
                {
                    rb.isKinematic = false;
                    rb.AddForce(tempRbVelocity, ForceMode.VelocityChange);
                }

                var yDelta = Mathf.DeltaAngle(rb.rotation.eulerAngles.y, targetRotation.y) * Mathf.Deg2Rad;
                torqueVector.y = yDelta * bodyYRotationTorque;
                rb.AddTorque(torqueVector);
            }
        }
    }

    private void ApplyStoppingForce()
    {
        var receivedUserInput = userInput.HasFlag(UserInput.ReceivedUserInput);
        if (!receivedUserInput && !health.IsRofled)
        {
            //apply counterforce
            var moveDirection = new Vector3(-rb.linearVelocity.x, 0, -rb.linearVelocity.z);
            rb.AddForce(moveDirection.normalized * moveAcceleration * Time.fixedDeltaTime * footGrip, ForceMode.Acceleration);
        }
    }

    private void AddFloatingForce()
    {
        if (health.IsRofled)
            return;
        Profiler.BeginSample(nameof(AddFloatingForce));
        var floatingForceValue = floatingForceCurve.Evaluate(minGroundDistance) * maxFloatingForce;
        if (rb.linearVelocity.y > 0)
            floatingForceValue *= Mathf.Clamp01(1 - rb.linearVelocity.y / floatingForceReductionDenominator);
        rb.AddForce(Vector3.up * floatingForceValue * Time.fixedDeltaTime, ForceMode.Acceleration);
        Profiler.EndSample();
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
    private float PUSH_RIGHT_DISPLACEMENT = .95f;
    [Client]
    private IEnumerator VisualizePush()
    {
        pushingTrail.transform.SetParent(null);
        pushingTrail.transform.position = transform.position + (health.IsRofled ? Vector3.zero : transform.right * col.radius * PUSH_RIGHT_DISPLACEMENT);
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
                pushingTrail.transform.position = transform.position + transform.right * col.radius * PUSH_RIGHT_DISPLACEMENT + transform.forward * distance;
            }
        }
        yield return new WaitForSeconds(pushCooldown - pushVisualizationMvtDuration);
        pushingTrail.gameObject.SetActive(false);
    }

    [Server]
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
        isGroundLayer = isGroundLayer && !collision.collider.isTrigger;
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
            if (scoreLabelGuiStyle == null || Mathf.Abs(scoreLabelRect.x - cam.pixelWidth) > 164)
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
        rb.angularDamping = bodyRoflDrag;
    }

    private void OnRoflOver()
    {
        if (!isServer) return;


        tempRbVelocity = rb.linearVelocity;
        rb.isKinematic = true;

        rb.angularVelocity = Vector3.zero;
        rb.constraints = RB_ROT_CONSTR;
        rb.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        rb.angularDamping = bodyRotationDrag;

        //Crunches I've tried but they did not fix the problem of rigidbody having inertial residue in angular velocity
        //var accumulatedTorque = rb.GetAccumulatedTorque(); 
        //rb.AddTorque(-accumulatedTorque, ForceMode.Force);

        //UnityEditor.Unsupported.SmartReset(rb);
    }

    private void OnDisable()
    {
        if (isServer)
        {
            OnInventoryChange(SyncSet<uint>.Operation.OP_CLEAR, 0);
        }
        EventManager.Instance.RemovePlayer(this);
        JoysticVisualizer.direction = Vector2.zero;
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

        Touch rotationTouch = new();
        bool rotTouchPresent = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).position.x > Screen.width / 2)
            {
                rotTouchPresent = true;
                rotationTouch = Input.GetTouch(i);
                if (rotationTouch.phase == TouchPhase.Began)
                {
                    userInput = userInput | UserInput.Push;
                }
                break;
            }
        }

        var rot = Vector3.zero;
        if (isTouchPresent)
        {
            if (rotTouchPresent)
                rot = new Vector3(-rotationTouch.deltaPosition.y / cam.pixelHeight, rotationTouch.deltaPosition.x / cam.pixelWidth / 0.5f) * touchRotationSensitivity;
        }
        else
        {
            rot = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * (camRotationSpeed);
        }
        var newEuler = container.rotation.eulerAngles + rot;
        newEuler.z = 0;
        while (newEuler.x > 180)
            newEuler.x -= 360;
        newEuler.x = Mathf.Clamp(newEuler.x, minCamAngle, maxCamAngle);
        while (newEuler.x < 0)
            newEuler.x += 360;
        container.rotation = Quaternion.Euler(newEuler);

        targetLookAngleY = container.rotation.eulerAngles.y;
    }
}
