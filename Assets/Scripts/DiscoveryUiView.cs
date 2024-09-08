using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiscoveryUiView : MonoBehaviour
{
    [SerializeField] public Button stopButton;
    [SerializeField] public Button hostButton;
    [SerializeField] Button connectToButton;
    [SerializeField] public Button pingLanButton;
    [SerializeField] InputField unknownAddress;
    [SerializeField] GameObject knownAddressPrefab;

    public event UnityAction<string> ConnectClicked;
    public event UnityAction<int> RemoveClicked;


    class AddressUI
    {
        public Button connect;
        public Text label;
        public Button remove;
        public int index;
        public RectTransform rTransform;
    }

    private void Start()
    {
        connectToButton.onClick.AddListener(() => ConnectClicked?.Invoke(unknownAddress.text));
        unknownAddress.text = DiscoveryUiController.DEFAULT_ADDRESS;
        buttonsParent = hostButton.transform.parent as RectTransform;
    }

    public void SetAddress(string address) => unknownAddress.text = address;

    List<AddressUI> buttons = new();
    IReadOnlyCollection<string> newAddresses = null;
    RectTransform buttonsParent;
    void Update()
    {
        if (newAddresses != null)
        {
            int i = 0;
            foreach (var addr in newAddresses)
            {
                if (i >= buttons.Count)
                {
                    buttons.Add(CreateButton());
                }
                var btn = buttons[i];

                btn.label.text = addr;
                btn.index = i;
                btn.rTransform.gameObject.SetActive(true);

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
        if (buttonsParent.gameObject.activeInHierarchy != shouldMenuBeEnabled)
            buttonsParent.gameObject.SetActive(shouldMenuBeEnabled);
    }
    private AddressUI CreateButton()
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

        result.connect.onClick.AddListener(() => ConnectClicked?.Invoke(result.label.text));
        result.remove.onClick.AddListener(() => RemoveClicked?.Invoke(result.index));
        return result;
    }

    public void UpdateKnownAddresses(IReadOnlyCollection<string> knownAddresses)
    {
        newAddresses = knownAddresses;
    }
}
