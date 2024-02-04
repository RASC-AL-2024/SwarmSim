using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoverController2 : MonoBehaviour
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

  private float wheel_diameter = 0.336f;
  private float axle_width = 0.60f;
  private float max_wheel_velocity = 1; // rad/s

  private float target_velocity = 0f;
  private float target_angular_velocity = 0f;

  private (float wL, float wR) wheelVelocities(float v, float w) {
    float s = 2 / wheel_diameter;
    return ((v - w * axle_width / 2) * s, (v + axle_width / 2 * w) * s);
  }

  void Start() {
    center_t = center.GetComponent<Transform>();
    StartCoroutine(StartDiffDrive());
  }

  private State getCurrentState() {
    float x = center_t.position.x;
    float y = center_t.position.z;
    float theta = -Mathf.Deg2Rad * center_t.localEulerAngles.y;
    State current_state = new State(new Vector2(x, y), theta);
    return current_state;
  }

  private IEnumerator StartDiffDrive() {
    start_state = getCurrentState();
    goal_state = new State(new Vector2(start_state.pos.x + 10f, start_state.pos.y + 10f), (float)Math.PI);
    diff_drive = new DifferentialDrive(start_state, goal_state);
    
    while (!diff_drive.hasArrived()) {
      State curr_state = getCurrentState();
      (target_velocity, target_angular_velocity) = diff_drive.step(curr_state);
      Debug.LogFormat("v: {0}, w: {1}", target_velocity, target_angular_velocity);
      yield return new WaitForSeconds(0.01f);
    }
    target_velocity = 0;
    target_angular_velocity = 0;
    yield break;
  }

  float constrainVelocity(float v) {
    return Math.Sign(v) * Mathf.Min(max_wheel_velocity, Mathf.Abs(v));
  }

  void Update() {
    (float wL, float wR) = wheelVelocities(target_velocity, target_angular_velocity); 

    Debug.LogFormat("l: {0}, r: {1}", wL, wR);
    float d = Math.Max(Mathf.Abs(wR / max_wheel_velocity), Mathf.Abs(wL / max_wheel_velocity));
    d = d > 1f ? d : 1f;
    Debug.LogFormat("d: {0}", d);
    Debug.LogFormat("dl: {0}, dr: {1}", Mathf.Rad2Deg * wL / d, Mathf.Rad2Deg * wR / d);

    // These target velocities are in deg/s
    foreach (var wheel in leftWheels) {
      var drive = wheel.xDrive;
      drive.targetVelocity = Mathf.Rad2Deg * wL / d;
      wheel.xDrive = drive;
    }
    foreach (var wheel in rightWheels) {
      var drive = wheel.xDrive;
      drive.targetVelocity = Mathf.Rad2Deg * wR / d;
      wheel.xDrive = drive;
    }
  }
}
