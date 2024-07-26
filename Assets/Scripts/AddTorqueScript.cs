
using UnityEditor.Timeline;
using UnityEngine;

public class AddTorqueScript : MonoBehaviour
{
    public float logPeriod = 10f;
    public float impulse = 10f;
    public bool neutralize = true;

    [SerializeField] Vector3 angularVelocity;
    [SerializeField] float maxAngularVelocity;
    [SerializeField] Vector3 accumulatedTorque;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    float nextTimeToLog = 10f;
    private void FixedUpdate()
    {
        rb.AddTorque(Vector3.up * impulse, ForceMode.Impulse);
        var accumulatedTorque = rb.GetAccumulatedTorque();
        if (neutralize)
        {
            var prevConstraints = rb.constraints;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddTorque(accumulatedTorque * -1f, ForceMode.Force);
            rb.constraints = prevConstraints;
        }

        accumulatedTorque = rb.GetAccumulatedTorque();

        if (Time.time > nextTimeToLog)
        {
            Debug.Log($"Accumulated torque ({accumulatedTorque.x}, {accumulatedTorque.y}, {accumulatedTorque.z})");
            nextTimeToLog = Time.time + logPeriod;
        }
    }

    private void Update()
    {
        angularVelocity = rb.angularVelocity;
        maxAngularVelocity = rb.maxAngularVelocity;
    }

    private void OnValidate()
    {
        if (rb)
        {
            rb.angularVelocity = angularVelocity;
            rb.maxAngularVelocity = maxAngularVelocity;
        }
    }
}