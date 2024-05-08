using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// Compiler gets pissed off without this
namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}

public class Planner : SingletonBehaviour<Planner>
{
    public Transform processingStation;
    public CentralResources resources;

    private List<GameAgent> rovers = new List<GameAgent>();
    private List<Miner> miners = new List<Miner>();
    private Dictionary<GameAgent, Goal> goals = new Dictionary<GameAgent, Goal>();
    private List<FailableModule> broken = new List<FailableModule>();

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

    public void registerRover(GameAgent rover)
    {
        rovers.Add(rover);
        dispatch(rover);

        Debug.LogFormat("Registered rover: {0}", rover.sid);
    }

    public void registerMiner(Miner miner)
    {
        miners.Add(miner);

        Debug.LogFormat("Registered miner: {0}", miner.name);
    }

    private GameAgent roverForRepair(FailableModule module)
    {
        return goals.FirstOrDefault(kv => kv.Value is RepairGoal rg && rg.broken == module).Key;
    }

    public void handleEvent(Event e)
    {
        switch (e)
        {
            case FinishedUnloading a:
                Debug.LogFormat("Rover {0} finished unloading", a.rover.sid);
                dispatch(a.rover);
                break;

            case FinishedCharging a:
                Debug.LogFormat("Rover {0} finished charging", a.rover.sid);
                dispatch(a.rover);
                break;

            case FinishedRepairing a:
                var rover = roverForRepair(a.module);
                Debug.LogFormat("Rover {0} finished repairing", rover.sid);
                dispatch(rover);
                broken.Remove(a.module);
                break;

            case FinishedLoading a:
                Debug.LogFormat("Rover {0} finished loading", a.rover.sid);
                dispatchUnload(a.rover);
                break;

            case Arrived a:
                Debug.LogFormat("Rover {0} arrived", a.rover.sid);
                doGoal(a.rover);
                break;

            case Broken a:
                Debug.LogFormat("Module {0} broken", a.module.name);
                broken.Add(a.module);
                break;
        }
    }

    int comp(Miner a, Miner b)
    {
        return 1;
    }

    private Miner findMiner()
    {
        List<int> weights = miners.Select(x => 1 / (x.waitingRovers.Count()+1)).ToList();
        double weight_sum = (double)weights.Sum();

        List<double> norm_weights = weights.Select(x => (double)x / weight_sum).ToList();

        System.Random rand = new System.Random();
        double randomNumber = rand.NextDouble();
        double cum = 0.0;

        for (int i = 0; i < norm_weights.Count; i++)
        {
            cum += norm_weights[i];
            if (randomNumber < cum)
            {
                return miners[i];
            }
        }
        throw new Exception("unable to sample miner");
    }

    public void dispatch(GameAgent rover)
    {
        if (!rover.GetComponentInParent<LoadModule>().Dirt.empty())
        {
            dispatchUnload(rover);
            return;
        }

        if (resources.SpareModules.current >= 1 && broken.Count > 0)
        {
            foreach (var module in broken)
            {
                // A rover has already been dispatched
                if (roverForRepair(module) != null)
                    continue;

                resources.SpareModules.remove(1f);
                goals[rover] = new RepairGoal(module);
                rover.setGoalPosition(new Vector2(module.realCenter.x, module.realCenter.z));
                return;
            }
        }
        
        if(miners.Count > 0)
        {

            var miner = findMiner();
            goals[rover] = new LoadGoal(miner);
            rover.setGoalPosition(new Vector2(miner.center.position.x, miner.center.position.z));
        }
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
                rover.GetComponentInParent<BatteryModule>().SourceBattery = resources.Battery;
                break;
            case UnloadGoal:
                rover.GetComponentInParent<LoadModule>().Unload = resources.Dirt;
                break;
        }
    }

    public void cancelGoal(GameAgent rover)
    {
        foreach (var behaviour in rover.GetComponents<Behaviour>())
            behaviour.Cancel();
        Debug.LogFormat("Cancelled goal for {0}", rover.sid);
    }

    void Update()
    {
        foreach (var rover in rovers)
        {
            if (rover.GetComponentInParent<BatteryModule>().Battery.isLow() && !(goals[rover] is ChargeGoal))
            {
                cancelGoal(rover);
                goals[rover] = new ChargeGoal();
                rover.setGoalPosition(new Vector2(processingStation.position.x, processingStation.position.z));
                Debug.LogFormat("Rover {0} moving to charge", rover.sid);
                return;
            }
        }
    }
}
