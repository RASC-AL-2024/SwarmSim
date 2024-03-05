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

    public static List<Miner> miners;
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

    public void generateChargingPlan(float chargingDuration) {
        resetPlan();
        setRepeatPlan(false);

        RoverNode moving_to_charging = new RoverNode(RStateType.MOVING_TO_CHARGING, new RGoal(RGoalType.POSITION));
        moving_to_charging.goal.pos_generator = () => new Vector2(TargetPlanner.processingStation.position.x, TargetPlanner.processingStation.position.z);

        RoverNode charging = new RoverNode(RStateType.CHARGING, new RGoal(RGoalType.DURATION));
        charging.goal.time_generator = () => chargingDuration;

        state_list.Add(moving_to_charging);
        state_list.Add(charging);
        
        state_list.head.Data.timestamp = Time.time;
        state_list.head.Data.goal.Generate();
    }

    public void generateRepairPlan(FailableModule target, float downTime) {
        // Go back to base, wait, go to failed thing, wait
        resetPlan();
        setRepeatPlan(false);

        RoverNode moving_to_base = new RoverNode(RStateType.MOVING_TO_REPAIRING, new RGoal(RGoalType.POSITION));
        moving_to_base.goal.pos_generator = () => new Vector2(TargetPlanner.processingStation.position.x, TargetPlanner.processingStation.position.z);

        RoverNode getting_materials = new RoverNode(RStateType.REPAIRING, new RGoal(RGoalType.DURATION));
        getting_materials.goal.time_generator = () => downTime;

        RoverNode moving_to_failed = new RoverNode(RStateType.MOVING_TO_REPAIRING, new RGoal(RGoalType.POSITION));
        moving_to_failed.goal.pos_generator = () => new Vector2(target.realCenter.x, target.realCenter.z);

        RoverNode repairing = new RoverNode(RStateType.REPAIRING, new RGoal(RGoalType.DURATION));
        repairing.goal.time_generator = () => downTime;

        state_list.Add(moving_to_base);
        state_list.Add(getting_materials);
        state_list.Add(moving_to_failed);
        state_list.Add(repairing);
        
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
        if (state_list.head == null) return false;
        return state_list.head.Data.goal.goal_type == RGoalType.POSITION;
    }

    public bool isChargePlan() {
      if (state_list.head == null) return false;
      return state_list.head.Data.state == RStateType.MOVING_TO_CHARGING
        || state_list.head.Data.state == RStateType.CHARGING;
    }

    public bool isRepairPlan() {
      if (state_list.head == null) return false;
      return state_list.head.Data.state == RStateType.MOVING_TO_REPAIRING
        || state_list.head.Data.state == RStateType.REPAIRING;
    }

    private void PRINT(string str)
    {
        Debug.Log("Rover " + sid + ": " + str);
    }

    Vector2 generateMiningPosition()
    {
        var miner = TargetPlanner.miners[UnityEngine.Random.Range(0, TargetPlanner.miners.Count)];
        return new Vector2(miner.center.position.x, miner.center.position.z);
    }

    public bool isValidState()
    {
        return state_list.head != null;
    }

    public RStateType getCurrentState()
    {
        return state_list.head.Data.state;
    }

    public void step(Vector2 position)
    {
        if(state_list.head == null)
        {
            PRINT("plan completed! returning to mining...");
            generateMiningPlan();
        }

        if(state_list.Step(position, Time.time))
        {
            state_list.AdvanceAndSet(Time.time);
            if(state_list.head == null) {
              return;
            }
            PRINT(state_list.head.Data.state.ToString());
        }
    }
}
