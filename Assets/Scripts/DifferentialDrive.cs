using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DifferentialDrive
{
    public float dt = 0.1f;
    private State current;
    private State goal;
    private float E = 0f;
    private float old_e = 0f;
    private float Kp = 1.0f;
    private float Ki = 0.01f;
    private float Kd = 0.01f;
    private float desiredV = 200.0f;
    private float distance_min = 50f;
    private float angle_min = 0.1f;
    private bool valid_angle = false;
    private bool valid_distance = false;

    public DifferentialDrive(State current_state, State goal_state)
    {
        current = current_state;
        goal = goal_state;
    }
    
    public void iteratePID(out float v, out float w)
    {

        float e = getAngleError();
        
        float e_P = e;
        float e_I = E + e;
        float e_D = e - old_e;

        w = Kp * e_P + Ki * e_I + Kd * e_D;
        v = desiredV;

        E = E + e;
        old_e = e;

        if(isAngleArrived())
        {
            valid_angle = true;
        }

        if (isDistanceArrived())
        {
            valid_distance = true;
        }
        
        if (!valid_angle || valid_distance)
        {
            v = 0f;
        }
        else if(valid_angle && !valid_distance)
        {
            w = 0f;
        }
    }

    private float getDistanceError()
    {
        float distance_err = Vector2.SqrMagnitude(current.pos - goal.pos);
        return distance_err;
    }

    private float getAngleError()
    {
        Vector2 delta = goal.pos - current.pos;
        Vector2 current_angle = new Vector2((float)Math.Cos(current.theta), (float)Math.Sin(current.theta));
        float angle_err = Mathf.Deg2Rad * Vector2.SignedAngle(current_angle, delta);
        return angle_err;
    }

    private bool isAngleArrived()
    {
        return Mathf.Abs(getAngleError()) < angle_min;
    }

    private bool isDistanceArrived()
    {
        return getDistanceError() < distance_min;
    }

    public bool hasArrived()
    {
        return isAngleArrived() && isDistanceArrived();
    }

    public void step(State state, out float v, out float w)
    {
        current = state;
        current.theta = -current.theta;
        iteratePID(out v, out w);
    }
}
