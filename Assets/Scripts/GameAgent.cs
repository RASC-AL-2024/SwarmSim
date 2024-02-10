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

    public bool move_down = false;

    RoverState rover_state;
    
    /** Random number generator. */
    private Random m_random = new Random();
    State processingState;

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

    void wrongWay(Vector2 vel)
    {
        if (vel.sqrMagnitude < 0.01f) return;

        transform.forward = new Vector3(vel.x, 0, vel.y).normalized;
        Vector3 delta_vec = new Vector3(vel.x, 0, vel.y);
        transform.position += delta_vec * Time.deltaTime;
    }

    void updateRobotPosition(Vector2 vel)
    {
        if (vel.sqrMagnitude < 0.01f) return;

        Vector3 targetDirection3D = new Vector3(vel.x, 0, vel.y);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection3D, Vector3.up);

        if (areVectorsAlmostEqual(targetDirection3D, transform.forward, 0.01f))
        {
            Vector3 delta_vec = new Vector3(vel.x, 0, vel.y);
            transform.position += delta_vec * Time.deltaTime;
        } else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);
            float theta = transform.rotation.eulerAngles.y;
            Vector2 direction = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero); // maybe just set it in the direction?
        }
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
                yield return moveRover(goal_position);
            }

            PRINT("Mining");
            {
                yield return new WaitForSeconds(miningDuration);
            }
            rover_state.hasLoad = true;
            
            PRINT("Moving to processing");
            {
                Vector2 goal_position = new Vector2(processingState.pos.x, processingState.pos.y);
                yield return moveRover(goal_position);
            }

            PRINT("Processing");
            {
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

            updateRobotPosition(actual_vel);
            //wrongWay(actual_vel);

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

    void Update()
    {

    }
}