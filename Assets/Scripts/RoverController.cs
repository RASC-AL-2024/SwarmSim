using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoverController : MonoBehaviour
{
    [SerializeField]
    public ArticulationBody[] leftWheels;
    [SerializeField]
    public ArticulationBody[] rightWheels;

    [SerializeField]
    GameObject center;

    private Transform center_t;

    private DifferentialDrive diff_drive;

    private State start_state;
    private State goal_state;
    private State current_state;
    private Velocity current_velocity;

    private float wheel_diameter = 0.336f;
    private float axle_width = 0.60f;
    private float max_wheel_velocity = 2; // rad/s

    private float target_velocity = 0f;
    private float target_angular_velocity = 0f;

    public int id;


    private (float wL, float wR) wheelVelocities(float v, float w)
    {
        float s = 2 / wheel_diameter;
        return ((v - w * axle_width / 2) * s, (v + axle_width / 2 * w) * s);
    }

    void Start()
    {
        center_t = center.GetComponent<Transform>();
        current_velocity = new Velocity();
        current_state = getCurrentState();
    }

    private State getCurrentState()
    {
        float x = center_t.position.x;
        float y = center_t.position.z;
        float theta = Mathf.Deg2Rad * center_t.localEulerAngles.y; // we don't really need theta anymore

        State current_state = new State(new Vector2(x, y), theta);
        current_state.rot = center_t.rotation;
        return current_state;
    }

    private bool closeEnough(State goal_state)
    {
        float min_dist = 5f;
        float dist = Vector2.Distance(goal_state.pos, current_state.pos);
        return dist < min_dist;
    }

    private IEnumerator generateDWAPlan(State goal_state)
    {
        while (!closeEnough(goal_state))
        {
            current_state = getCurrentState();
            current_velocity = DWA.DWAPlanner.Planning(current_state, current_velocity, goal_state.pos, PointCloud.g_point_cloud, id);
            PointCloud.g_point_cloud.points[id] = current_state.pos;
            yield return new WaitForSeconds(0.1f);
        }
        current_velocity.linear_vel = 0f;
        current_velocity.angular_vel = 0f;
        yield break;
    }

    public IEnumerator waitWaypoint(State goal_state)
    {
        yield return StartCoroutine(generateDWAPlan(goal_state));
    }

    float constrainVelocity(float v)
    {
        return Math.Sign(v) * Mathf.Min(max_wheel_velocity, Mathf.Abs(v));
    }

    void Update()
    {
        (float wL, float wR) = wheelVelocities(current_velocity.linear_vel, current_velocity.angular_vel);

        // Scale so we don't go too fast
        float d = Math.Max(Mathf.Abs(wR / max_wheel_velocity), Mathf.Abs(wL / max_wheel_velocity));
        d = d > 1f ? d : 1f;

        // These target velocities are in deg/s
        foreach (var wheel in leftWheels)
        {
            var drive = wheel.xDrive;
            drive.targetVelocity = Mathf.Rad2Deg * wL / d;
            wheel.xDrive = drive;
        }
        foreach (var wheel in rightWheels)
        {
            var drive = wheel.xDrive;
            drive.targetVelocity = Mathf.Rad2Deg * wR / d;
            wheel.xDrive = drive;
        }
    }
}
