
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _objectToFollow;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private float _followSpeed = 10;
    [SerializeField] private float _lookSpeed = 10;

    void FixedUpdate()
    {
        LookAtTarget();
        MoveToTarget();
    }

    void LookAtTarget()
    {
        Vector3 lookDirection = _objectToFollow.position - transform.position;
        Quaternion rot = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, _lookSpeed * Time.deltaTime);
    }

    void MoveToTarget()
    {
        Vector3 targetPos = _objectToFollow.position +
                             _objectToFollow.forward * _offset.z +
                             _objectToFollow.right * _offset.x +
                             _objectToFollow.up * _offset.y;
        transform.position = Vector3.Lerp(transform.position, targetPos, _followSpeed * Time.deltaTime);
    }
}