using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;

using RStateType = RoverNode.State;

public class Storage
{
    public float current;
    public float capacity;

    public Storage(float capacity)
    {
        this.current = capacity;
        this.capacity = capacity;
    }

    public void add(float rate, float dt)
    {
        add(rate * dt);
    }

    public void add(float dcurrent)
    {
        current = Mathf.Min(capacity, current + dcurrent);
    }

    public float remove(float rate, float dt)
    {
        var oldcurrent = current;
        current = Mathf.Max(current - rate * dt, 0f);
        return oldcurrent - current;
    }

    public bool empty()
    {
        return current == 0f;
    }

    public bool full()
    {
        return current == capacity;
    }

    public void transferTo(Storage other, float rate, float dt)
    {
        other.add(remove(rate, rate * dt));
    }
}

public class Battery : Storage
{
    // Capacity is in J
    public Battery(float capacity) : base(capacity) { }

    public bool isLow()
    {
        return (current / capacity) <= Constants.lowBatteryThreshold;
    }
}

public class BatteryModule : MonoBehaviour
{
    public ArticulationBody rootBody;
    public Battery battery = new Battery(Constants.roverBatteryCapacity);
    public Battery sourceBattery; // Maybe null
    public bool alwaysAttach = false;

    void Start()
    {
        rootBody = GetComponentInParent<ArticulationBody>();
    }

    void Update()
    {
        var velocities = new List<float>();
        rootBody.GetJointVelocities(velocities);
        float totalDrain = 0f;
        foreach (var velocity in velocities)
        {
            totalDrain += Constants.servoIdleDrain + (velocity > 0.01 ? Constants.servoActiveDrain : 0);
        }
        battery.remove(totalDrain, Time.deltaTime);

        // Maybe charge us
        if (battery.full() && !alwaysAttach)
        {
            sourceBattery = null;

        }

        if (sourceBattery != null)
        {
            sourceBattery.transferTo(battery, Constants.chargeRate, Time.deltaTime);
        }
    }
}

public class LoadModule : MonoBehaviour
{
    public Storage dirt = new Storage(Constants.roverCarryingCapacity);
    public Storage unload; // maybe null

    private Miner miner; // maybe null

    public void setMiner(Miner miner)
    {
        this.miner = miner;
        this.miner.RegisterRover(GetComponentInParent<GameAgent>().bucket);
    }

    void Update()
    {
        // Unregister us from the miner if we are full
        if (miner != null && dirt.full())
        {
            miner.UnregisterRover(GetComponentInParent<GameAgent>().bucket);
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedLoading(GetComponentInParent<GameAgent>()));
            miner = null;
        }

        // We are done
        if (unload != null && dirt.empty())
        {
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedUnloading(GetComponentInParent<GameAgent>()));
            unload = null;
        }

        if (unload != null)
        {
            dirt.transferTo(unload, Constants.dirtTransferRate, Time.deltaTime);
        }
    }
}

public class RepairModule : MonoBehaviour
{
    private FailableModule module; // maybe null
    float expiry = 0f;

    public void setRepairModule(FailableModule module)
    {
        this.module = module;
        expiry = Time.time + Constants.repairTime;
    }

    void Update()
    {
        // We have finished the repair
        if (module != null && Time.time >= expiry)
        {
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedRepairing(module));
            module.fix();
            module = null;
        }
    }
}

public class FailableModule : MonoBehaviour
{
    public float maybeFailFreq = 100f;
    public float failureChance = 0.01f;
    public Vector3 realCenter;

    public bool broken = false;

    protected void initFailable()
    {
        realCenter = GetComponentsInChildren<Renderer>()[0].bounds.center;
        StartCoroutine(failLoop());
    }

    public virtual void fail() { }
    public virtual void fix() { }

    void registerBroken()
    {
        SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.Broken(this));
        broken = true;
    }

    void doFail()
    {
        if (broken)
            return;

        Debug.LogFormat("Module {0} failed", name);
        registerBroken();
        fail();
    }

    IEnumerator failLoop()
    {
        while (true)
        {
            if (UnityEngine.Random.Range(0f, 1f) <= failureChance)
            {
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

    private Vector2? goalPosition;

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

    private Miner loadingMiner = null;
    private FailableModule repairTarget = null;

    private BatteryModule batteryModule;

    void Start()
    {
        miners = new List<Miner>(UnityEngine.Object.FindObjectsOfType<Miner>());

        initFailable();
        rover_state = new RoverState(sid);

        initTargetPlanner();

        target_planner = new TargetPlanner(sid);
        motion_planner = new MotionPlanner(sid, transform);

        batteryModule = GetComponentInParent<BatteryModule>();
    }

    public override void fail()
    {
        // Stop us
        Velocity zero_velocity = new Velocity(0, 0);
        updateWheels(zero_velocity);
        Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
        Simulator.Instance.setAgentIsMoving(sid, false);
    }
    public override void fix()
    {
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

        // rover_state.updateBattery(activityMap[state_type], Time.deltaTime);

        var curr_state = getCurrentState();
        rover_state.updateState(curr_state);
    }

    void maybeStartRepairPlan()
    {
        foreach (var kvp in SingletonBehaviour<GameMainManager>.Instance.brokenModules)
        {
            if (!kvp.Value)
            {
                // there has got to be a better way
                SingletonBehaviour<GameMainManager>.Instance.brokenModules[kvp.Key] = true;
                target_planner.generateRepairPlan(kvp.Key, repairDowntimeS);
                Debug.LogFormat("Going to repair {0}", kvp.Key.name);
                repairTarget = kvp.Key;
                break;
            }
        }
    }

    void endRepair()
    {
        if (repairTarget is null)
        {
            return;
        }

        var dict = SingletonBehaviour<GameMainManager>.Instance.brokenModules;
        if (dict.ContainsKey(repairTarget))
        {
            dict[repairTarget] = false;
        }
        repairTarget = null;
    }

    // goofy
    void checkImpact()
    {
        var transform = SingletonBehaviour<GameMainManager>.Instance.impact;
        if (transform != null && !target_planner.isChargePlan())
        {
            Debug.LogFormat("Detected impact");
            endRepair();
            target_planner.generateChargingPlan(100f);
        }
    }

    public void setGoalPosition(Vector2 goalPosition)
    {
        this.goalPosition = goalPosition;
    }

    void Update()
    {
        // Do nothing if we are broken
        if (broken || batteryModule.battery.empty())
            return;

        // Check arrival
        if (goalPosition.HasValue && Vector2.Distance(goalPosition.Value, get2dPosition()) < 2f)
        {
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.Arrived(this));
            goalPosition = null;
        }

        if (goalPosition.HasValue)
        {
            Simulator.Instance.setAgentIsMoving(sid, true);
            Velocity curr_vel = motion_planner.step(goalPosition.Value);
            updateWheels(curr_vel);
        }
        else
        {
            Velocity zero_velocity = new Velocity(0, 0);
            updateWheels(zero_velocity);
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
            Simulator.Instance.setAgentIsMoving(sid, false);
        }
    }
}
