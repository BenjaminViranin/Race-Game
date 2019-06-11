using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class CarController : MonoBehaviour
{
    [SerializeField] protected float _gravityFactor = 10f;

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
    public bool m_isOnRamp;

    public Vector3 m_gravityVector;
    public Vector3 m_malusBuffer;
    public float m_colliderHeightAtStart;

    public float m_currentGravityFactor;

    public void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_distanceToFloor = GetComponent<CapsuleCollider>().radius * transform.localScale.y;
        m_colliderHeightAtStart = GetComponent<CapsuleCollider>().height;
        m_rigidbody.freezeRotation = true;
        m_rigidbody.useGravity = false;
        m_currentGravityFactor = _gravityFactor * 0.5f;
    }

    protected void FixedUpdate()
    {
        m_gravityVector.Set(0, 0, 0);

        AlignToGround();

        m_gravityVector += m_malusBuffer;

        if (m_isGrounded)
        {
            ApplyRotation();
            CalculateDrift();
            CalculateSpeed();

            Vector3 movVector = Vector3.Lerp(transform.forward, m_lateralVelocity, m_interpolationVelocity) * m_currentSpeed;
            if (!m_isOnRamp)
                m_gravityVector += -Vector3.up;

            m_rigidbody.velocity = movVector + m_gravityVector;
            m_currentGravityFactor = _gravityFactor * 0.5f;
        }
        else
        {
            m_isDrifting = false;
            m_driftInput = false;
            Vector3 movVector = transform.forward * m_currentSpeed;
            m_currentGravityFactor = Mathf.Lerp(m_currentGravityFactor, _gravityFactor, Time.fixedDeltaTime);
            m_gravityVector += -m_currentGravityFactor * Vector3.up;
            m_rigidbody.velocity = movVector + m_gravityVector;
        }
    }

    private void AlignToGround()
    {
        Debug.DrawRay(transform.position + m_distanceToFloor * transform.up, -transform.up * 1.2f, Color.yellow);

        RaycastHit hit;
        if (Physics.Raycast(transform.position + m_distanceToFloor * transform.up, -transform.up, out hit, Mathf.Infinity))
        {
            m_isGrounded = hit.distance <= 1.2f;

            m_isOnRamp = hit.collider.gameObject.CompareTag("Ramp") && m_isGrounded;

            if (m_isOnRamp)
            {
                m_gravityVector -= hit.normal;
                GetComponent<CapsuleCollider>().height = 0;
            }
            else
            {
                GetComponent<CapsuleCollider>().height = m_colliderHeightAtStart;
            }

            Vector3 lookDir = Vector3.Cross(hit.normal, -transform.right);
            Quaternion lookRotation = Quaternion.LookRotation(lookDir, hit.normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * (30 / Quaternion.Angle(transform.rotation, lookRotation)) * (m_isOnRamp ? 5 : 1));
        }
        else
        {
            m_isGrounded = false;
            Vector3 lookDir = Vector3.Cross(Vector3.up, -transform.right);
            Quaternion lookRotation = Quaternion.LookRotation(lookDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * (30 / Quaternion.Angle(transform.rotation, lookRotation)) * (m_isOnRamp ? 5 : 1));
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
//                    m_malusBuffer += transform.up;
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

    public void SpeedUp(float p_speedPower, float p_speedDuration)
    {
        StartCoroutine(SpeedUpCoroutine(p_speedPower, p_speedDuration));
    }

    public void HitByMalus(float p_malusPower)
    {
        StartCoroutine(HitByMalusCoroutine(p_malusPower));
    }

    IEnumerator SpeedUpCoroutine(float p_speedPower, float p_speedDuration)
    {
        _maxSpeed += p_speedPower;
        _acceleration += p_speedPower;

        for (int i = 0; i < p_speedDuration; ++i)
        {
            _maxSpeed -= p_speedPower / p_speedDuration;
            _acceleration -= p_speedPower / p_speedDuration;
            yield return new WaitForSeconds(.1f);
        }
    }

    IEnumerator HitByMalusCoroutine(float p_malusPower)
    {
        m_currentSpeed = 0.0f;
        m_isDrifting = false;
        m_driftInput = false;

        m_malusBuffer += Vector3.up * p_malusPower * 2;
        yield return new WaitForSeconds(0.5f);

        m_malusBuffer.Set(0, 0, 0);
    }

    public bool NoControle { get; set; }
}