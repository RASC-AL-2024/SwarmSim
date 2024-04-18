﻿#define USE_PLANNER

using System.IO;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using Newtonsoft.Json;

public class GameMainManager : SingletonBehaviour<GameMainManager>
{
    public GameObject agentPrefab;

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

    // Only non-null if there is impact
    // Just poll this lol
    public Transform impact = null;

    private StreamWriter csvWriter;

    void Start()
    {
        Time.timeScale = time_scale;

        csvWriter = new StreamWriter("resourceData.csv", true);

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        rovers = RoverSpawner.spawnRovers(agentPrefab, spawn_radius, n_rovers);

        // Maps broken module to rover currently repairing it
        brokenModules = new Dictionary<FailableModule, bool>();

        StartCoroutine(logResource());

#if USE_PLANNER
        udp_socket.OnPlannerInput += ProcessPlannerInput;
        StartCoroutine(sendStateToPlanner());
#endif
    }

    IEnumerator logResource()
    {
        while (true)
        {
            csvWriter.WriteLine(totalResources.ToString());
            csvWriter.Flush();
            yield return new WaitForSeconds(60f);
        }
    }

    IEnumerator sendStateToPlanner()
    {
        yield return new WaitForSeconds(1f);
        while (true)
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
