#define NO_PHYSICS

using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using Random = System.Random;

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

    RoverState rover_state;

    /** Random number generator. */
    private Random m_random = new Random();
    State processingState;

    private float wheel_diameter = 0.336f;
    private float axle_width = 0.60f;
    private float maxwheel_velocity = 2; // rad/s

    private float target_velocity = 0f;
    private float target_angular_velocity = 0f;

    private Velocity current_velocity;

    private State stateInBounds(Bounds bounds)
    {
        return new State(randomStateInBounds(bounds), 0);
    }

    Vector2 randomStateInBounds(Bounds bounds)
    {
        float xoffset = 1f;
        float z_offset = 1f;

        float x = UnityEngine.Random.Range(bounds.min.x + xoffset, bounds.max.x - xoffset);
        float z = UnityEngine.Random.Range(bounds.min.z + z_offset, bounds.max.z - z_offset);
        return new Vector2(x, z);
    }

    void Start()
    {
        rover_state = new RoverState(sid);

        var t = processingStation.GetComponent<Transform>();
        processingState = new State(new Vector2(t.position.x, t.position.z), 0);
        StartCoroutine(Background());
    }

    Vector2 getCurrPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    Vector2 getGoalVector(Vector2 goal_position)
    {
        return goal_position - getCurrPosition();
    }

    public bool areVectorsAlmostEqual(Vector3 a, Vector3 b, float tolerance)
    {
        Vector3 diff = (a - b);
        return diff.magnitude < tolerance;
    }

    float getAngularVelocity(Quaternion from_rotation, Quaternion to_rotation)
    {
        Vector3 fromDirection = from_rotation * Vector3.forward;
        Vector3 toDirection = to_rotation * Vector3.forward;

        fromDirection.y = 0;
        toDirection.y = 0;
        fromDirection.Normalize();
        toDirection.Normalize();

        float angle = Vector3.SignedAngle(fromDirection, toDirection, Vector3.up);
        float angular_velocity = -angle / Time.deltaTime;
        return angular_velocity;
    }

    Velocity getRobotVel(Vector2 vel)
    {
        if (vel.sqrMagnitude < 0.01f) return new Velocity(0, 0);

        Vector3 targetDirection3D = new Vector3(vel.x, 0, vel.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection3D, Vector3.up);

        float angular_velocity = 0f;
        float linear_velocity = 0f;

        if (areVectorsAlmostEqual(targetDirection3D, transform.forward, 0.3f))
        {
            Vector3 delta_vec = new Vector3(vel.x, 0, vel.y);
            angular_velocity = 0f;
            linear_velocity = 1f;
#if NO_PHYSICS
            transform.position += delta_vec * Time.deltaTime;
#endif

        }
        else
        {
            Quaternion new_rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);
            Quaternion from_rotation = transform.rotation;
            angular_velocity = getAngularVelocity(from_rotation, new_rotation);
            linear_velocity = 0.5f;

#if NO_PHYSICS
            Vector3 direction = transform.rotation * Vector3.forward;
            transform.rotation = new_rotation;
            transform.position += direction * Time.deltaTime;
#endif

        }
        return new Velocity(linear_velocity, angular_velocity);
    }

    private T randomElement<T>(T[] arr)
    {
        return arr[UnityEngine.Random.Range(0, arr.Length)];
    }

    private void PRINT(string str)
    {
        Debug.Log("Rover " + sid + ": " + str);
    }

    IEnumerator Background()
    {
        while (true)
        {
            PRINT("Moving to mine");
            {
                var goal_state = stateInBounds(randomElement(resourceAreas));
                Vector2 goal_position = new Vector2(goal_state.pos.x, goal_state.pos.y);
                Simulator.Instance.setAgentIsMoving(true);
                yield return moveRover(goal_position);
            }

            PRINT("Mining");
            {
                updateWheels(new Velocity(0, 0));
                Simulator.Instance.setAgentIsMoving(false);
                yield return new WaitForSeconds(miningDuration);
            }
            rover_state.hasLoad = true;

            PRINT("Moving to processing");
            {
                Vector2 goal_position = new Vector2(processingState.pos.x, processingState.pos.y);
                Simulator.Instance.setAgentIsMoving(true);
                yield return moveRover(goal_position);
            }

            PRINT("Processing");
            {
                updateWheels(new Velocity(0, 0));
                Simulator.Instance.setAgentIsMoving(false);
                yield return new WaitForSeconds(processingDuration);
            }

            rover_state.hasLoad = false;
        }
    }

    void OnDrawGizmos()
    {
        foreach (var bounds in resourceAreas)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }

    IEnumerator moveRover(Vector2 goal_position)
    {
        while (true)
        {
            Vector2 goalVector = getGoalVector(goal_position);
            if (RVOMath.absSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.normalize(goalVector);
            }
            else
            {
                yield break;
            }

            Vector2 actual_vel = Simulator.Instance.getAgentVelocity(sid);
            Vector2 pos = Simulator.Instance.getAgentPosition(sid);
            Vector2 vel = Simulator.Instance.getAgentPrefVelocity(sid);

            Velocity robot_vel = getRobotVel(actual_vel);
            updateWheels(robot_vel);

            Vector2 real_pos = new Vector2(transform.position.x, transform.position.z);
            Simulator.Instance.setAgentPosition(sid, real_pos);

            Simulator.Instance.setAgentPrefVelocity(sid, goalVector);

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */
            float angle = (float)m_random.NextDouble() * 2f * (float)Math.PI;
            float dist = (float)m_random.NextDouble() * 0.0001f;

            Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
                                                         dist *
                                                         new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void updateWheels(Velocity current_velocity)
    {
#if NO_PHYSICS
        return;
#endif
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

    void Update()
    {

    }
}