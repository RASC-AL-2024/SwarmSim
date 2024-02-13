//#define NO_PHYSICS
#undef NO_PHYSICS

using System;
using System.Collections;
using System.Collections.Generic;
using Lean;
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

    [SerializeField]
    int n_rovers = 1;
    [SerializeField]
    float spawn_radius = 10f;
    [SerializeField]
    float time_scale = 10f;

    [SerializeField]
    UdpSocket udp_socket;


    GameObject[] rovers;
    
    void Start()
    {
#if NO_PHYSICS
         Physics.autoSimulation = false;
#endif
        Time.timeScale = time_scale;

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        // add in awake
        // Simulator.Instance.processObstacles();

        rovers = RoverSpawner.spawnRovers(agentPrefab, spawn_radius, n_rovers);
    }

    private string serializeState()
    {
        string output_string = "";
        foreach (GameObject rover in rovers)
        {
            RoverState rover_state = rover.GetComponent<GameAgent>().rover_state;
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