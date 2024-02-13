using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RVO;

using RStateType = RoverNode.State;
using RGoal = RoverNode.Goal;
using RGoalType = RoverNode.GoalType;

public class TargetPlanner
{

    public static Bounds[] resourceAreas;
    public static Transform processingStation;
    public static float miningDuration;
    public static float processingDuration;

    private int sid;
    CircularLinkedList state_list;

    public TargetPlanner(int t_sid)
    {
        sid = t_sid;
        state_list = new CircularLinkedList();

    }

    public void resetPlan()
    {
        state_list = new CircularLinkedList();
    }

    public void setRepeatPlan(bool should_repeat)
    {
        state_list.should_repeat = should_repeat;
    }

    public void addPosition(Vector2 position)
    {
        RoverNode waypoint = new RoverNode(RStateType.OTHER, new RGoal(RGoalType.POSITION));
        waypoint.goal.pos_generator = () => position;
        state_list.Add(waypoint);
    }

    public void addDuration(float time)
    {
        RoverNode waiting = new RoverNode(RStateType.OTHER, new RGoal(RGoalType.DURATION));
        waiting.goal.time_generator = () => time;
        state_list.Add(waiting);
    }

    public void generateMiningPlan()
    {
        resetPlan();
        setRepeatPlan(true);
        RoverNode moving_to_mine = new RoverNode(RStateType.MOVING_TO_MINE, new RGoal(RGoalType.POSITION));
        moving_to_mine.goal.pos_generator = generateMiningPosition;

        RoverNode mining = new RoverNode(RStateType.MINING, new RGoal(RGoalType.DURATION));
        mining.goal.time_generator = () => TargetPlanner.miningDuration;

        RoverNode moving_to_processing = new RoverNode(RStateType.MOVING_TO_PROCESSING, new RGoal(RGoalType.POSITION));
        moving_to_processing.goal.pos_generator = () => new Vector2(TargetPlanner.processingStation.position.x, TargetPlanner.processingStation.position.z);

        RoverNode processing = new RoverNode(RStateType.PROCESSING, new RGoal(RGoalType.DURATION));
        processing.goal.time_generator = () => TargetPlanner.processingDuration;

        state_list.Add(moving_to_mine);
        state_list.Add(mining);
        state_list.Add(moving_to_processing);
        state_list.Add(processing);

        state_list.head.Data.timestamp = Time.time;
        state_list.head.Data.goal.Generate();
    }

    public Vector2 getGoalPosition()
    {
        if(state_list.head.Data.goal.goal_type == RGoalType.POSITION)
        {
            return state_list.head.Data.goal.goal_pos;
        }
        throw new Exception("invalid call to getGoalPosition");
    }

    public bool getIsMoving()
    {
        return state_list.head.Data.goal.goal_type == RGoalType.POSITION;
    }

    private void PRINT(string str)
    {
        Debug.Log("Rover " + sid + ": " + str);
    }

    Vector2 generateMiningPosition()
    {
        var bounds = TargetPlanner.resourceAreas[UnityEngine.Random.Range(0, TargetPlanner.resourceAreas.Length)];

        float xoffset = 1f;
        float z_offset = 1f;

        float x = UnityEngine.Random.Range(bounds.min.x + xoffset, bounds.max.x - xoffset);
        float z = UnityEngine.Random.Range(bounds.min.z + z_offset, bounds.max.z - z_offset);
        
        return new Vector2(x, z);
    }

    public RStateType getCurrentState()
    {
        return state_list.head.Data.state;
    }

    public void step(Vector2 position)
    {
        if(state_list.head == null)
        {
            generateMiningPlan();
        }

        if(state_list.Step(position, Time.time))
        {
            state_list.AdvanceAndSet(Time.time);
            PRINT(state_list.head.Data.state.ToString());
        }
    }
}
