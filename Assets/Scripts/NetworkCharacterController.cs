using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class NetworkCharacterController : NetworkBehaviour
{
    private const RigidbodyConstraints RB_ROT_CONSTR = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    public float moveSpeed = 10f;
    public float jumpForce = 10f;
    public float camRotationSpeed = 1f;
    public float bodyYRotationTorque = 1f;
    public float bodyStabilizationSpeed = 4;
    public float bodyStability = 0.3f;
    float roflRandomVelocityMultiplier = 3.14f;

    public float groundCastDistance = 1.1f;
    public LayerMask groundLayer = Physics.DefaultRaycastLayers;

    public float minCamAngle = -30;
    public float maxCamAngle = 60;
    public Vector3 camOffset = new Vector3(1, 0, 0);

    public float maxFloatingForce = 500;
    public AnimationCurve floatingForceCurve = AnimationCurve.Linear(0.5f, 1, 1.5f, 0);
    public float floatingForceReductionDenominator = 10;

    public float maxRunningSpeed = 10;

    [Header("Self-damage")]
    public float maxVelocityDamage = .8f;

    public Transform hand;


    private Rigidbody rb;
    private CapsuleCollider col;
    private Camera cam;
    private PlayerHealth health;
    private float height;

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

            if (isServer)
            {
                health.OnRofl += OnRofl;
                health.OnRoflOver += OnRoflOver;
            }

            DisableControlAndCamera();

            if (isClient) //pick up all objects picked earlier than connection
            {
                foreach (var item in inventoryIds)
                {
                    //OnInventoryChange(SyncSortedSet<NetworkIdentity>.Operation.OP_ADD, item);
                }
            }
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
    private void Update()
    {
        if (isLocalPlayer)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            CmdMove(vertical, horizontal, Input.GetKey(KeyCode.Space), targetLookAngleY);

        }
    }
    public bool HasFeather => inventoryIds.Count > 0;

    private double lastCmdMoveTime = 0;
    private RaycastHit[] groundHits;
    /// <summary>
    /// Distance between the ground and body center (not equal to center of collider)
    /// </summary>
    [SerializeField][ReadOnly] private float minGroundDistance = float.MaxValue;
    [SerializeField][ReadOnly] private float footGrip = 0f;
    [SerializeField][ReadOnly] private bool isGrounded;
    [Command]
    private void CmdMove(float run, float strafe, bool jump, float targetRotationY)
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
        rb.AddForce(moveDirection * moveSpeed * (float)dt * forceSpeedReduction * footGrip, ForceMode.Acceleration);

        isGrounded = minGroundDistance <= 1;
        if ((isGrounded || health.IsRofled) && rb.velocity.y < 0.1f && jump && dt > 0 && rb.velocity.sqrMagnitude < maxRunningSpeed * maxRunningSpeed * 10)
        {
            // Add an upward force to the rigidbody to make the character jump
            rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
            //rb.AddTorque(Vector3.one * jumpForce, ForceMode.VelocityChange);
        }

        targetRotation = new Vector3(0, targetRotationY);

        receivedUserInput = strafe != 0 || run != 0;
    }



    private bool receivedUserInput = false;
    [SerializeField][ReadOnly] private Vector3 targetRotation;
    private void FixedUpdate()
    {
        if (rb != null)
        {//server
            const int GROUND_RAY_COUNT = 4;
            if (groundHits == null) groundHits = new RaycastHit[GROUND_RAY_COUNT];

            minGroundDistance = float.MaxValue;
            if (groundCollisionHashes.Count > 0)
            {
                minGroundDistance = col.height / 2 - col.center.y;
            }
            for (int i = 0; i < groundHits.Length; i++)
            {
                var ray = new Ray(transform.position + Quaternion.Euler(0, i * (360 / groundHits.Length), 0) * (Vector3.forward * col.radius * .5f), Vector3.down);
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * groundCastDistance, Color.green, 0f, true);
                var hit = Physics.Raycast(ray, out groundHits[i], groundCastDistance, groundLayer);
                if (!hit) groundHits[i].distance = float.PositiveInfinity;
                if (groundHits[i].distance < minGroundDistance)
                    minGroundDistance = groundHits[i].distance;
                if (hit)
                    footGrip = (footGrip * i + groundHits[i].collider.material.dynamicFriction) / (i + 1);
            }

            AddFloatingForce();

            ApplyStoppingForce();

            RotateToCameraDirection();


            if(transform.position.y < -100)
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

#if UNITY_WEBGL

#else

#endif


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
        if (!receivedUserInput && !health.IsRofled)
        {
            //apply counterforce
            var moveDirection = new Vector3(-rb.velocity.x, 0, -rb.velocity.z);
            rb.AddForce(moveDirection.normalized * moveSpeed * Time.fixedDeltaTime * footGrip, ForceMode.Acceleration);
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



    private SortedSet<int> groundCollisionHashes = new();
    private readonly SyncHashSet<uint> inventoryIds = new();
    private void OnCollisionEnter(Collision collision)
    {
        if (isServer)
        {
            if (IsColisionWithGround(collision))
            {
                groundCollisionHashes.Add(collision.collider.GetInstanceID());
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

        if (pickable != null)
        {
            if (pickableNetId != null)
            {
                inventoryIds.Add(pickableNetId.netId);
            }
            //pickable.PickUp(gameObject, hand);
            ////if (pickableNetId != null && isServer)
            ////    RpcPickObject(pickableNetId);
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
        if(op == SyncSet<uint>.Operation.OP_CLEAR)
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
            if(otherPlayer.HasFeather)
            {
                damageReduction *= 1f;
            }
            else
            {
                damageReduction *= 0f; //no damage if no feather
            }
        }
        else
        {
            damageReduction *= 1f; //full damage if wall
        }
        var damage = (mag / maxRunningSpeed) * maxVelocityDamage * asymmetryCoef * fallDamageReductionCoef * damageReduction;
        health.TakeDamage(Mathf.Clamp01(damage));
    }

    private void OnCollisionExit(Collision collision)
    {
        if (isServer)
        {
            groundCollisionHashes.Remove(collision.collider.GetInstanceID());
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




    private void OnRofl()
    {
        if (isServer) inventoryIds.Clear();

        rb.constraints = RigidbodyConstraints.None;
        rb.AddTorque(Random.onUnitSphere * roflRandomVelocityMultiplier, ForceMode.VelocityChange);
    }

    private void OnRoflOver()
    {
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
        var rot = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X")) * (camRotationSpeed * Time.deltaTime);
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
