using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class Sentry : NetworkBehaviour
{
    [SerializeField] float rechargeTime = 8f;
    [SerializeField] Color depletedColor = Color.blue;
    [SerializeField] Color chargedColor = Color.red;
    [SerializeField] Color warningColor = Color.yellow;


    Rigidbody rb;
    Material material;
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
            rb = GetComponent<Rigidbody>();
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            UpdateCharge();
        }
        if (charge < 1f)
            material.color = Color.Lerp(depletedColor, warningColor, charge);
        else material.color = chargedColor;
    }

    [Server]
    private void UpdateCharge()
    {
        charge += Time.deltaTime / rechargeTime;
        charge = Mathf.Clamp01(charge);
    }

    [SyncVar]
    private float charge = 0f;
    private void OnTriggerEnter(Collider other)
    {
        if (isServer && other.attachedRigidbody)
        {
            var directlyVisible = Physics.Raycast(transform.position, other.transform.position - transform.position, out var rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers);
            if (directlyVisible && rhi.collider.attachedRigidbody && rhi.collider.attachedRigidbody == other.attachedRigidbody)
                PushPlayer(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (isServer && other.attachedRigidbody)
        {
            var directlyVisible = Physics.Raycast(transform.position, other.transform.position - transform.position, out var rhi, float.PositiveInfinity, Physics.DefaultRaycastLayers);
            if (directlyVisible && rhi.collider.attachedRigidbody && rhi.collider.attachedRigidbody == other.attachedRigidbody)
                PushPlayer(other);
        }
    }

    [Server]
    private void PushPlayer(Collider other)
    {
        var character = other.attachedRigidbody.gameObject.GetComponent<NetworkCharacterController>();
        if (character != null && charge >= 1f)
        {
            var direction = character.transform.position - transform.position;
            direction.Normalize();
            var force = direction * character.pushMaxForce;
            other.attachedRigidbody.AddForce(force, ForceMode.Impulse);
            rb.AddForce(-force, ForceMode.Impulse);
            charge = 0f;
        }
    }
}
