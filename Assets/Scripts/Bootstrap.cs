using System.Collections.Generic;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public static Bootstrap Instance { get; private set; }
    public AccelShake accelShake;
    public JoysticVisualizer joysticVisualizer;
    public UnityEngine.UI.InputField playerNameField;

    private void Awake()
    {
        Instance = this;
    }
}
