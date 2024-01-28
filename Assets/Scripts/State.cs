using UnityEngine;
using System.Collections.Generic;

public class State
{
    public Vector2 pos;
    public float theta;

    public State(Vector2 pos_, float theta_)
    {
        pos = pos_;
        theta = theta_;
    }
}
