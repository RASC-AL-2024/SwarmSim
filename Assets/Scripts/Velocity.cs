using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Velocity
{
    public float linear_vel = 0f;
    public float angular_vel = 0f;
    public Velocity(float t_linear_vel=0, float t_angular_vel=0)
    {
        linear_vel = t_linear_vel;
        angular_vel = t_angular_vel;
    }
}