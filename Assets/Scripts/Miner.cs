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

    [SerializeField]
    public float resourcesPerScoop;

    private List<Transform> waitingRovers;
    private IKStatus status;
    private Dictionary<Transform, float> resourcesLoaded;

    private Transform activeLoading;

    void Start()
    {
        initFailable();
        waitingRovers = new List<Transform>();
        status = new IKStatus(GetComponent<InverseKinematics>());
        resourcesLoaded = new Dictionary<Transform, float>();
    }

    public bool RegisterRover(Transform container)
    {
        if (broken)
            return false;

        Debug.LogFormat("{0}, registered: {1}", name, container);
        waitingRovers.Add(container);
        resourcesLoaded.Add(container, 0.0f);
        return true;
    }

    public float UnregisterRover(Transform container)
    {
        waitingRovers.Remove(container);
        float totalLoaded;
        resourcesLoaded.Remove(container, out totalLoaded);
        Debug.LogFormat("{0}, unregistered: {1} with {2} resources", name, container, totalLoaded);
        return totalLoaded;
    }

    void Update()
    {
        bool converged = status.Step();

        if (broken)
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
            resourcesLoaded[activeLoading] += resourcesPerScoop;
            activeLoading = null;
            status.Target(minePosition);
            return;
        }
    }
}
