using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventfulNetworkManager : NetworkManager
{
    public event UnityAction OnClientStarted;
    public event UnityAction OnClientStopped;
    public event UnityAction OnServerStarted;
    public event UnityAction OnServerStopped;
    /// <summary>
    /// On client fired when client is connected to server and the address is known
    /// </summary>
    public event UnityAction<string> OnConnectedToServer;
    public static EventfulNetworkManager Instance => singleton as EventfulNetworkManager;

    [System.Flags]
    public enum State
    {
        None, Server = 1, Client = 2, Host = 3
    }

    public State state { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnClientStarted?.Invoke();
        state |= State.Client;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        OnClientStopped?.Invoke();
        state &= ~State.Client;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStarted?.Invoke();
        state |= State.Server;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        OnServerStopped?.Invoke();
        state &= ~State.Server;
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        var address = networkAddress;
        if(Transport.active is PortTransport portTransport)
        {
            address = $"{networkAddress}:{portTransport.Port}";
        }
        OnConnectedToServer?.Invoke(address);
    }
}
