using UnityEngine;
using System.Collections.Generic;

public class Miner : FailableModule
{
    // Where to scoop froom
    [SerializeField]
    public Transform minePosition;

    // Used for generating mining positions
    [SerializeField]
    public Transform center;

    public List<Transform> waitingRovers = new List<Transform>(); // change back to private
    private IKStatusAbstract status;
    private Dictionary<Transform, Storage> resourcesLoaded = new Dictionary<Transform, Storage>();

    private Transform activeLoading;

    private BatteryModule batteryModule;

    void Start()
    {
        initFailable();
        status = IKStatusFactory.getIKStatus(GetComponent<InverseKinematics>(), Constants.isFast);

        // Always draw directly from central battery
        batteryModule = gameObject.AddComponent<BatteryModule>();
        batteryModule.alwaysAttach = true;
        batteryModule.SourceBattery = SingletonBehaviour<Planner>.Instance.resources.Battery;

        SingletonBehaviour<Planner>.Instance.registerMiner(this);
    }

    public bool RegisterRover(Transform container, Storage roverStorage)
    {
        if (broken || batteryModule.Battery.empty())
            return false;

        Debug.LogFormat("{0}, registered: {1}", name, container);
        waitingRovers.Add(container);
        resourcesLoaded.Add(container, roverStorage);
        return true;
    }

    public void UnregisterRover(Transform container)
    {
        waitingRovers.Remove(container);
        resourcesLoaded.Remove(container);
        Debug.LogFormat("{0}, unregistered: {1}", name, container);
    }

    void Update()
    {
        bool converged = status.Step();

        if (broken || batteryModule.Battery.empty())
            return;

        // The rover left
        if (activeLoading != null && !waitingRovers.Contains(activeLoading))
        {
            status.Target(minePosition);
            activeLoading = null;
            return;
        }

        // We have a new load
        if (converged && activeLoading == null && waitingRovers.Count > 0)
        {
            // Round robin
            activeLoading = waitingRovers[0];
            waitingRovers.RemoveAt(0);
            waitingRovers.Add(activeLoading);
            status.Target(activeLoading);
            return;
        }

        // We dropped off a load
        if (converged && activeLoading != null)
        {
            resourcesLoaded[activeLoading].add(Constants.scoopCapacity);
            activeLoading = null;
            status.Target(minePosition);
            return;
        }
    }
}
