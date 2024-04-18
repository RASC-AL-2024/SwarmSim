using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public class Storage
{
    public float current;
    public float capacity;

    public Storage(float capacity, float? current = null)
    {
        this.current = current ?? capacity;
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

    public void remove(float dcurrent)
    {
        current = Mathf.Max(current - dcurrent, 0f);
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

[System.Serializable]
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
        battery = new Battery(Constants.roverBatteryCapacity);
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
    public Storage dirt = new Storage(Constants.roverCarryingCapacity, 0);
    public Storage unload = null; // maybe null

    private Miner miner = null; // maybe null

    public void setMiner(Miner miner)
    {
        this.miner = miner;
        this.miner.RegisterRover(GetComponentInParent<GameAgent>().bucket, dirt);
    }

    void Update()
    {
        // Unregister us from the miner if we are full
        if (miner != null && (miner.broken || dirt.full()))
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
            if (UnityEngine.Random.Range(0f, 1f) <= Constants.failureChance)
            {
                fail();
            }

            yield return new WaitForSeconds(Constants.maybeFailInterval);
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
        if (broken || batteryModule.battery.empty())
        {
            Velocity zero_velocity = new Velocity(0, 0);
            updateWheels(zero_velocity);
            Simulator.Instance.setAgentVelocity(sid, Vector2.zero);
            Simulator.Instance.setAgentIsMoving(sid, false);
            return;
        }

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
