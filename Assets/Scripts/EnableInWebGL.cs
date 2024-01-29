using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableInWebGL : MonoBehaviour
{
    public GameObject target;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_WEBGL
        target.SetActive(true);
#else
        target.SetActive(false);
#endif
    }
}
