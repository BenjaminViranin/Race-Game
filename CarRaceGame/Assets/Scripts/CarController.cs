using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [SerializeField] protected float _gravity = 1f;

    [Header("Car Properties")]
    [SerializeField] protected float _maxSpeed = 30f;
    [SerializeField] protected float _acceleration = 10f;
    [SerializeField] protected float _rotationSensibility = 1f;
    [SerializeField, Tooltip("How much the drift will make the car turn")] protected float _driftInfluence = 1f;
    [SerializeField] protected float _afterDriftBonusSpeed = 20f;
    [SerializeField, Tooltip("In milliseconds")] protected float _afterDriftBonusDuration = 10f;
    [SerializeField, Range(0f, 100f), Tooltip("Percent of the max speed to start a drift")] protected float _percentOfSpeedToDrift = 80f;

    [Header("Wheels")]
    [SerializeField] protected Transform _wheelFrontLeftT;
    [SerializeField] protected Transform _wheelFrontRightT;
    [SerializeField] protected Transform _wheelBackLeftT;
    [SerializeField] protected Transform _wheelBackRightT;


    protected Rigidbody m_rigidbody;
    protected float m_distanceToFloor;

    [Header("Dev data ")]
    public float m_verticalAxis;
    public float m_horizontalAxis;
    public bool m_driftInput;

    public float m_currentSpeed = 0;
    public Vector3 m_lateralVelocity;
    public float m_interpolationVelocity = 0;
    public float m_driftState;
    public bool m_isDrifting = false;

    public bool m_isGrounded;

    public Vector3 data;


    protected void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        //m_distanceToFloor = GetComponent<BoxCollider>().size.y * 0.5f;
        m_distanceToFloor = GetComponent<CapsuleCollider>().radius * 0.5f + 0.1f;
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity))
        {
            m_isGrounded = hit.distance <= m_distanceToFloor;

            //transform.eulerAngles = new Vector3(Mathf.Lerp(transform.eulerAngles.x, hit.transform.eulerAngles.x, Time.fixedDeltaTime), transform.eulerAngles.y, 0);
            if (hit.collider.gameObject.CompareTag("Ramp"))
            {
                //Debug.Log("Oui");
            }

            float lerpFirstValue = 0;

            if (hit.transform.eulerAngles.x > 180f)
            {
                if (transform.eulerAngles.x < 180f)
                {
                    lerpFirstValue = transform.eulerAngles.x + 360f;
                }
                else
                {
                    lerpFirstValue = transform.eulerAngles.x;
                }
            }
            else
            {
                if (transform.eulerAngles.x < 180f)
                {
                    lerpFirstValue = transform.eulerAngles.x;
                }
                else
                {
                    lerpFirstValue = transform.eulerAngles.x - 360f;
                }
            }

            data.x = transform.eulerAngles.x;
            data.y = hit.transform.eulerAngles.x;
            data.z = lerpFirstValue;

            transform.eulerAngles = new Vector3(Mathf.Lerp(lerpFirstValue, hit.transform.eulerAngles.x, Mathf.Pow(15f / Mathf.Abs(lerpFirstValue - hit.transform.eulerAngles.x), 2f) * Time.fixedDeltaTime), transform.eulerAngles.y, 0);

            Debug.DrawRay(transform.position, -transform.up * hit.distance, Color.yellow);
        }
        else
            m_isGrounded = false;

        if (m_isGrounded)
        {
            ApplyRotation();
            CalculateDrift();
            CalculateSpeed();

            Vector3 movVector = Vector3.Lerp(transform.forward, m_lateralVelocity, m_interpolationVelocity) * m_currentSpeed;
            m_rigidbody.velocity = movVector + new Vector3(0, m_rigidbody.velocity.y - movVector.y, 0);
        }
        else if (m_isDrifting)
        {
            ApplyRotation();
            CalculateDrift();

            Vector3 movVector = Vector3.Lerp(transform.forward, m_lateralVelocity, m_interpolationVelocity) * m_currentSpeed;
            m_rigidbody.velocity = movVector + new Vector3(0, m_rigidbody.velocity.y - movVector.y, 0);
        }
        else
        {
            Vector3 movVector = transform.forward * m_currentSpeed;
            m_rigidbody.velocity = movVector + new Vector3(0, m_rigidbody.velocity.y - movVector.y - _gravity, 0);
        }
    }

    void ApplyRotation()
    {
        if (Mathf.Abs(m_horizontalAxis) > 0.01f)
        {
            transform.Rotate(new Vector3(0, m_horizontalAxis * _rotationSensibility, 0));

            _wheelFrontLeftT.eulerAngles = new Vector3(0, m_horizontalAxis * 45, 0) + transform.eulerAngles;
            _wheelFrontRightT.eulerAngles = new Vector3(0, m_horizontalAxis * 45, 0) + transform.eulerAngles;
        }

        if (m_isDrifting)
            transform.Rotate(m_driftState * new Vector3(0, -(Mathf.Pow(m_interpolationVelocity, 3) + 1f), 0));
    }

    private void CalculateDrift()
    {
        if (m_driftInput &&
            Mathf.Abs(m_driftState) < 0.2f &&
            Mathf.Abs(m_currentSpeed) > _maxSpeed * _percentOfSpeedToDrift * 0.01f)
        {
            if (!m_isDrifting && Mathf.Abs(m_horizontalAxis) > 0.01f)
            {
                StartCoroutine(DriftCoroutine());
            }
        }
        else
        {
            if (!m_isDrifting)
            {
                m_driftState *= 0.95f;
                if (Mathf.Abs(m_driftState) < 0.01f)
                    m_driftState = 0;

                m_interpolationVelocity -= 0.02f;
                m_interpolationVelocity = Mathf.Max(0, m_interpolationVelocity);
            }
        }

        m_lateralVelocity = transform.right * m_driftState * _driftInfluence;
    }

    void CalculateSpeed()
    {
        if (Mathf.Abs(m_verticalAxis) > 0.01f)
        {
            if (m_verticalAxis * m_currentSpeed < 0)
                m_currentSpeed *= 0.9f;
            else
            {
                m_currentSpeed += m_verticalAxis * Time.fixedDeltaTime * _acceleration;
                m_currentSpeed = m_currentSpeed > 0
                    ? Mathf.Min(m_currentSpeed, _maxSpeed)
                    : Mathf.Max(m_currentSpeed, -_maxSpeed);
            }
        }
        else
        {
            m_currentSpeed -= _acceleration * Time.fixedDeltaTime * Mathf.Sign(m_currentSpeed);
        }

        if (Mathf.Abs(m_currentSpeed) < 0.1f)
            m_currentSpeed = 0;

        _wheelBackRightT.Rotate(m_currentSpeed * new Vector3(1, 0, 0));
        _wheelBackLeftT.Rotate(m_currentSpeed * new Vector3(1, 0, 0));
    }

    IEnumerator DriftCoroutine()
    {
        while (m_driftInput)
        {
            if (Mathf.Abs(m_horizontalAxis) > 0.01f)
            {
                if (!m_isDrifting && m_isGrounded)
                {
                    m_driftState = -1 * Mathf.Sign(m_horizontalAxis);
                    m_rigidbody.AddForce(transform.up * _maxSpeed * 3);
                    m_isDrifting = true;
                }

                m_interpolationVelocity += 0.02f;
                m_interpolationVelocity = Mathf.Min(0.4f, m_interpolationVelocity);
            }

            yield return null;
        }

        m_isDrifting = false;

        if (m_interpolationVelocity > 0.39)
        {
            _maxSpeed += _afterDriftBonusSpeed;
            _acceleration += _afterDriftBonusSpeed;
            StartCoroutine(AfterDriftCoroutine());
        }
    }

    IEnumerator AfterDriftCoroutine()
    {
        for (int i = 0; i < _afterDriftBonusDuration; ++i)
        {
            _maxSpeed -= _afterDriftBonusSpeed / _afterDriftBonusDuration;
            _acceleration -= _afterDriftBonusSpeed / _afterDriftBonusDuration;
            yield return new WaitForSeconds(.1f);
        }
    }

    public bool NoControle { get; set; }
}