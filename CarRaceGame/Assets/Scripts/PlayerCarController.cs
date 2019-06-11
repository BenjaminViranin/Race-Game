using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class PlayerCarController : CarController
{
    private float m_frontalDistance;
    public EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Stop;

    public Checkpoint currentCheckpoint;

    public int rank;
    public UnityEngine.UI.Text rankText;
    public bool isFinishedTheRace;

    // Looping Stuff
    private bool isOnLooping;
    private float distanceTravelled;
    private PathCreator RoadPath;
    private Vector3 oldPosition;
    private float loopRot = 90.0f;
    bool canStartFollowSP = false;
    private Vector3 scriptrdPathStartPos;
    private Quaternion scriptrdPathStartRot;
    private float scriptrdPathLoopLerpAlpha;

    public float timeStuck;
    private Vector3 oldPositionForStuck;

    void Start()
    {
        RoadPath = GameObject.Find("Road Path Manager").GetComponent<RoadPathManager>().roadPath_Looping.GetComponent<PathCreator>();

        oldPosition = transform.position;

        base.Start();
    }

    void RespawnToCheckpoint()
    {
        if (transform.position.y <= -1 || timeStuck >= 5.0f)
        {
            transform.position = currentCheckpoint.gameObject.transform.position;
            m_rigidbody.velocity = Vector3.zero;
            m_currentSpeed = 0;
            m_verticalAxis = 0;
            m_horizontalAxis = 0;
            timeStuck = 0.0f;
        }

        int oldX = (int)oldPositionForStuck.x;
        int oldY =(int)oldPositionForStuck.y;
        int oldZ =(int)oldPositionForStuck.z;
        int X = (int)transform.position.x;
        int Y = (int)transform.position.y;
        int Z = (int)transform.position.z;

        if (oldX == X && oldY == Y && oldZ == Z)
            timeStuck += Time.deltaTime;
        else
        {
            timeStuck = 0.0f;
        }
    }

    void Update()
    {
        rankText.text = rank.ToString();
        if (!isOnLooping)
        {
            // Used to accelerate
            m_verticalAxis = Input.GetAxis("Vertical");
            // Used to turn
            m_horizontalAxis = Input.GetAxis("Horizontal");
            // Used to drift
            m_driftInput = Mathf.Abs(Input.GetAxis("Drift")) > 0.01f;

            m_frontalDistance = GetComponent<CapsuleCollider>().height * 0.5f * transform.localScale.z;
        }
        else
            FollowPath();

        RespawnToCheckpoint();

        oldPositionForStuck = transform.position;
    }

    void FollowPath()
    {
        if (scriptrdPathLoopLerpAlpha == 0.0f)
        {
            scriptrdPathStartPos = transform.position;
            scriptrdPathStartRot = transform.rotation;
        }
        else if (scriptrdPathLoopLerpAlpha >= 1)
            canStartFollowSP = true;

        if (!canStartFollowSP)
        {
            Vector3 tempPos = Vector3.Lerp(scriptrdPathStartPos, RoadPath.StartPosition, scriptrdPathLoopLerpAlpha);
            transform.position = new Vector3(tempPos.x, transform.position.y, tempPos.z);

            transform.rotation = Quaternion.Slerp(scriptrdPathStartRot, RoadPath.StartRotation, scriptrdPathLoopLerpAlpha);

            scriptrdPathLoopLerpAlpha += 2.8f * Time.deltaTime;
        }
        else
        {
            distanceTravelled += (30 + 2) * Time.deltaTime;
            Vector3 pos = RoadPath.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
            Quaternion rot = RoadPath.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);

            transform.position = pos;
            transform.rotation = rot;

            if (distanceTravelled <= RoadPath.path.length / 2)
                transform.Rotate(0, 0, loopRot);
            else
            {
                transform.Rotate(0, 0, loopRot);
                loopRot -= loopRot > 0 ? loopRot - 1 * Time.deltaTime : 0;
            }
        }

        oldPosition = transform.position;

        if (distanceTravelled >= RoadPath.path.length)
        {
            isOnLooping = false;
        }
    }

    void FixedUpdate()
    {
        TestFrontalCollision();
        base.FixedUpdate();
    }

    private void TestFrontalCollision()
    {
        Debug.DrawRay(transform.position + m_distanceToFloor * transform.up, transform.forward * m_frontalDistance, Color.cyan);

        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0, m_distanceToFloor, 0), transform.forward, out hit, m_frontalDistance))
        {
            if (m_verticalAxis > 0 && !m_isOnRamp && !hit.collider.CompareTag("ItemBox"))
                m_currentSpeed = 0;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Looping Start")
        {
            isOnLooping = true;
        }

        if (other.gameObject.tag == "Respawn")
        {
            currentCheckpoint = other.gameObject.GetComponent<Checkpoint>();
        }
        
    }
}
