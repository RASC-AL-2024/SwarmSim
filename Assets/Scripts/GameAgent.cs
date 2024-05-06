using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEditor;

// If you uncomment these it shits the bed I have no idea why I hate unity
// [System.Serializable]
public class Storage
{
    public double current;
    public double capacity;

    public Storage(double capacity, double? current = null)
    {
        this.current = current ?? capacity;
        this.capacity = capacity;
    }

    public void add(double rate, double dt)
    {
        add(rate * dt);
    }

    public void add(double dcurrent)
    {
        current = Math.Min(capacity, current + dcurrent);
    }

    public double remove(double rate, double dt)
    {
        return remove(rate * dt);
    }

    public double remove(double dcurrent)
    {
        var oldcurrent = current;
        current = Math.Max(current - dcurrent, 0f);
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

    public void transferTo(Storage other, double rate, double dt, double efficiency = 1.0)
    {
        other.add(efficiency * remove(Math.Min((other.capacity - other.current) / efficiency, rate * dt)));
    }
}

// [System.Serializable]
public class Battery : Storage
{
    // Capacity is in J
    public Battery(double capacity) : base(capacity) { }
    public Battery(double capacity, double? current = null) : base(capacity, current) { }

    public bool isLow()
    {
        return (current / capacity) <= Constants.lowBatteryThreshold;
    }
}

public abstract class Behaviour : MonoBehaviour
{
    public abstract void Cancel();
}

public class BatteryModule : Behaviour
{
    public Battery Battery { get; set; } = new Battery(Constants.roverBatteryCapacity);
    private ArticulationBody rootBody;
    public Battery SourceBattery { get; set; } = null;
    public bool alwaysAttach = false;

    void Start()
    {
        rootBody = GetComponentInParent<ArticulationBody>();
        StartCoroutine(Log());
    }

    IEnumerator Log()
    {
        while (true)
        {
            Debug.LogFormat("Rover {0} battery: {1}%", GetComponentInParent<GameAgent>().sid, 100f * (Battery.current / Battery.capacity));
            yield return new WaitForSeconds(2048);
        }
    }

    public override void Cancel()
    {
        SourceBattery = null;
    }

    void Update()
    {
        var velocities = new List<float>();
        rootBody.GetJointVelocities(velocities);
        float totalDrain = 0f;
        foreach (var velocity in velocities)
        {
            // velocities includes for non-revolute joints, real joint velocity is never quite 0
            if (velocity == 0)
                continue;

            totalDrain += Constants.servoIdleDrain + (Math.Abs(velocity) > 0.01 ? Constants.servoActiveDrain : 0);
        }
        if (!alwaysAttach && false)
        {
            Debug.LogFormat("{0}, {1}, [{2}]", totalDrain, velocities.Count, String.Join(", ", velocities.Select(n => n.ToString())));
        }

        Battery.remove(totalDrain, Time.deltaTime);

        if (SourceBattery != null && !Battery.full())
        {
            SourceBattery.transferTo(Battery, Constants.chargeRate, Time.deltaTime);
        }

        if (SourceBattery != null && Battery.full() && !alwaysAttach)
        {
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedCharging(GetComponentInParent<GameAgent>()));
            SourceBattery = null;
        }
    }
}

public class LoadModule : Behaviour
{
    public Storage Dirt { get; set; } = new Storage(Constants.roverCarryingCapacity, 0);

    private Miner miner = null; // maybe null
    public Storage Unload { get; set; } = null;

    public void setMiner(Miner miner)
    {
        this.miner = miner;
        this.miner.RegisterRover(GetComponentInParent<GameAgent>().bucket, Dirt);
    }

    public override void Cancel()
    {
        if (miner != null)
            miner.UnregisterRover(GetComponentInParent<GameAgent>().bucket);
        miner = null;
    }

    void Update()
    {
        // Unregister us from the miner if we are full
        if (miner != null && (miner.broken || Dirt.full()))
        {
            Cancel();
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedLoading(GetComponentInParent<GameAgent>()));
        }

        // We are done
        if (Unload != null && Dirt.empty())
        {
            Unload = null;
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedUnloading(GetComponentInParent<GameAgent>()));
        }

        if (Unload != null)
        {
            Dirt.transferTo(Unload, Constants.dirtTransferRate, Time.deltaTime);
            CentralResources.TotalDirt.add(Dirt.capacity); // quick hack because I can't code :(
        }
    }
}

public class RepairModule : Behaviour
{
    private FailableModule module; // maybe null
    float expiry = 0f;

    public void setRepairModule(FailableModule module)
    {
        this.module = module;
        expiry = Time.time + Constants.repairTime;
    }

    public override void Cancel()
    {
        module = null;
    }

    void Update()
    {
        // We have finished the repair
        if (module != null && Time.time >= expiry)
        {
            var oldModule = module;
            module = null;
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.FinishedRepairing(oldModule));
            oldModule.fix();
        }
    }
}

public class FailableModule : MonoBehaviour
{
    public Vector3 realCenter;

    public bool broken = false;

    protected void initFailable()
    {
        realCenter = GetComponentsInChildren<Renderer>()[0].bounds.center;
        StartCoroutine(failLoop());
    }

    public void fix()
    {
        broken = false;
    }

    public void fail()
    {
        if (broken)
            return;

        Debug.LogFormat("Module {0} failed", name);
        SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.Broken(this));
        broken = true;
    }

    IEnumerator failLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Constants.maybeFailInterval);

            if (UnityEngine.Random.Range(0f, 1f) <= Constants.failureChance)
            {
                fail();
            }
        }
    }
};

[CustomEditor(typeof(FailableModule))]
public class FailableModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draws the default inspector

        FailableModule script = (FailableModule)target;

        // Add a custom button in the inspector
        if (GUILayout.Button("Kill"))
        {
            script.fail();
        }
    }
}

public class GameAgent : FailableModule
{
    [HideInInspector] public int sid = -1;

    [SerializeField]
    public Transform bucket;

    [SerializeField]
    public ArticulationBody[] leftWheels;
    [SerializeField]
    public ArticulationBody[] rightWheels;

    private Vector2? goalPosition = null;

    private float wheel_diameter = 0.336f;
    private float axle_width = 0.60f;
    private float maxwheel_velocity = 2; // rad/s
    private float min_goal_distance = 5f; // 3

    public MotionPlanner motion_planner;

    private BatteryModule batteryModule;

    void Start()
    {
        initFailable();

        motion_planner = new MotionPlanner(sid, transform);

        batteryModule = gameObject.AddComponent<BatteryModule>();
        gameObject.AddComponent<LoadModule>();
        gameObject.AddComponent<RepairModule>();

        SingletonBehaviour<Planner>.Instance.registerRover(this);
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

    public void setGoalPosition(Vector2 goalPosition)
    {
        this.goalPosition = goalPosition;
    }

    void Update()
    {
        // Do nothing if we are broken
        if (broken || batteryModule.Battery.empty())
        {
            Velocity zero_velocity = new Velocity(0, 0);
            updateWheels(zero_velocity);
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
            Simulator.Instance.setAgentIsMoving(sid, false);
            return;
        }

        // Check arrival
        if (goalPosition.HasValue && Vector2.Distance(goalPosition.Value, get2dPosition()) < min_goal_distance)
        {
            goalPosition = null;
            SingletonBehaviour<Planner>.Instance.handleEvent(new Planner.Arrived(this));
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
