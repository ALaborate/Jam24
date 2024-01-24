using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthVisualizer : MonoBehaviour
{
    public MeshRenderer mRenderer;
    public Color healthyColor = Color.green;
    public Color roflColor = Color.red + Color.yellow;

    private PlayerHealth playerHealth;
    private Material materialInstance;
    // Start is called before the first frame update
    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        materialInstance = mRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        materialInstance.color = Color.Lerp(roflColor, healthyColor, playerHealth.Health);
    }
}
