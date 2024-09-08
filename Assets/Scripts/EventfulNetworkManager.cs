using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventfulNetworkManager : NetworkManager
{
    public event UnityAction OnClientStart;
    public event UnityAction OnClientStop;
    public event UnityAction OnServerStart;
    public event UnityAction OnServerStop;

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnClientStart?.Invoke();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnClientStop?.Invoke();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStart?.Invoke();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        OnServerStop?.Invoke();
    }
}
