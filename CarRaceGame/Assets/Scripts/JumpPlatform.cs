using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpPlatform : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider p_collider)
    {
        if (p_collider.gameObject.CompareTag("Car"))
        {
            p_collider.GetComponent<CarController>().NoControle = true;
        }
    }

    void OnTriggerExit(Collider p_collider)
    {
        if (p_collider.gameObject.CompareTag("Car"))
        {
            p_collider.GetComponent<CarController>().NoControle = false;
        }
    }
}
