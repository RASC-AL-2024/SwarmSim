#define USE_PLANNER

using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;
using Random = System.Random;
using Newtonsoft.Json;

public class GameMainManager : SingletonBehaviour<GameMainManager>
{
    public GameObject agentPrefab;
    public Miner[] miners;

    [SerializeField]
    int n_rovers = 1;
    [SerializeField]
    float spawn_radius = 10f;
    [SerializeField]
    float time_scale = 10f;

    float planner_update_freq = 1f;

    [SerializeField]
    UdpSocket udp_socket;

    GameAgent[] rovers;

    public Dictionary<FailableModule, bool> brokenModules;
    public float totalResources = 1000.0f; // roughly grams??

    void Start()
    {
        Time.timeScale = time_scale;

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        rovers = RoverSpawner.spawnRovers(agentPrefab, spawn_radius, n_rovers);

        // Maps broken module to rover currently repairing it
        brokenModules = new Dictionary<FailableModule, bool>();

        // jank
        foreach (var rover in rovers)
          foreach (var miner in miners)
            rover.miners.Add(miner);

#if USE_PLANNER
        udp_socket.OnPlannerInput += ProcessPlannerInput;
        StartCoroutine(sendStateToPlanner());
#endif
    }

    IEnumerator sendStateToPlanner()
    {
        yield return new WaitForSeconds(1f);
        while(true)
        {
            string output_string = serializeState();
            udp_socket.SendData(output_string);
            yield return new WaitForSeconds(planner_update_freq);
        }
    }

    private void ProcessPlannerInput(string planner_input)
    {
        PlannerInterface.applyInputs(rovers, planner_input);
    }

    private string serializeState()
    {
        string output_string = "";
        foreach (GameAgent agent in rovers)
        {
            RoverState rover_state = agent.rover_state;
            rover_state.serializeObject();
            string state_string = JsonConvert.SerializeObject(rover_state);
            output_string += state_string + "\n";
        }

        return output_string;
    }

    private void Update()
    {
        Simulator.Instance.doStep();
    }
}
