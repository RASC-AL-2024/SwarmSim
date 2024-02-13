using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using RStateType = RoverNode.State;

[Serializable]
public class RoverState
{
    [Serializable]
    public class Battery
    {
        public float chargeAmount;
        float chargeTime;
        float chargeDuration;
        float maxCapacity;
        float dischargeRate;

        public Battery(float chargeDuration_, float maxCapacity_, float dischargeRate_)
        {
            chargeTime = Time.time;
            chargeDuration = chargeDuration_;
            chargeAmount = maxCapacity_;
            maxCapacity = maxCapacity_;
            dischargeRate = dischargeRate_;
        }

        public void charge()
        {
            chargeAmount = maxCapacity;
        }

        public void discharge()
        {
            chargeAmount -= dischargeRate;
            if(chargeAmount < 0)
            {
                chargeAmount = 0;
            }
        }

        public bool empty()
        {
            return chargeAmount <= 0.5f;
        }
    }

    public int id { get; set; }
    public bool hasLoad { get; set; }
    public Battery battery { get; set; }
    public State state { get; set; }

    public RoverState(int t_id)
    {
        battery = new Battery(180f, 1000f, 1f);
        id = t_id;
        hasLoad = false;
        state = new State();
    }

    public void serializeObject()
    {
        state.serializeObject();
    }

    public void updateState(State new_state)
    {
        state = new_state;
    }

    public void updateHasLoad(bool has_load)
    {
        hasLoad = has_load;
    }

    public void updateBattery(bool is_moving)
    {
        if (is_moving)
        {
            battery.discharge();
        }
    }

}
