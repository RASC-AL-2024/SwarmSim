using UnityEngine;
using System.Collections.Generic;

public class CentralResources : MonoBehaviour
{
    public Battery battery = new Battery(Constants.centralBatteryCapacity);
    public Storage dirt = new Storage(Constants.centralDirtCapacity);
    public int spareModules = 0;

    void Update()
    {
        // Solar power needs a sine weighting
        float dayCycle = Mathf.Sin(Time.time / Constants.dayLength * 2 * Mathf.PI);
        if (dayCycle > 0)
            battery.add(Constants.peakSolarPower * dayCycle, Time.deltaTime);
    }
}

// battery discharge function of actual joint movements

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}

public class Planner : SingletonBehaviour<Planner>
{
    public Transform processingStation;
    public CentralResources resources;

    private List<GameAgent> gameAgents;
    private List<Miner> miners;
    private Dictionary<GameAgent, Goal> goals;
    private Dictionary<FailableModule, GameAgent> broken;

    public abstract record Goal;
    public record LoadGoal(Miner miner) : Goal;
    public record RepairGoal(FailableModule broken) : Goal;
    public record UnloadGoal : Goal;
    public record ChargeGoal : Goal;

    public abstract record Event;
    public record Arrived(GameAgent rover) : Event;
    public record FinishedCharging(GameAgent rover) : Event;
    public record FinishedUnloading(GameAgent rover) : Event;
    public record FinishedLoading(GameAgent rover) : Event;
    public record Broken(FailableModule module) : Event;
    public record FinishedRepairing(FailableModule module) : Event;

    public void handleEvent(Event e)
    {
        switch (e)
        {
            case FinishedUnloading a:
                dispatch(a.rover);
                break;

            case FinishedCharging a:
                dispatch(a.rover);
                break;

            case FinishedRepairing a:
                var rover = broken[a.module];
                dispatch(rover);
                broken.Remove(a.module);
                break;

            case FinishedLoading a:
                dispatchUnload(a.rover);
                break;

            case Arrived a:
                doGoal(a.rover);
                break;

            case Broken a:
                broken.Add(a.module, null);
                break;
        }
    }

    public void dispatch(GameAgent rover)
    {
        var batteryModule = rover.GetComponentInParent<BatteryModule>();

        if (batteryModule.battery.isLow())
        {
            goals[rover] = new ChargeGoal();
            rover.setGoalPosition(new Vector2(processingStation.position.x, processingStation.position.z));
            return;
        }

        if (resources.spareModules > 0 && broken.Count > 0)
        {
            foreach (var module in broken.Keys)
            {
                // A rover has already been dispatched
                if (broken[module] != null)
                    continue;

                --resources.spareModules;
                broken[module] = rover;
                goals[rover] = new RepairGoal(module);
                rover.setGoalPosition(new Vector2(module.realCenter.x, module.realCenter.z));
                return;
            }
        }

        var miner = miners[UnityEngine.Random.Range(0, miners.Count)];
        goals[rover] = new LoadGoal(miner);
        rover.setGoalPosition(new Vector2(miner.center.position.x, miner.center.position.z));
    }

    public void dispatchUnload(GameAgent rover)
    {
        goals[rover] = new UnloadGoal();
        rover.setGoalPosition(new Vector2(processingStation.position.x, processingStation.position.z));
    }

    public void doGoal(GameAgent rover)
    {
        switch (goals[rover])
        {
            case LoadGoal g:
                rover.GetComponentInParent<LoadModule>().setMiner(g.miner);
                break;
            case RepairGoal g:
                rover.GetComponentInParent<RepairModule>().setRepairModule(g.broken);
                break;
            case ChargeGoal:
                rover.GetComponentInParent<BatteryModule>().sourceBattery = resources.battery;
                break;
            case UnloadGoal:
                rover.GetComponentInParent<LoadModule>().unload = resources.dirt;
                break;
        }
    }
}
