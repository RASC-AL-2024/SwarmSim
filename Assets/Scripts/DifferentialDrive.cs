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
  private PidController static_angle_controller = new PidController(5f, 0.00f, 0.000f);

  // Moves rover towards goal (ignoring goal angle)
  private PidController angle_controller = new PidController(1.0f, 0.0f, 0.0f);
  private PidController speed_controller = new PidController(0.5f, 0.0f, 0.0f);

  // Tracks error of each stage so that we know when to progress to the next one
  private MovingAverage error_average = new MovingAverage(0.05f);

  private State target;
  private State current;

  // Controls the accuracy of the controller
  private float arrived_distance = 0.30f;
  private float arrived_angle_rad = Mathf.Deg2Rad * 10f;

  public DifferentialDrive(State current_, State target_) {
    current = current_;
    target = target_;
  }

  public float reducedAngle(float angle) {
    angle = Math.Sign(angle) * (Math.Abs(angle) % (2f * (float)Math.PI)); // [-2pi, 2pi]
    if (Math.Abs(angle) <= Math.PI) {
      return angle;
    }
    return Math.Sign(angle) * (Math.Abs(angle) - 2 * (float) Math.PI); 
  }

  private Vector2 direction(float theta) {
    return new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
  }

  public (float velocity, float angular_velocity) step(State current_) {
    current = current_;
    if (stage == 0) {
      // Here we want to point the rover towards the next waypoint
      Vector2 current_direction = direction(current.theta);
      Vector2 position_delta = target.pos - current.pos;

      float angle_error = Mathf.Deg2Rad * Vector2.SignedAngle(current_direction, position_delta);
      error_average.update(Math.Abs(angle_error));
      float target_angular_velocity = static_angle_controller.update(angle_error);

      // Have we been close enough in direction?
      if (error_average.Average < arrived_angle_rad) {
        ++stage;
        static_angle_controller.clear();
        error_average.clear();
      }

      return (0f, target_angular_velocity);
    } else if (stage == 1) {
      Vector2 current_direction = direction(current.theta);
      Vector2 position_delta = target.pos - current.pos;

      // Are we close to the waypoint?
      error_average.update(position_delta.magnitude);
      if (error_average.Average < arrived_distance) {
        ++stage;
        error_average.clear();
      }

      // Don't speed up if we are going orthogonal 
      float distance_error = Vector2.Dot(position_delta, current_direction);

      float target_velocity = speed_controller.update(distance_error);

      float angle_error = Mathf.Deg2Rad * Vector2.SignedAngle(current_direction, position_delta);
      float target_angular_velocity = angle_controller.update(angle_error);

      return (target_velocity, target_angular_velocity);
    } else if (stage == 2) {
      // Here we want to point the rover towards the next waypoint
      float angle_error = reducedAngle(reducedAngle(target.theta) - reducedAngle(current.theta));

      // Are we pointed close?
      error_average.update(Math.Abs(angle_error));
      if (error_average.Average < arrived_angle_rad) {
        ++stage;
      }

      float target_angular_velocity = static_angle_controller.update(angle_error);
      return (0f, target_angular_velocity);
    }

    return (0f, 0f);
  }

  public bool hasArrived() {
    return stage == 3;
  }
}
