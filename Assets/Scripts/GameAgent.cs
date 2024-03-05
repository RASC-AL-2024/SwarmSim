using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;

using RStateType = RoverNode.State;

public class FailableModule : MonoBehaviour {
  public float maybeFailFreq = 100f;
  public float failureChance = 0.01f;
  public Vector3 realCenter;
  public float materialsToRepair = 60f;

  public float neededMaterials = -1;

  protected void initFailable() {
    // jank
    materialsToRepair = 60f;
    realCenter = GetComponentsInChildren<Renderer>()[0].bounds.center;
    StartCoroutine(failLoop());
  }

  public void repair(float materials) {
    Debug.Assert(neededMaterials >= 0);
    neededMaterials -= materials;
    Debug.LogFormat("Failed module {0} needs {1} more materials", name, neededMaterials);
    if (neededMaterials <= 0f) {
      registerFixed();
      fix();
      Debug.LogFormat("Module {0} fixed", name);
    }
  }

  public virtual void fail() { Debug.Assert(false); }
  public virtual void fix() { Debug.Assert(false); }

  void registerBroken() {
    SingletonBehaviour<GameMainManager>.Instance.brokenModules.Add(this, false);
  }

  void registerFixed() {
    SingletonBehaviour<GameMainManager>.Instance.brokenModules.Remove(this);
  }

  void doFail() {
    if (neededMaterials > 0)
      return;

    Debug.LogFormat("Module {0} failed", name);
    registerBroken();
    neededMaterials = materialsToRepair;
    fail();
  }

  IEnumerator failLoop() {
    while (true) {
      if (UnityEngine.Random.Range(0f, 1f) <= failureChance) {
        doFail();
      }

      yield return new WaitForSeconds(maybeFailFreq);
    }
  }
};

public class GameAgent : FailableModule
{
    [HideInInspector] public int sid = -1;

    public List<Miner> miners;

    [SerializeField]
    public GameObject processingStation;
    [SerializeField]
    public float miningDuration; // seconds
    [SerializeField]
    public float processingDuration; // seconds

    [SerializeField]
    public Transform bucket;

    [SerializeField]
    public ArticulationBody[] leftWheels;
    [SerializeField]
    public ArticulationBody[] rightWheels;

    public RoverState rover_state;

    State processingState;

    private float wheel_diameter = 0.336f;
    private float axle_width = 0.60f;
    private float maxwheel_velocity = 2; // rad/s

    private float target_velocity = 0f;
    private float target_angular_velocity = 0f;

    public float lowBattery = 20f;
    public float carryableResources = 100f;
    public float repairDowntimeS = 100f;

    private static Dictionary<RStateType, RoverState.Activity> activityMap =
      new Dictionary<RStateType, RoverState.Activity>{
        {RStateType.MOVING_TO_MINE, RoverState.Activity.MOVING},
        {RStateType.MOVING_TO_PROCESSING, RoverState.Activity.MOVING},
        {RStateType.MOVING_TO_CHARGING, RoverState.Activity.MOVING},
        {RStateType.MOVING_TO_REPAIRING, RoverState.Activity.MOVING},
        {RStateType.MINING, RoverState.Activity.MINING},
        {RStateType.CHARGING, RoverState.Activity.CHARGING},
        {RStateType.REPAIRING, RoverState.Activity.REPAIRING},
        {RStateType.PROCESSING, RoverState.Activity.NEUTRAL},
        {RStateType.OTHER, RoverState.Activity.NEUTRAL}
      };

    public MotionPlanner motion_planner;
    public TargetPlanner target_planner;

    private Miner? loadingMiner = null;
    private FailableModule? repairTarget = null;

    private bool broken = false;

    void Start()
    {
        initFailable();
        rover_state = new RoverState(sid);
        
        initTargetPlanner();

        target_planner = new TargetPlanner(sid);
        motion_planner = new MotionPlanner(sid, transform);
    }

    public override void fail() {
      broken = true;

      // Stop us
      Velocity zero_velocity = new Velocity(0, 0);
      updateWheels(zero_velocity);
      Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
      Simulator.Instance.setAgentIsMoving(sid, false);
    }
    public override void fix() {
      broken = false;
    }

    private void initTargetPlanner()
    {
        TargetPlanner.miners = miners; 
        TargetPlanner.processingStation = processingStation.transform;
        TargetPlanner.miningDuration = miningDuration;
        TargetPlanner.processingDuration = processingDuration;
    }

    private void updateWheels(Velocity current_velocity)
    {
        (float wL, float wR) = wheelVelocities(current_velocity.linear_vel, current_velocity.angular_vel);

        // Scale so we don't go too fast
        float d = Math.Max(Mathf.Abs(wR / maxwheel_velocity), Mathf.Abs(wL / maxwheel_velocity));
        d = d > 1f ? d : 1f;

        // These target velocities are in deg/s
        foreach (var wheel in leftWheels)
        {
            var drive = wheel.xDrive;
            drive.targetVelocity = Mathf.Rad2Deg * wL / d;
            wheel.xDrive = drive;
        }
        foreach (var wheel in rightWheels)
        {
            var drive = wheel.xDrive;
            drive.targetVelocity = Mathf.Rad2Deg * wR / d;
            wheel.xDrive = drive;
        }
    }

    private (float wL, float wR) wheelVelocities(float v, float w)
    {
        float s = 2 / wheel_diameter;
        return ((v - w * axle_width / 2) * s, (v + axle_width / 2 * w) * s);
    }

    private Vector2 get2dPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    State getCurrentState()
    {
        Vector2 position = new Vector2(transform.position.x, transform.position.z);
        Quaternion rot = transform.rotation;
        return new State(position, rot);
    }

    void updateRoverState(RStateType state_type)
    {
        bool has_load = state_type == RStateType.MOVING_TO_PROCESSING;
        rover_state.updateHasLoad(has_load);

        rover_state.updateBattery(activityMap[state_type], Time.deltaTime);
        
        var curr_state = getCurrentState();
        rover_state.updateState(curr_state);
    }

    void updateMinerState(bool isMoving) {
      if (isMoving) {
        if (loadingMiner != null) {
          float loadedResources = loadingMiner.UnregisterRover(bucket);
          SingletonBehaviour<GameMainManager>.Instance.totalResources += loadedResources;
          loadingMiner = null;
        }
        return;
      }

      Miner? nearbyMiner = null;
      foreach (var miner in miners) {
        if ((miner.minePosition.position - transform.position).magnitude <= 5) {
          nearbyMiner = miner;
          break;
        }
      }

      if (nearbyMiner == null || loadingMiner != null || nearbyMiner == loadingMiner)
        return;

      loadingMiner = nearbyMiner;
      loadingMiner.RegisterRover(bucket);
    }

    void maybeStartRepairPlan() {
      foreach (var kvp in SingletonBehaviour<GameMainManager>.Instance.brokenModules) {
        if (!kvp.Value) {
          // there has got to be a better way
          SingletonBehaviour<GameMainManager>.Instance.brokenModules[kvp.Key] = true;
          target_planner.generateRepairPlan(kvp.Key, repairDowntimeS);
          Debug.LogFormat("Going to repair {0}", kvp.Key.name);
          repairTarget = kvp.Key;
          break;
        }
      }
    }

    void endRepair() {
      if (repairTarget is null) {
        return;
      }

      SingletonBehaviour<GameMainManager>.Instance.brokenModules[repairTarget] = false;
      repairTarget = null;
    }

    void updateRepairState() {
      if (!target_planner.isRepairPlan() || target_planner.getIsMoving() || repairTarget is null) {
        return;
      }
  
      if ((repairTarget.realCenter - transform.position).magnitude <= 5) {
        float resources = Mathf.Min(SingletonBehaviour<GameMainManager>.Instance.totalResources, carryableResources);
        Debug.LogFormat("Repaired with {0} resources", resources);
        SingletonBehaviour<GameMainManager>.Instance.totalResources -= resources;
        repairTarget.repair(resources);
        endRepair();
      }
    }

    void Update()
    {
        // Do nothing if we are broken
        if (broken || rover_state.battery.chargeAmount <= 0f)
          return;

        if (rover_state.battery.chargeAmount <= lowBattery && !target_planner.isChargePlan()) {
          endRepair();
          target_planner.generateChargingPlan(rover_state.battery.chargeDuration());
        }
        if (!target_planner.isChargePlan()) {
          maybeStartRepairPlan();
        }

        target_planner.step(get2dPosition());

        if (target_planner.getIsMoving())
        {
            Simulator.Instance.setAgentIsMoving(sid, true);
            Vector2 goal_position = target_planner.getGoalPosition();
            Velocity curr_vel = motion_planner.step(goal_position);
            updateWheels(curr_vel);
        } else
        {
            Velocity zero_velocity = new Velocity(0, 0);
            updateWheels(zero_velocity);
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
            Simulator.Instance.setAgentIsMoving(sid, false);
        }
        updateMinerState(target_planner.getIsMoving());
        updateRepairState();
        if(target_planner.isValidState())
            updateRoverState(target_planner.getCurrentState());
    }
}
