using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] float regenerationRate = 0.05f;
    [SerializeField] float delayBeforeUntickle = .5f;
    [SerializeField] float damageCausalityTime = 6f;

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

    public UnityEvent OnRofl;
    public UnityEvent OnRoflOver;
    public UnityEvent OnBeingTickled;
    public UnityEvent OnStopTickled;

    public const uint INVALID_SRC = uint.MaxValue;
    private float nextTimeToResetDamageSource = float.MaxValue;
    uint lastDamageSourceNetID = uint.MaxValue;
    [Server]
    public void SetSourceOfDamage(uint sourceNetID)
    {
        if (sourceNetID != INVALID_SRC)
        {
            lastDamageSourceNetID = sourceNetID;
            nextTimeToResetDamageSource = Time.time + damageCausalityTime;
        }
    }
    public void TakeDamage(float damage, uint sourceNetID = INVALID_SRC)
    {
        if (isServer)
        {
            health = health - damage;
            health = Mathf.Clamp01(health);
            SetSourceOfDamage(sourceNetID);
            if (health == 0 && isRofled == false)
            {
                isRofled = true;
                EventManager.Instance.ReportBeingRofledBy(lastDamageSourceNetID);
                OnRofl?.Invoke();
                RpcRpfl();
            }
        }
    }
    [ClientRpc]
    private void RpcRpfl()
    {
        if (isClientOnly) //prevent double-call on host
            OnRofl?.Invoke();
    }


    // Update is called once per frame
    private float prevHealth = 1f;
    private bool isTickledRaw = false;
    private Coroutine untickleCoroutine = null;
    void Update()
    {
        if (isServer)
        {
            health = health + regenerationRate * Time.deltaTime;
            health = Mathf.Clamp01(health);

            if (Time.time > nextTimeToResetDamageSource)
            {
                lastDamageSourceNetID = INVALID_SRC;
            }
        }

        if ((health > prevHealth || prevHealth == 1) && isTickledRaw)
        {
            isTickledRaw = false;
            if (untickleCoroutine == null)
                untickleCoroutine = StartCoroutine(UnTickle());
        }

        if (health < prevHealth && !isTickledRaw)
        {
            isTickledRaw = true;

            if (untickleCoroutine == null)
                OnBeingTickled?.Invoke();
            else
            {
                StopCoroutine(untickleCoroutine);
                untickleCoroutine = null;
            }
        }

        prevHealth = health;

        if (health == 1 && isRofled)
        {
            isRofled = false;
            OnRoflOver?.Invoke();
        }
    }

    private IEnumerator UnTickle()
    {
        yield return new WaitForSeconds(delayBeforeUntickle);
        OnStopTickled?.Invoke();
        untickleCoroutine = null;
    }
}
