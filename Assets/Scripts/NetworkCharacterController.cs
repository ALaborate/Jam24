using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class NetworkCharacterController : NetworkBehaviour
{
    public float moveSpeed = 10f;
    public float jumpForce = 10f;
    public float rotationSpeed = 1f;

    private Rigidbody rb;
    private CapsuleCollider col;

    public LayerMask groundLayer = Physics.DefaultRaycastLayers;

    public float groundDistance = 1f;
    [SerializeField][ReadOnly] private bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        // Get the rigidbody component
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
    }


    private void FixedUpdate()
    {
        //if (!isLocalPlayer)
        //    return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical);
        moveDirection = transform.TransformDirection(moveDirection);
        rb.AddForce(moveDirection * moveSpeed * Time.fixedDeltaTime, ForceMode.Acceleration);
        

        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance, groundLayer);
        Debug.DrawLine(transform.position, transform.position + Vector3.down * groundDistance, Color.blue, -1, false);

        if (isGrounded && Input.GetKey(KeyCode.Space))
        {
            // Add an upward force to the rigidbody to make the character jump
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        if (isGrounded)
        {
            var mouseMovement = Input.mouseScrollDelta.x;
            rb.AddTorque(Vector3.up * mouseMovement * rotationSpeed * Time.fixedDeltaTime, ForceMode.Acceleration);
        }

    }
}
