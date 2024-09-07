using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JoysticVisualizer : MonoBehaviour
{

    private RectTransform knob;
    private RectTransform origin;
    private Image background;


    public Vector2 direction = Vector2.zero;
    // Start is called before the first frame update
    void Start()
    {
        background = GetComponent<Image>();
        knob = transform.GetChild(0) as RectTransform;
        origin = transform as RectTransform;
    }

    private void Update()
    {
        if(direction == Vector2.zero)
        {
            background.enabled = false;
            knob.gameObject.SetActive(false);
        }
        else
        {
            background.enabled = true;
            knob.gameObject.SetActive(true);
            var dirMag = direction.magnitude;
            var clampedDir = direction / (dirMag > 1 ? dirMag : 1);
            knob.anchoredPosition = (origin.sizeDelta / 2.4f) * clampedDir;
        }
    }

}
