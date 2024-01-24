using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feather : MonoBehaviour, IPickable
{
    public float releseVelocity = 2f;

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
    }
}

public interface IPickable
{
    void PickUp(GameObject player, Transform place);
    void Drop(GameObject formerPlayer);
}
