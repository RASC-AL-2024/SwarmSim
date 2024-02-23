using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class RoverState
{
    public enum Activity { MINING, MOVING, CHARGING, NEUTRAL };
    float neutralDischargeRate = 0.1f;
    float miningDischargeRate = 0.0f;
    float movingDischargeRate = 0.1f;
    float movingLoadScale = 2.0f;

    [Serializable]
    public class Battery
    {
        public float chargeAmount;
        float maxCapacity;
        float chargeRate;

        public Battery(float maxCapacity_, float chargeRate_)
        {
            chargeAmount = maxCapacity_;
            maxCapacity = maxCapacity_;
            chargeRate = chargeRate_;
        }

        public void charge(float dt)
        {
            chargeAmount = Mathf.Min(maxCapacity, chargeAmount + chargeRate * dt);
        }

        public float chargeDuration() {
          return maxCapacity / chargeRate;
        }

        public void discharge(float rate, float dt)
        {
            chargeAmount = Mathf.Max(chargeAmount - rate * dt, 0f);
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
        battery = new Battery(100f, 1f);
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

    public void updateBattery(Activity activity, float dt)
    {
      switch (activity) {
        case Activity.CHARGING:
          battery.charge(dt);
          return;
        case Activity.MINING:
          battery.discharge(neutralDischargeRate + miningDischargeRate, dt);
          return;
        case Activity.MOVING:
          float scale = hasLoad ? movingLoadScale : 1.0f;
          battery.discharge(neutralDischargeRate + movingDischargeRate * scale, dt);
          return;
        case Activity.NEUTRAL:
          battery.discharge(neutralDischargeRate, dt);
          return;
      }
    }
}
