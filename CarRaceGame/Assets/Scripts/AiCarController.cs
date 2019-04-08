using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

[RequireComponent(typeof(Rigidbody))]
public class AiCarController : CarController
{
    public PathCreator RoadPath;
    private EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop;

    private float distanceTravelled = 4.0f;
    private float targetRange = 10.0f;
    private Transform target;
    private Vector3 oldPosition;
    private float oldDistanceToTarget;

    private float editorMaxSpeed;
    private float aiMaxSpeed = 15.0f;

    private float rayLeftHitTimer;
    private float rayRightHitTimer;

    public float AngleToDrift = 45.0f;

    public Transform CarFront;
    public float FOV_Range = 10.0f;
    public float FOV_Angle = 45.0f;
    private bool fovHasContact;
    private Vector3 obstacleHitPoint;
    private bool isManagingTurn;

    public float angleCarForward_TargetForward;

    void Start()
    {
        oldPosition = transform.position;

        editorMaxSpeed = _maxSpeed;
        _maxSpeed = aiMaxSpeed;

        var empty = new GameObject();
        target = empty.transform;

        base.Start();
    }

    void Update()
    {
        if (!ManageObstacle())
            FollowTarget();
        else
        {
            // Debug
            Debug.DrawLine(new Vector3(target.position.x - 1, target.position.y, target.position.z), new Vector3(target.position.x + 1, target.position.y, target.position.z), Color.red);
            Debug.DrawLine(new Vector3(target.position.x, target.position.y, target.position.z - 1), new Vector3(target.position.x, target.position.y, target.position.z + 1), Color.red);
        }
    }

    void FollowTarget()
    {
        // Calculate Target position
        Vector3 dir = transform.position - oldPosition;
        if (m_verticalAxis > 0 && Vector3.Angle(transform.forward, m_rigidbody.velocity) < 80.0f)
            distanceTravelled += oldDistanceToTarget < targetRange ? dir.magnitude : dir.magnitude * 0.7f;

        oldPosition = transform.position;
        target.position = RoadPath.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
        
        // Calculate Target Rotation
        target.rotation = RoadPath.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
        angleCarForward_TargetForward = Vector3.Angle(CarFront.forward, target.forward);

        // Debug
        Debug.DrawLine(new Vector3(target.position.x - 1, target.position.y, target.position.z), new Vector3(target.position.x + 1, target.position.y, target.position.z), oldDistanceToTarget < targetRange ? Color.yellow : Color.white);
        Debug.DrawLine(new Vector3(target.position.x, target.position.y, target.position.z - 1), new Vector3(target.position.x, target.position.y, target.position.z + 1), oldDistanceToTarget < targetRange ? Color.yellow : Color.white);

        // Calculate Direction
        Vector3 direction = target.position - transform.position;
        oldDistanceToTarget = direction.magnitude;
        direction.Normalize();

        // Calculate Angle between target and self
        float rotationAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);

        // Set Input
        if (!isManagingTurn)
        { 
            m_verticalAxis = 1;
            m_horizontalAxis = rotationAngle / 180.0f >= 0 ? 1 : -1;
        }
        
        // Manage Turn
        if (rotationAngle > AngleToDrift)
            m_driftInput = true;
        else
            m_driftInput = false;

        // Debug
        //Debug.DrawRay(new Vector3(target.position.x, target.position.y + 2.0f, target.position.z), target.forward * 10.0f, Color.blue);
    }

    bool ManageObstacle()
    {
        Vector3 rayFront = CarFront.forward;
        Vector3 rayLeft = Quaternion.AngleAxis(-FOV_Angle, CarFront.up) * rayFront;
        Vector3 rayRight = Quaternion.AngleAxis(FOV_Angle, CarFront.up) * rayFront;

        Vector3 targetDirection = target.position - CarFront.position;
        float targetDistance = targetDirection.magnitude;
        targetDirection.Normalize();

        RaycastHit rayFrontHit, rayLeftHit, rayRightHit, rayTargetHit;
        bool rayFrontHasHit = Physics.Raycast(CarFront.position, rayFront, out rayFrontHit, FOV_Range);
        bool rayLeftHasHit = Physics.Raycast(CarFront.position, rayLeft, out rayLeftHit, FOV_Range);
        bool rayRightHasHit = Physics.Raycast(CarFront.position, rayRight, out rayRightHit, FOV_Range);
        bool rayTargetHasHit = Physics.Raycast(CarFront.position, targetDirection, out rayTargetHit, targetDistance + FOV_Range);
        if (Vector3.Angle(rayFront, targetDirection) > 40.0f || targetDistance > targetRange * 1.5f)
            rayTargetHasHit = false;

        // FOV check
        if (!fovHasContact)
        {
            obstacleHitPoint = rayFrontHasHit ? rayFrontHit.transform.position
                               : rayLeftHasHit ? rayLeftHit.transform.position
                               : rayRightHasHit ? rayRightHit.transform.position
                               : Vector3.zero;
        }

        if (obstacleHitPoint != Vector3.zero)
        {
            Vector3 self_obstacle_dir = obstacleHitPoint - CarFront.position;
            float self_obstacle_distance = self_obstacle_dir.magnitude;
            float self_obstacle_angle = Vector3.Angle(CarFront.forward, self_obstacle_dir.normalized);

            if (self_obstacle_distance <= FOV_Range && self_obstacle_angle <= FOV_Angle * 0.25f)
                fovHasContact = true;
            else
                fovHasContact = false;
        }

        // Debug
        if (fovHasContact) Debug.DrawLine( new Vector3( CarFront.position.x, CarFront.position.y + 0.75f, CarFront.position.z), 
                                           new Vector3(obstacleHitPoint.x, obstacleHitPoint.y + 0.75f, obstacleHitPoint.z), Color.blue);

        if (rayFrontHasHit || rayLeftHasHit || rayRightHasHit || rayTargetHasHit || fovHasContact)
        {
            isManagingTurn = true;
            // Debug
            Debug.DrawRay(CarFront.position, rayFront * FOV_Range, rayFrontHasHit ? Color.yellow : Color.white);
            Debug.DrawRay(CarFront.position, rayLeft * FOV_Range, rayLeftHasHit ? Color.yellow : Color.white);
            Debug.DrawRay(CarFront.position, rayRight * FOV_Range, rayRightHasHit ? Color.yellow : Color.white);
            if (rayTargetHasHit) Debug.DrawRay(CarFront.position, targetDirection * (FOV_Range + targetDistance) , Color.green);

            // If there is e Straight line -> Go Faster
            RaycastHit rayInfiniteFrontHit;
            Physics.Raycast(CarFront.position, rayFront, out rayInfiniteFrontHit, Mathf.Infinity);
            if (rayInfiniteFrontHit.distance >= FOV_Range * 3 && angleCarForward_TargetForward <= 4.0f)
            {
                _maxSpeed = editorMaxSpeed;
                // Debug
                Debug.DrawRay(CarFront.position, rayFront * (FOV_Range * 3), Color.magenta);
            }
            else
                _maxSpeed = aiMaxSpeed;

            // If obstacle close -> Turn
            bool tempLeftHit = Physics.Raycast(CarFront.position, rayLeft, out rayLeftHit, FOV_Range);
            bool tempRightHit = Physics.Raycast(CarFront.position, rayRight, out rayRightHit, FOV_Range);

            if (tempLeftHit && tempRightHit)
                m_horizontalAxis = rayLeftHit.distance == Mathf.Max(rayLeftHit.distance, rayRightHit.distance) ? -1 : 1;
            else if (tempLeftHit || tempRightHit)
                m_horizontalAxis = tempLeftHit ? 1 : -1;
            else
                return false;

            // If Stuck -> Go back
            if (rayFrontHasHit && rayFrontHit.distance < 1)
            {
                if (m_currentSpeed > 0)
                    m_currentSpeed *= 0.5f;
                m_horizontalAxis = 0;
                m_verticalAxis = -1;
            }
            else if (rayLeftHasHit && rayLeftHit.distance < 1)
            {
                rayLeftHitTimer += Time.deltaTime;
                if (rayLeftHitTimer > 1.5f)
                {
                    if (m_currentSpeed > 0)
                        m_currentSpeed *= 0.5f;
                    m_horizontalAxis = 0;
                    m_verticalAxis = -1;
                }
            }
            else if (rayRightHasHit && rayRightHit.distance < 1)
            {
                rayRightHitTimer += Time.deltaTime;
                if (rayRightHitTimer > 1.5f)
                {
                    if (m_currentSpeed > 0)
                        m_currentSpeed *= 0.5f;
                    m_horizontalAxis = 0;
                    m_verticalAxis = -1;
                }
            }
            else
            {
                rayLeftHitTimer = 0.0f;
                rayRightHitTimer = 0.0f;
                m_verticalAxis = 1;
            }

            // If going to Obstacle too fast -> Brake
            if (rayFrontHasHit && m_currentSpeed >= _maxSpeed * 0.85f)
                m_verticalAxis = -1;

            if (rayTargetHasHit && !rayFrontHasHit && !rayLeftHasHit && !rayRightHasHit)
                return false;
            else
                return true;
        }
        else
        {
            isManagingTurn = false;
            // Debug
            Debug.DrawRay(CarFront.position, rayFront * FOV_Range, Color.white);
            Debug.DrawRay(CarFront.position, rayLeft * FOV_Range, Color.white);
            Debug.DrawRay(CarFront.position, rayRight * FOV_Range, Color.white);
            //Debug.DrawRay(CarFront.position, targetDirection * (FOV_Range + targetDistance), Color.white);

            return false;
        }
    }
}