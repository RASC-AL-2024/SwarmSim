using UnityEngine;
using RVO;
using System;

public class MotionPlanner
{
    private int sid;
    private System.Random m_random;
    private Transform transform;

    public MotionPlanner(int t_sid, Transform t_transform)
    {
        sid = t_sid;
        m_random = new System.Random();
        transform = t_transform;
    }

    public Velocity step(Vector2 goal_position)
    {
        Vector2 goalVector = getGoalVector(goal_position);
        if (RVOMath.absSq(goalVector) > 1.0f)
        {
            goalVector = RVOMath.normalize(goalVector);
        }
        else
        {
            return new Velocity(0, 0);
        }

        Vector2 actual_vel = Simulator.Instance.getAgentVelocity(sid);
        Velocity robot_vel = getRobotVel(actual_vel);
        updateSimulator(goalVector);

        return robot_vel;
    }

    private void updateSimulator(Vector2 goalVector)
    {
        Vector2 real_pos = new Vector2(transform.position.x, transform.position.z);
        Simulator.Instance.setAgentPosition(sid, real_pos);

        Simulator.Instance.setAgentPrefVelocity(sid, goalVector);

        /* Perturb a little to avoid deadlocks due to perfect symmetry. */
        float angle = (float)m_random.NextDouble() * 2f * (float)Math.PI;
        float dist = (float)m_random.NextDouble() * 0.0001f;

        Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
                                                     dist *
                                                     new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
    }

    public bool areVectorsAlmostEqual(Vector3 a, Vector3 b, float tolerance)
    {
        Vector3 diff = (a - b);
        return diff.magnitude < tolerance;
    }

    Vector2 getCurrPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    Vector2 getGoalVector(Vector2 goal_position)
    {
        return goal_position - getCurrPosition();
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
            linear_velocity = Constants.roverSpeed;
        }
        else
        {
            Quaternion new_rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 100f * Time.deltaTime);
            Quaternion from_rotation = transform.rotation;
            angular_velocity = getAngularVelocity(from_rotation, new_rotation) * Constants.roverSpeed;
            linear_velocity = Constants.roverSpeed / 2;
        }
        return new Velocity(linear_velocity, angular_velocity);
    }
}
