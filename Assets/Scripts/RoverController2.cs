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

    private float wheel_diameter = 0.315f;
    private float axle_width = 0.60f;

    private float max_wheel_speed = 2.00f; // linear m/s
    private float max_wheel_acceleration = 0.50f; // linear m/s^2

    private float current_left_velocity = 0f;
    private float current_right_velocity = 0f;

    private float target_velocity = 0f;
    private float target_angular_velocity = 0f;

    void Start()
    {
        center_rb = center.GetComponent<Rigidbody>();
        center_t = center.GetComponent<Transform>();
        StartCoroutine(StartDiffDrive());
    }

    private float getRadPerSecond(float velocity)
    {
        return (velocity / ((float)Math.PI * wheel_diameter));
    }

    private void setWheelAnimation(Transform[] wheels, float v, bool inverted)
    {
        float rad_s = getRadPerSecond(v);
        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(Vector3.forward, (inverted ? -1 : 1) * Mathf.Rad2Deg * rad_s * Time.deltaTime, Space.Self);
        }
    }

    private (float left_velocity, float right_velocity) wheelVelocities(float v, float w)
    {
        return (v - axle_width / 2 * w, v + axle_width / 2 * w);
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

    private float limitVelocityChange(float current_velocity, float target_velocity, float dt) {
      float acceleration = Mathf.Clamp((target_velocity - current_velocity) / dt, -max_wheel_acceleration, max_wheel_acceleration);
      float new_velocity = Mathf.Clamp(current_velocity + acceleration * dt, -max_wheel_speed, max_wheel_speed);
      return new_velocity;
    }

    private float currentVelocity() {
      return (current_left_velocity + current_right_velocity) / 2;
    }
    private float currentAngularVelocity() {
      return (current_right_velocity - current_left_velocity) / axle_width;
    }

    private State getCurrentState()
    {
        float x = center_t.position.x;
        float y = center_t.position.z;
        float theta = -Mathf.Deg2Rad * center_t.localEulerAngles.y;
        State current_state = new State(new Vector2(x, y), theta);
        return current_state;
    }

    private IEnumerator StartDiffDrive()
    {
        start_state = getCurrentState();
        goal_state = new State(new Vector2(start_state.pos.x + 10f, start_state.pos.y + 10f), (float)Math.PI);
        diff_drive = new DifferentialDrive(start_state, goal_state);
        
        while (!diff_drive.hasArrived())
        {
            State curr_state = getCurrentState();
            (target_velocity, target_angular_velocity) = diff_drive.step(curr_state);
            yield return new WaitForSeconds(0.01f);
        }
        target_velocity = 0;
        target_angular_velocity = 0;
        yield break;
    }

    void Update() {
        (float vL, float vR) = wheelVelocities(target_velocity, target_angular_velocity); 
        (float target_left_velocity, float target_right_velocity) = wheelVelocities(target_velocity, target_angular_velocity);
        float dt = Time.deltaTime;
        current_left_velocity = limitVelocityChange(current_left_velocity, target_left_velocity, dt);
        current_right_velocity = limitVelocityChange(current_right_velocity, target_right_velocity, dt);

        setWheelAnimation(leftWheels, current_left_velocity, true);
        setWheelAnimation(rightWheels, current_right_velocity, false);

        setVelocity(currentVelocity());
        setRotation(currentAngularVelocity());
    }
}
