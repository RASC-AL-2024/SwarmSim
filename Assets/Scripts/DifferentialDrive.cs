using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PidController {
  private float total_error = 0f;
  private float last_error = 0f;
  private float Kp;
  private float Ki;
  private float Kd;

  public PidController(float kp, float ki, float kd) {
    Kp = kp;
    Ki = ki;
    Kd = kd;
  }

  public float update(float error) {
    float e_P = error;

    total_error += error;
    float e_I = total_error;

    float e_D = error - last_error;
    last_error = error;

    return e_P * Kp + e_I * Ki + e_D * Kd;
  }

  public void clear() {
    total_error = 0f;
    last_error = 0f;
  }
}

public class MovingAverage {
  private float? current = null;
  private float factor;

  public MovingAverage(float factor_) {
    factor = factor_;
  }

  public void update(float x) {
    current = current ?? x;
    current = factor * x + (1 - factor) * current;
  }

  public float? Average => current;

  public void clear() {
    current = null;
  }
}

public class DifferentialDrive {
  // Differential drive controller that achieves a given position/angle
  // 3 Stages:
  // 1. Turn rover towards waypoint
  // 2. Drive to waypoint
  // 3. Tun to angle of waypoint
  // PIDs probably need more tuning

  private int stage = 0;

  // Just for turning towards a desired angle
  private PidController static_angle_controller = new PidController(0.6f, 0.0002f, 0.005f);

  // Moves rover towards goal (ignoring goal angle)
  private PidController angle_controller = new PidController(0.3f, 0.0001f, 0f);
  private PidController speed_controller = new PidController(0.15f, 0.0001f, 0f);

  // Tracks error of each stage so that we know when to progress to the next one
  private MovingAverage error_average = new MovingAverage(0.05f);

  private State target;
  private State current;

  // Controls the accuracy of the controller
  private float arrived_distance = 20f;
  private float arrived_angle_rad = 0.1f;

  public DifferentialDrive(State current_, State target_) {
    current = current_;
    target = target_;
  }

  public float signedAngle(float angle) {
      return angle > (float)Math.PI ? (angle - 2 * (float)Math.PI) : angle;
  }

  public (float velocity, float angular_velocity) step(State current_) {
    current = current_;
    if (stage == 0) {
      // Here we want to point the rover towards the next waypoint
      Vector2 current_direction = new Vector2((float)Math.Cos(current.theta), -(float)Math.Sin(current.theta));
      Vector2 position_delta = target.pos - current.pos;

      float angle_error = Mathf.Deg2Rad * Vector2.SignedAngle(current_direction, position_delta);
      error_average.update(Math.Abs(angle_error));

      float target_angular_velocity = static_angle_controller.update(angle_error);

      if (error_average.Average < arrived_angle_rad) {
        ++stage;
        static_angle_controller.clear();
        error_average.clear();
      }

      return (0f, target_angular_velocity);
    } else if (stage == 1) {
      Vector2 current_direction = new Vector2((float)Math.Cos(current.theta), -(float)Math.Sin(current.theta));
      Vector2 position_delta = target.pos - current.pos;
      error_average.update(position_delta.magnitude);
      if (error_average.Average < arrived_distance) {
        ++stage;
        error_average.clear();
      }

      // We don't want to speed up if we are going orthogonal 
      float distance_error = Vector2.Dot(position_delta, current_direction);

      float target_velocity = speed_controller.update(distance_error);

      float angle_error = Mathf.Deg2Rad * Vector2.SignedAngle(current_direction, position_delta);
      float target_angular_velocity = angle_controller.update(angle_error);
      return (target_velocity, target_angular_velocity);
    } else if (stage == 2) {
      // Here we want to point the rover towards the next waypoint
      float angle_error = signedAngle(target.theta - current.theta);
      Debug.LogFormat("{0}, {1}, {2}", angle_error, target.theta, current.theta);
      error_average.update(Math.Abs(angle_error));
      if (error_average.Average < arrived_angle_rad) {
        ++stage;
      }

      float target_angular_velocity = static_angle_controller.update(-angle_error);
      return (0f, target_angular_velocity);
    }

    return (0f, 0f);
  }

  public bool hasArrived() {
    return stage == 3;
  }
}
