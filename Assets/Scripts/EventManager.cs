using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class EventManager : NetworkBehaviour
{
    [SerializeField] EventHandler handler;

    [SerializeField] float scoreForRofling = 1f;

    private void Awake()
    {
        instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        scores.Clear();
    }

    private readonly SyncDictionary<uint, float> scores = new();

    private Dictionary<uint, NetworkCharacterController> players = new();
    public void AddPlayer(NetworkCharacterController player)
    {
        players.Add(player.netId, player);
    }
    public void RemovePlayer(NetworkCharacterController player)
    {
        players.Remove(player.netId);
    }
    public int PlayerCount => players.Count;
    [Server]
    public void ReportBeingRofledBy(uint cause)
    {
        if (cause != PlayerHealth.INVALID_SRC)
        {
            if (!scores.ContainsKey(cause)) scores.Add(cause, 0);
            scores[cause] += scoreForRofling;
        }
    }
    [Server]
    public void ReportBonusCollected(uint collector, EventHandler.EvtKind kind)
    {
        RpcBonusCollected(kind);
        var data = GetEventData(kind);
        StartCoroutine(TriggerEventEnding(collector, data));
        //todo: use collector to add score
    }
    private IEnumerator TriggerEventEnding(uint eventSource, EventHandler.Data data)
    {
        yield return new WaitForSeconds(data.EndingDelay);
        if (eventSource != PlayerHealth.INVALID_SRC)
        {
            foreach (var item in players)
            {
                item.Value.TakeRandomDamage(1.1f, eventSource);
            }
        }

    }
    [ClientRpc]
    private void RpcBonusCollected(EventHandler.EvtKind kind)
    {
        handler.TriggerEvent(kind);
    }

    public EventHandler.Data GetEventData(EventHandler.EvtKind kind)
    {
        EventHandler.Data ret = null;
        Debug.Assert(kind != EventHandler.EvtKind.None);
        foreach (var evt in handler.Events)
        {
            if (evt.kind == kind)
            {
                ret = evt;
                break;
            }
        }
        return ret;
    }


    private static EventManager instance;
    public static EventManager Instance
    {
        get
        {
            return instance;
        }
    }
    public static float GetScore(uint netID)
    {
        var ret = 0f;
        if (instance != null)
        {
            if (instance.scores.ContainsKey(netID))
            {
                ret = instance.scores[netID];
            }
        }
        return ret;
    }
}
