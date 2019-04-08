using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCarController : CarController
{
    void Update()
    {
        // Used to accelerate
        m_verticalAxis = Input.GetAxis("Vertical");
        // Used to turn
        m_horizontalAxis = Input.GetAxis("Horizontal");
        // Used to drift
        m_driftInput = Mathf.Abs(Input.GetAxis("Drift")) > 0.01f;
    }
}
