using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoverNode
{
    public enum GoalType { POSITION, DURATION }
    public enum State { MOVING_TO_MINE, MINING, MOVING_TO_PROCESSING, PROCESSING, MOVING_TO_CHARGING, CHARGING, MOVING_TO_REPAIRING, REPAIRING, OTHER };

    public State state;
    public Goal goal;
    public float timestamp;

    public RoverNode(State t_state, Goal t_goal)
    {
        state = t_state;
        goal = t_goal;
    }

    public class Goal
    {
        public GoalType goal_type;
        public Vector2 goal_pos;
        public float goal_duration;
        public Miner goal_miner;

        public Func<Vector2> pos_generator;
        public Func<float> time_generator;

        public Goal(GoalType t_type)
        {
            goal_type = t_type;
        }

        public bool Check(Vector2 pos, float time)
        {
            float position_epsilon = 2f;
            if (goal_type == GoalType.DURATION)
            {
                return goal_duration <= time;
            } else if(goal_type == GoalType.POSITION)
            {
                return Vector2.Distance(pos, goal_pos) < position_epsilon;
            }
            return false;
        }

        public void Generate()
        {
            if (goal_type == GoalType.DURATION)
            {
                goal_duration = time_generator();
            }
            else if (goal_type == GoalType.POSITION)
            {
                goal_pos = pos_generator();
            }
        }
    }
}
