using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MineController : MonoBehaviour
{
    [SerializeField]
    public Bounds[] resourceAreas;
    [SerializeField]
    public GameObject processingStation;
    [SerializeField]
    public float miningDuration; // seconds
    [SerializeField]
    public float processingDuration; // seconds
    [SerializeField]
    public int id;

    public RoverState rover_state;

    private State processingState;

    private RoverController roverController;

    private State stateInBounds(Bounds bounds)
    {
        return new State(randomStateInBounds(bounds), 0);
    }

    Vector2 randomStateInBounds(Bounds bounds)
    {
        float x_offset = 1f;
        float z_offset = 1f;

        float x = UnityEngine.Random.Range(bounds.min.x + x_offset, bounds.max.x - x_offset);
        float z = UnityEngine.Random.Range(bounds.min.z + z_offset, bounds.max.z - z_offset);
        return new Vector2(x, z);
    }

    void Start()
    {
        rover_state = new RoverState(id);
        roverController = GetComponent<RoverController>();

        var t = processingStation.GetComponent<Transform>();
        processingState = new State(new Vector2(t.position.x + 2, t.position.z - 2), 0);
        StartCoroutine(Background());
    }

    private State[] trajectorySolve(State goalState)
    {
        // Nothing smart for now
        State[] trajectory = { goalState };
        return trajectory;
    }

    private T randomElement<T>(T[] arr)
    {
        return arr[UnityEngine.Random.Range(0, arr.Length)];
    }

    private void PRINT(string str)
    {
        Debug.Log("Rover " + id + ": " + str);
    }

    IEnumerator Background()
    {
        // phsyics causes spawned rovers to move a bit in the beginning
        // wait for them to stop before calling RoverController
        yield return new WaitForSeconds(10f);

        while (true)
        {
            PRINT("Moving to mine");
            {
                var goalState = stateInBounds(randomElement(resourceAreas));
                var trajectory = trajectorySolve(goalState);
                foreach (var state in trajectory)
                {
                    yield return roverController.waitWaypoint(state);
                }
            }

            PRINT("Mining");
            {
                yield return new WaitForSeconds(miningDuration);
            }
            rover_state.hasLoad = true;

            PRINT("Moving to processing");
            {
                var trajectory = trajectorySolve(processingState);
                foreach (var state in trajectory)
                {
                    yield return roverController.waitWaypoint(state);
                }
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
}
