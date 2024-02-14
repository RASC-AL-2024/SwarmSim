using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using Random = System.Random;

using RStateType = RoverNode.State;

public class GameAgent : MonoBehaviour
{
    [HideInInspector] public int sid = -1;

    [SerializeField]
    public Bounds[] resourceAreas;
    [SerializeField]
    public GameObject processingStation;
    [SerializeField]
    public float miningDuration; // seconds
    [SerializeField]
    public float processingDuration; // seconds

    [SerializeField]
    public ArticulationBody[] leftWheels;
    [SerializeField]
    public ArticulationBody[] rightWheels;

    public RoverState rover_state;

    /** Random number generator. */
    State processingState;

    private float wheel_diameter = 0.336f;
    private float axle_width = 0.60f;
    private float maxwheel_velocity = 2; // rad/s

    private float target_velocity = 0f;
    private float target_angular_velocity = 0f;

    public MotionPlanner motion_planner;
    public TargetPlanner target_planner;

    void Start()
    {
        rover_state = new RoverState(sid);
        
        var t = processingStation.GetComponent<Transform>();
        processingState = new State(new Vector2(t.position.x, t.position.z), Quaternion.identity);

        initTargetPlanner();

        target_planner = new TargetPlanner(sid);
        motion_planner = new MotionPlanner(sid, transform);
    }

    private void initTargetPlanner()
    {
        TargetPlanner.resourceAreas = resourceAreas;
        TargetPlanner.processingStation = processingStation.transform;
        TargetPlanner.miningDuration = miningDuration;
        TargetPlanner.processingDuration = processingDuration;
    }

    private void updateWheels(Velocity current_velocity)
    {
        (float wL, float wR) = wheelVelocities(current_velocity.linear_vel, current_velocity.angular_vel);

        // Scale so we don't go too fast
        float d = Math.Max(Mathf.Abs(wR / maxwheel_velocity), Mathf.Abs(wL / maxwheel_velocity));
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

    private (float wL, float wR) wheelVelocities(float v, float w)
    {
        float s = 2 / wheel_diameter;
        return ((v - w * axle_width / 2) * s, (v + axle_width / 2 * w) * s);
    }

    private Vector2 get2dPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    State getCurrentState()
    {
        Vector2 position = new Vector2(transform.position.x, transform.position.z);
        Quaternion rot = transform.rotation;
        return new State(position, rot);
    }

    void updateRoverState(RStateType state_type)
    {
        bool has_load = state_type == RStateType.MOVING_TO_PROCESSING;
        rover_state.updateHasLoad(has_load);

        bool is_moving = target_planner.getIsMoving();
        rover_state.updateBattery(is_moving);

        var curr_state = getCurrentState();
        rover_state.updateState(curr_state);
    }

    void Update()
    {
        target_planner.step(get2dPosition());

        if (target_planner.getIsMoving())
        {
            Simulator.Instance.setAgentIsMoving(sid, true);
            Vector2 goal_position = target_planner.getGoalPosition();
            Velocity curr_vel = motion_planner.step(goal_position);
            updateWheels(curr_vel);
        } else
        {
            Velocity zero_velocity = new Velocity(0, 0);
            updateWheels(zero_velocity);
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
            Simulator.Instance.setAgentIsMoving(sid, false);
        }

        if(target_planner.isValidState())
            updateRoverState(target_planner.getCurrentState());
    }

    void OnDrawGizmos()
    {
        foreach (var bounds in resourceAreas)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}