using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class RoverState
{
    [Serializable]
    public class Battery
    {
        float chargeTime;
        float chargeDuration;

        public Battery(float chargeDuration_)
        {
            chargeTime = Time.time;
            chargeDuration = chargeDuration_;
        }

        public float charge()
        {
            return 1f - (Time.time - chargeTime) / chargeDuration;
        }

        public bool empty()
        {
            return charge() <= 0f;
        }
    }

    public int id { get; set; }
    public bool hasLoad { get; set; }
    public Battery battery { get; set; }
    public State state { get; set; }

    public RoverState(int t_id)
    {
        battery = new Battery(180f);
        id = t_id;
        hasLoad = false;
        // state = t_state;
    }
}
