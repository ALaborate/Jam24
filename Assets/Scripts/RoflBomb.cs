using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoflBomb : NetworkBehaviour, ICollectable
{
    [SerializeField] float spriteRotation = 60f;
    [SyncVar]
    public EventHandler.EvtKind kind;

    SpriteRenderer spriteSymbol;
    EventHandler.Data data;
    [Server]
    public void Collect(uint coolectorNetID)
    {
        EventManager.Instance.ReportBonusCollected(coolectorNetID, kind);
        Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        spriteSymbol = GetComponentInChildren<SpriteRenderer>();
        data = EventManager.Instance.GetEventData(kind);
        spriteSymbol.sprite = data.picture;
    }

    // Update is called once per frame
    void Update()
    {
        spriteSymbol.transform.rotation *= Quaternion.Euler(0, spriteRotation * Time.deltaTime, 0);
    }
}


public interface ICollectable
{
    void Collect(uint collectorNetID);
}