using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoverController : MonoBehaviour
{
    [SerializeField]
    Transform[] leftWheels;

    [SerializeField]
    Transform[] rightWheels;

    [SerializeField]
    GameObject center;

    private Rigidbody center_rb;
    private Transform center_t;

    private DifferentialDrive diff_drive;

    private State start_state;
    private State goal_state;

    private float R = 31.5f;
    private float L = 60f;

    void Start()
    {
        center_rb = center.GetComponent<Rigidbody>();
        center_t = center.GetComponent<Transform>();
        StartCoroutine(StartDiffDrive());
    }

    private float getRPM(float v)
    {
        return (v / (2.0f * (float)Math.PI * R));
    }

    private void setWheelAnimation(Transform[] wheels, float v)
    {
        float rpm = v; //getRPM(v);
        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(Vector3.forward, rpm * Time.deltaTime, Space.Self);
        }
    }

    private void uniToDiff(float v, float w, out float vR, out float vL)
    {
        vR = (2 * v + w * L) / (2 * R);
        vL = (2 * v - w * L) / (2 * R);
    }

    private void diffToUni(float vR, float vL, out float v, out float w)
    {
        v = R / 2 * (vR + vL);
        w = R / L * (vR - vL);
    }

    private void setVelocity(float v)
    {
        float appliedSpeed = Time.fixedDeltaTime * v;
        center_rb.AddRelativeForce(Vector3.right * appliedSpeed, ForceMode.VelocityChange);
    }

    private void setRotation(float w)
    {
        float appliedRotation = -1f * Time.fixedDeltaTime * w;
        center_rb.AddRelativeTorque(Vector3.up * appliedRotation, ForceMode.VelocityChange);
    }

    private void applyAction(float v, float w)
    {
        float vL, vR;
        uniToDiff(v, w, out vR, out vL);
        setWheelAnimation(leftWheels, vL*30);
        setWheelAnimation(rightWheels, vR*30);
        setVelocity(v);
        setRotation(w);
    }

    private State getCurrentState()
    {
        float x = center_t.position.x;
        float y = center_t.position.z;
        float theta = Mathf.Deg2Rad * center_t.localEulerAngles.y;
        State current_state = new State(new Vector2(x, y), theta);
        return current_state;
    }

    private IEnumerator StartDiffDrive()
    {
        start_state = getCurrentState();
        goal_state = new State(new Vector2(start_state.pos.x + 100f, start_state.pos.y), 0f);
        diff_drive = new DifferentialDrive(start_state, goal_state);
        float v, w;
        
        while (!diff_drive.hasArrived())
        {
            State curr_state = getCurrentState();
            diff_drive.step(curr_state, out v, out w);
            applyAction(v, w);
            yield return new WaitForSeconds(diff_drive.dt);
        }
        setVelocity(0);
        setRotation(0);
        yield break;
    }

    void Update()
    {

    }
}
