using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] float regenerationRate = 0.05f;
    // Start is called before the first frame update

    [SyncVar]
    [SerializeField]
    [ReadOnly]
    private float health = 1f;
    [SyncVar]
    private bool isRofled = false;

    public float Health
    {
        get
        {
            return health;
        }
    }
    public bool IsRofled
    {
        get
        {
            return isRofled;
        }
    }   

    public void TakeDamage(float damage)
    {
        if (isServer)
        {
            health = health - damage;
            health = Mathf.Clamp01(health);
            if(health == 0)
            {
                isRofled = true;
            } 
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            health = health + regenerationRate * Time.deltaTime;
            health = Mathf.Clamp01(health); 
            if(health == 1)
            {
                isRofled = false;
            }
        }
    }
}
