using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmJoint : MonoBehaviour {
    public Vector3 StartOffset;
    private Transform _transform;
    public char _rotationAxis;
    public Vector3 RotationAxis;

    private void Awake() {
        _transform = this.transform;
        StartOffset = _transform.localPosition;
        switch (_rotationAxis)
        {
            case 'x':
                RotationAxis = Vector3.right;
                break;
            case 'y':
                RotationAxis = Vector3.up;
                break;
            case 'z':
                RotationAxis = Vector3.forward;
                break;
        }
    }

}