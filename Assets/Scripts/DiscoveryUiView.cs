using Mirror;
using Mirror.BouncyCastle.Asn1.Mozilla;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class DiscoveryUiView : MonoBehaviour
{
    public Button stopButton;
    [SerializeField] Button selectModeSceenButton;

    [SerializeField] Button createScreenButton;
    public Button hostButton;
    public Button headlessButton;

    [SerializeField] Button connectScreeenButton;
    public Button pingLanButton;
    [SerializeField] Button connectToButton;
    [SerializeField] InputField unknownAddress;
    [SerializeField] GameObject[] additionalConnectScreenObjects;

    [SerializeField] GameObject knownAddressPrefab;

    public event UnityAction<string> ConnectClicked;
    public event UnityAction<int> RemoveClicked;


    private void Start()
    {
        connectToButton.onClick.AddListener(() => ConnectClicked?.Invoke(unknownAddress.text));
        unknownAddress.text = DiscoveryUiController.DEFAULT_ADDRESS;
        verticalGroupTransform = hostButton.transform.parent as RectTransform;


        createScreen = new(hostButton, headlessButton, selectModeSceenButton);
        connectScreen = new(pingLanButton, connectToButton, unknownAddress, selectModeSceenButton);
        connectScreen.visibleGameObjects.AddRange(additionalConnectScreenObjects);
        selectModeScreen = new(createScreenButton, connectScreeenButton);

        createScreenButton.onClick.AddListener(() => ChangeScreen(createScreen));
        connectScreeenButton.onClick.AddListener(() => ChangeScreen(connectScreen));
        selectModeSceenButton.onClick.AddListener(() => ChangeScreen(selectModeScreen));

        ChangeScreen(selectModeScreen);
    }

    public void SetAddress(string address) => unknownAddress.text = address;

    Screen createScreen;
    Screen connectScreen;
    Screen selectModeScreen;

    List<AddressUI> buttons = new();
    IReadOnlyCollection<string> newAddresses = null;
    RectTransform verticalGroupTransform;
    void Update()
    {
        if (newAddresses != null)
        {
            int i = 0;
            foreach (var addr in newAddresses)
            {
                if (i >= buttons.Count)
                {
                    buttons.Add(CreateKnownAddressButton());
                }
                var btn = buttons[i];

                btn.label.text = addr;
                btn.index = i;

                i++;
            }
            for (; i < buttons.Count; i++)
            {
                buttons[i].rTransform.gameObject.SetActive(false);
            }
            newAddresses = null;
        }
    }

    public void UpdateMenuVisibility(bool shouldStopBeEnabled, bool shouldMenuBeEnabled)
    {
        if (stopButton.isActiveAndEnabled != shouldStopBeEnabled)
            stopButton.gameObject.SetActive(shouldStopBeEnabled);
        if (verticalGroupTransform.gameObject.activeInHierarchy != shouldMenuBeEnabled)
            verticalGroupTransform.gameObject.SetActive(shouldMenuBeEnabled);
    }
    private AddressUI CreateKnownAddressButton()
    {
        var go = Instantiate(knownAddressPrefab);
        var btns = go.GetComponentsInChildren<Button>();

        var result = new AddressUI()
        {
            connect = btns[0],
            remove = btns[1],
            rTransform = go.transform as RectTransform
        };
        result.label = result.connect.GetComponentInChildren<Text>();
        result.rTransform.parent = transform;
        result.rTransform.localScale = Vector3.one;
        connectScreen.visibleGameObjects.Add(go);
        connectScreen.Active = false;
        ChangeScreen(_currentScreen);

        result.connect.onClick.AddListener(() => ConnectClicked?.Invoke(result.label.text));
        result.remove.onClick.AddListener(() => RemoveClicked?.Invoke(result.index));
        return result;
    }

    public void UpdateKnownAddresses(IReadOnlyCollection<string> knownAddresses)
    {
        newAddresses = knownAddresses;
    }

    Screen _currentScreen;
    Screen[] _allScreens;
    private void ChangeScreen(Screen newScreen)
    {
        if(_allScreens == null)
        {
            var fields = GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var screens = from f in fields where f.FieldType == typeof(Screen) let v = f.GetValue(this) as Screen where v != null select v;
            _allScreens = screens.ToArray();
        }

        if (_currentScreen == null)
            foreach (var item in _allScreens)
                item.Active = false;
        else
            _currentScreen.Active = false;

        _currentScreen = newScreen;
        _currentScreen.Active = true;
    }

    class AddressUI
    {
        public Button connect;
        public Text label;
        public Button remove;
        public int index;
        public RectTransform rTransform;
    }

    class Screen
    {
        public List<GameObject> visibleGameObjects;
        public bool Active
        {
            get => visibleGameObjects.Any(go => go.activeInHierarchy);
            set
            {
                for (int i = 0; i < visibleGameObjects.Count; i++)
                {
                    if (!visibleGameObjects[i])
                        visibleGameObjects.RemoveAt(i--);
                    else
                    {
                        visibleGameObjects[i].SetActive(value);
                    }
                }
            }
        }

        public Screen() { }
        public Screen(params Component[] components)
        {
            visibleGameObjects = new(from c in components select c.gameObject);
        }
    }
}
