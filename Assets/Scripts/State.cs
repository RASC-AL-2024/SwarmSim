using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class State
{
    public Vector2 pos;
    public float theta;

    public State()
    {
        pos = Vector2.zero;
        theta = 0f;
    }

    public State(Vector2 pos_, float theta_)
    {
        pos = pos_;
        theta = theta_;
    }
}
