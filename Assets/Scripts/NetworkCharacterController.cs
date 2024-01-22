using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

#if UNITY_WEBGL
public class NetworkCharacterController : MonoBehaviour
#else
public class NetworkCharacterController : NetworkBehaviour
#endif
{
    public float moveSpeed = 10f;
    public float jumpForce = 10f;
    public float rotationSpeed = 1f;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer = Physics.DefaultRaycastLayers;

    public float minCamAngle = -30;
    public float maxCamAngle = 60;




    [SerializeField][ReadOnly] private bool isGrounded;

    private Rigidbody rb;
    private CapsuleCollider col;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (col == null)
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponentInChildren<CapsuleCollider>();
            cam = GetComponentInChildren<Camera>();
            DisableControlAndCamera();
        }
    }
    private void DisableControlAndCamera()
    {
        if (!isLocalPlayer)
        {
            Destroy(cam.gameObject);
            Destroy(rb);
        }
    }

    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //    Initialize();
    //}

    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    Initialize();
    //}


    private void Update()
    {
        if (rb != null)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 moveDirection = new Vector3(horizontal, 0, vertical);
            moveDirection = transform.TransformDirection(moveDirection);
            rb.AddForce(moveDirection * moveSpeed * Time.deltaTime, ForceMode.Acceleration);


            isGrounded = Physics.CheckSphere(transform.position + Vector3.down * col.height / 2, groundCheckRadius, groundLayer);

            if (isGrounded && Input.GetKey(KeyCode.Space))
            {
                // Add an upward force to the rigidbody to make the character jump
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }

            var mouseMovement = Input.GetAxis("Mouse X");
            transform.localRotation *= Quaternion.Euler(0, mouseMovement * (rotationSpeed * Time.deltaTime), 0);
        }
        //transform.Rotate(mouseMovement * (rotationSpeed * Time.deltaTime) * Vector3.up);
    }

    private void LateUpdate()
    {
        RotateCamera();
    }

    private void RotateCamera()
    {
        if (cam == null)
        {
            return;
        }
        var mouseMovement = Input.GetAxis("Mouse Y");
        var container = cam.transform.parent;
        container.localRotation *= Quaternion.Euler(mouseMovement * (rotationSpeed * Time.deltaTime) * Vector3.left);
        container.localRotation = ClampRotationAroundXAxis(container.localRotation);
    }

    private Quaternion ClampRotationAroundXAxis(Quaternion localRotation)
    {
        //autogenerated
        var angle = Quaternion.Angle(Quaternion.identity, localRotation);
        var sign = Mathf.Sign(localRotation.x);
        if (angle > maxCamAngle)
        {
            return Quaternion.identity * Quaternion.Euler(maxCamAngle * sign, 0, 0);
        }
        else if (angle < minCamAngle)
        {
            return Quaternion.identity * Quaternion.Euler(minCamAngle * sign, 0, 0);
        }
        return localRotation;
    }
}
