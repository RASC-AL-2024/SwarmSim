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

    private List<Transform> waitingRovers;
    private IKStatus status;
    private Dictionary<Transform, Storage> resourcesLoaded;

    private Transform activeLoading;

    private BatteryModule batteryModule;

    void Start()
    {
        initFailable();
        waitingRovers = new List<Transform>();
        status = new IKStatus(GetComponent<InverseKinematics>());
        resourcesLoaded = new Dictionary<Transform, Storage>();

        batteryModule = gameObject.AddComponent<BatteryModule>();

        SingletonBehaviour<Planner>.Instance.registerMiner(this);
    }

    public bool RegisterRover(Transform container, Storage roverStorage)
    {
        if (broken || batteryModule.battery.empty())
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

        if (broken || batteryModule.battery.empty())
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
