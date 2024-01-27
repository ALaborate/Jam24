using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feather : Mirror.NetworkBehaviour, IPickable
{
    public float releseVelocity = 2f;
    public float respawnTimout = 64f;

    Collider col;
    Rigidbody rb;

    private void Start()
    {
        col = GetComponentInChildren<Collider>();
        rb = GetComponent<Rigidbody>();
    }
    public void PickUp(GameObject player, Transform place)
    {
        if(place == null)
        {
            gameObject.SetActive(false);
        }
        else
        {
            if (rb == null) Start();

            rb.isKinematic = true;
            col.enabled = false;
            transform.SetParent(place);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity; 
        }
    }

    public void Drop(GameObject formerPlayer)
    {
        gameObject.SetActive(true);
        rb.isKinematic = false;
        col.enabled = true;
        transform.SetParent(null);
        transform.position = formerPlayer.transform.position + Vector3.up * 1.2f;
        rb.AddForce(Vector3.up * releseVelocity, ForceMode.VelocityChange);

        nextTimeToRespawn = Time.time + respawnTimout;
    }

    public override void OnStartServer()
    {
        nextTimeToRespawn = Time.time + respawnTimout;
    }

    private float nextTimeToRespawn;
    private void Update()
    {
        if (Time.time >= nextTimeToRespawn || transform.position.y < -30)
        {
            gameObject.SetActive(true);
            nextTimeToRespawn = Time.time + respawnTimout;
            var spawns = GameObject.FindGameObjectsWithTag("FeatherSpawn");

            Vector3 spawnPos = Vector3.up * 30;
            if (spawns.Length > 0)
            {
                var spawnInx = Random.Range(0, spawns.Length);
                spawnPos = spawns[spawnInx].transform.position;
            }
            transform.position = spawnPos + Random.onUnitSphere;
        }
    }
}

public interface IPickable
{
    void PickUp(GameObject player, Transform place);
    void Drop(GameObject formerPlayer);
}
