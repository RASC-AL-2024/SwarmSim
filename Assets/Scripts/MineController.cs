using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

class Battery {
  float chargeTime;
  float chargeDuration;

  public Battery(float chargeDuration_) {
    chargeTime = Time.time;
    chargeDuration = chargeDuration_;
  }

  public float charge() {
    return 1f - (Time.time - chargeTime) / chargeDuration;
  }

  public bool empty() {
    return charge() <= 0f;
  }
}

public class MineController : MonoBehaviour {
  [SerializeField]
  public Bounds[] resourceAreas;
  [SerializeField]
  public GameObject processingStation;
  [SerializeField]
  public float miningDuration; // seconds
  [SerializeField]
  public float processingDuration; // seconds
  [SerializeField]
  public float batteryDuration; // seconds

  private State processingState;

  private RoverController roverController;

  private Battery battery;
  private bool hasLoad = false; // unused for now

  private State stateInBounds(Bounds bounds) {
    return new State(new Vector2(bounds.center.x, bounds.center.z), 0);
  }

  void Start() {
    roverController = GetComponent<RoverController>();
    var t = processingStation.GetComponent<Transform>();
    processingState = new State(new Vector2(t.position.x + 2, t.position.z - 2), 0);
    StartCoroutine(Background());
  }

  private State[] trajectorySolve(State goalState) {
    // Nothing smart for now
    State[] trajectory = {goalState};
    return trajectory;
  }

  private T randomElement<T>(T[] arr) {
    return arr[UnityEngine.Random.Range(0, arr.Length)];
  }

  IEnumerator Background() {
    battery = new Battery(batteryDuration);

    while (true) {
      Debug.Log("Moving to mine");
      {
        var goalState = stateInBounds(randomElement(resourceAreas));
        var trajectory = trajectorySolve(goalState);
        foreach (var state in trajectory) {
          yield return roverController.waitWaypoint(state);
        }
      }

      Debug.Log("Mining");
      {
        yield return new WaitForSeconds(miningDuration);
      }
      hasLoad = true;

      Debug.Log("Moving to processing");
      {
        var trajectory = trajectorySolve(processingState);
        foreach (var state in trajectory) {
          yield return roverController.waitWaypoint(state);
        }
      }

      Debug.Log("Processing");
      battery = new Battery(batteryDuration);
      {
        yield return new WaitForSeconds(processingDuration);
      }
      hasLoad = false;
    }
  }

  void OnDrawGizmos() {
    foreach (var bounds in resourceAreas) {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
  }
}
