using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class EventManager : NetworkBehaviour
{
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
    [Server]
    public void ReportBeingRofledBy(uint cause)
    {
        if (cause != PlayerHealth.INVALID_SRC)
        {
            if (!scores.ContainsKey(cause))
            {
                scores.Add(cause, 0);
            }
            scores[cause] += scoreForRofling; 
        }
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
        if(instance != null)
        {
            if(instance.scores.ContainsKey(netID))
            {
                ret = instance.scores[netID];
            }
        }
        return ret;
    }
}
