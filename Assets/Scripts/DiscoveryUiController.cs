using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DiscoveryUiController : MonoBehaviour
{
    public const string ADDRESSES_KEY = "KNOWN_ADDRESSES";
    public const string ADDRESS_SEPAR = ";";
    public static string DEFAULT_ADDRESS => $"localhost:7777";

    [SerializeField] DiscoveryUiView view;
    public IReadOnlyCollection<string> KnownAddresses => knownAddresses;

    public void ServerFound(ServerResponse response)
    { 
        view.SetAddress($"{response.EndPoint.Address}:{(manager.transport as PortTransport).Port}");
    }

    List<string> knownAddresses = new();
    EventfulNetworkManager manager;
    NetworkDiscovery networkDiscovery;
    private void Start()
    {
        manager = EventfulNetworkManager.Instance;
        networkDiscovery = GetComponent<NetworkDiscovery>();
        networkDiscovery.OnServerFound.AddListener(ServerFound);

        view.ConnectClicked += ConnectClicked;
        view.RemoveClicked += i => RemoveAddressAt(i);
        view.stopButton.onClick.AddListener(StopWhateverClicked);
        view.hostButton.onClick.AddListener(HostClicked);

        view.pingLanButton.onClick.AddListener(PingLanClicked);

        manager.OnClientStarted += () => { if (!NetworkServer.active) networkDiscovery.StopDiscovery(); };
        manager.OnServerStarted += () => networkDiscovery.AdvertiseServer();

        manager.OnServerStopped += OnStopped;
        manager.OnClientStopped += OnStopped;

        manager.OnConnectedToServer +=
            address =>
            {
                AddAddress(address);
                #if UNITY_ANDROID
                SaveAddresses();
                #endif
            };

        var addresses = PlayerPrefs.GetString(ADDRESSES_KEY, string.Empty);
        if (string.IsNullOrEmpty(addresses))
            AddAddress(DEFAULT_ADDRESS);
        foreach (var addr in addresses.Split(ADDRESS_SEPAR))
            AddAddress(addr);
    }

    private void PingLanClicked()
    {
        networkDiscovery.StartDiscovery();
        networkDiscovery.Invoke(nameof(networkDiscovery.BroadcastDiscoveryRequest), 1);
    }

    private void OnStopped()
    {
        networkDiscovery.StopDiscovery();
    }

    private void HostClicked()
    {
        manager.StartHost();
    }

    private void StopWhateverClicked()
    {
        if (NetworkServer.activeHost)
            manager.StopHost();
        else if (NetworkClient.active)
            manager.StopClient();
        else if (NetworkServer.active)
            manager.StopServer();
    }

    private void ConnectClicked(string address)
    {
        if (!string.IsNullOrEmpty(address))
        {
            if (Transport.active is PortTransport portTransport)
            {
                var split = address.Split(':');
                if (!string.IsNullOrEmpty(split[0]))
                    manager.networkAddress = split[0];
                else manager.networkAddress = DEFAULT_ADDRESS.Split(':')[0];
                if (split.Length > 1 && ushort.TryParse(split[1], out var port))
                {
                    portTransport.Port = port;
                }
                else portTransport.Port = ushort.Parse(DEFAULT_ADDRESS.Split(':')[1]);
            }
            else
                manager.networkAddress = address;
            manager.StartClient();
        }
    }
    private void AddAddress(string address)
    {
        var validated = address.Replace(" ", string.Empty).ToLowerInvariant();
        if (!knownAddresses.Contains(validated) && !string.IsNullOrEmpty(address))
        {
            knownAddresses.Add(validated);
            view.UpdateKnownAddresses(knownAddresses);
        }
    }
    private void RemoveAddressAt(int i)
    {
        knownAddresses.RemoveAt(i);
        view.UpdateKnownAddresses(knownAddresses);
    }

    private void Update()
    {
        var shouldStopViewed = NetworkServer.active || NetworkClient.active;
#if UNITY_WEBGL
        shouldStopViewed = false;
#endif
        var shouldMenuViewed = !shouldStopViewed;
#if UNITY_WEBGL
        shouldMenuViewed = false;
#endif
        view.UpdateMenuVisibility(shouldStopViewed, shouldMenuViewed);
    }

    private void OnDisable()
    {
        SaveAddresses();
    }

    private void SaveAddresses()
    {
        PlayerPrefs.SetString(ADDRESSES_KEY, string.Join(ADDRESS_SEPAR, knownAddresses));
    }
}
