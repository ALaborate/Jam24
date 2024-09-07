using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AccelShake : MonoBehaviour
{
    [SerializeField] float filter = 1f;
    [SerializeField] float threshold = .8f;
    [SerializeField] float timeout = 0.5f;
    void Start() => lowPassValue = Input.acceleration;

    public UnityEvent<float> OnShake;

    Vector3 lowPassValue = Vector3.zero;
    float maxDelta = 0f;
    float nextTimeToShake = 0f;
    void Update()
    {
        for (int i = 0; i < Input.accelerationEventCount; i++)
        {
            var evt = Input.GetAccelerationEvent(i);
            var acceleration = evt.acceleration;

            lowPassValue = Vector3.Lerp(lowPassValue, acceleration, filter * evt.deltaTime);
            var deltaAcc = acceleration - lowPassValue;
            var delta = deltaAcc.sqrMagnitude;

            if (delta > maxDelta)
                maxDelta = delta;

            if (delta > threshold * threshold && Time.time >= nextTimeToShake)
            {
                OnShake?.Invoke(delta);
                nextTimeToShake = Time.time + timeout;
                Debug.Log($"Shaking. Delta is {delta}. Overall max is {maxDelta}");
            }

        }
    }
}
