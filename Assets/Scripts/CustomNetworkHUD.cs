using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomNetworkHUD : MonoBehaviour
{
    NetworkManager manager;

    public int offsetX;
    public int offsetY;
    [Range(1e-5f, 0.2f)]
    public float btnHeightCoef = 0.03f;

    private Camera mainCam;

    void Awake()
    {
        manager = GetComponent<NetworkManager>();
        mainCam = Camera.main;
    }

    private void Start()
    {
#if UNITY_WEBGL
        manager.StartHost(); //dont show gui in webgl
#endif
    }

    void OnGUI()
    {
        // If this width is changed, also change offsetX in GUIConsole::OnGUI
        int width = 300;
        var cam = Camera.main;
        width = Mathf.RoundToInt(cam.pixelWidth / 2);

        GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, width, 9999));

        if (!NetworkClient.isConnected && !NetworkServer.active)
            StartButtons();
        else
            StatusLabels();

        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            if (GUILayout.Button("Client Ready"))
            {
                // client ready
                NetworkClient.Ready();
                if (NetworkClient.localPlayer == null)
                    NetworkClient.AddPlayer();
            }
        }

        StopButtons();

        GUILayout.EndArea();
    }


    void StartButtons()
    {
        if (!NetworkClient.active)
        {
#if UNITY_WEBGL
                // cant be a server in webgl build
                if (GUILayout.Button("Single Player"))
                {
                    NetworkServer.dontListen = true;
                    manager.StartHost();
                }
#else
            // Server + Client
            if (GUILayout.Button("Host (Server + Client)", GUILayout.Height(mainCam.pixelHeight * btnHeightCoef)))
                manager.StartHost();
#endif

            // Client + IP (+ PORT)
            GUILayout.BeginHorizontal();

#if !UNITY_WEBGL
            if (GUILayout.Button("Client", GUILayout.ExpandWidth(false), GUILayout.Height(mainCam.pixelHeight * btnHeightCoef)))
                manager.StartClient(); 
#endif
            PortTransport portTransport = Transport.active as PortTransport;
            string fullAddress = GUILayout.TextField(manager.networkAddress + (portTransport == null ? "" : $":{portTransport.Port}"), GUILayout.Height(mainCam.pixelHeight * btnHeightCoef));
            // only show a port field if we have a port transport
            // we can't have "IP:PORT" in the address field since this only
            // works for IPV4:PORT.
            // for IPV6:PORT it would be misleading since IPV6 contains ":":
            // 2001:0db8:0000:0000:0000:ff00:0042:8329
            if (portTransport != null)
            {
                var splitAddress = fullAddress.Split(':');
                manager.networkAddress = splitAddress[0];
                const ushort defaultPort = 7777;
                ushort port = defaultPort;
                // use TryParse in case someone tries to enter non-numeric characters
                if (splitAddress.Length > 1)
                    if (!ushort.TryParse(splitAddress[1], out port))
                        port = defaultPort;
                portTransport.Port = port;
            }
            else
            {
                manager.networkAddress = fullAddress;
            }

            GUILayout.EndHorizontal();

            // Server Only
#if UNITY_WEBGL
                // cant be a server in webgl build
                GUILayout.Box("( WebGL cannot be server )");
#else
            if (GUILayout.Button("Server Only", GUILayout.Height(mainCam.pixelHeight * btnHeightCoef)))
                manager.StartServer();
#endif
        }
        else
        {
            // Connecting
            GUILayout.Label($"Connecting to {manager.networkAddress}..", GUILayout.Height(mainCam.pixelHeight * btnHeightCoef));
            if (GUILayout.Button("Cancel Connection Attempt", GUILayout.Height(mainCam.pixelHeight * btnHeightCoef)))
                manager.StopClient();
        }
    }

    void StatusLabels()
    {
        // host mode
        // display separately because this always confused people:
        //   Server: ...
        //   Client: ...
        if (NetworkServer.active && NetworkClient.active)
        {
            // host mode
            GUILayout.Label($"<b>Host</b>: running via {Transport.active}");
        }
        else if (NetworkServer.active)
        {
            // server only
            GUILayout.Label($"<b>Server</b>: running via {Transport.active}");
        }
        else if (NetworkClient.isConnected)
        {
            // client only
            GUILayout.Label($"<b>Client</b>: connected to {manager.networkAddress} via {Transport.active}");
        }
    }

    void StopButtons()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            GUILayout.BeginHorizontal();
#if UNITY_WEBGL
                if (GUILayout.Button("Stop Single Player"))
                    manager.StopHost();
#else
            // stop host if host mode
            if (GUILayout.Button("Stop Host"))
                manager.StopHost();

            // stop client if host mode, leaving server up
            if (GUILayout.Button("Stop Client"))
                manager.StopClient();
#endif
            GUILayout.EndHorizontal();
        }
        else if (NetworkClient.isConnected)
        {
            // stop client if client-only
            if (GUILayout.Button("Stop Client"))
                manager.StopClient();
        }
        else if (NetworkServer.active)
        {
            // stop server if server-only
            if (GUILayout.Button("Stop Server"))
                manager.StopServer();
        }
    }
}
