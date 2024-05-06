using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using RVO;
using UnityEngine;

public class GameMainManager : SingletonBehaviour<GameMainManager>
{
    public GameObject rover_prefab;
    public GameObject arm_prefab;

    [SerializeField]
    int n_rovers = 1;

    [SerializeField]
    int n_arms = 1;

    [SerializeField]
    float rover_spawn_radius = 10f;

    [SerializeField]
    float arm_spawn_radius = 10f;

    [SerializeField]
    float time_scale = 10f;

    [SerializeField]
    Planner planner;

    [SerializeField]
    Transform lander_location;

    [SerializeField]
    Transform new_rover_location;

    [SerializeField]
    Transform new_arm_location;

    [SerializeField]
    UdpSocket udp_socket;


    // public Dictionary<FailableModule, bool> brokenModules;
    public float totalResources = 1000.0f; // roughly grams??

    // Only non-null if there is impact
    // Just poll this lol
    public Transform impact = null;

    private double arm_rover_ratio = 1;

    private int ARM_MODULES = 2;
    private int ROVER_MODULES = 2;

    void Awake()
    {
        Time.timeScale = time_scale;

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        RoverSpawner.spawnRobots(arm_prefab, new_arm_location.position, n_arms, arm_spawn_radius);
        RoverSpawner.spawnRobots(rover_prefab, lander_location.position, n_rovers, rover_spawn_radius);
    }

    private bool checkSpareModules(int n_modules)
    {
        return planner.resources.SpareModules.current >= n_modules;
    }

    private void createNewArm()
    {
        Debug.Log("creating new arm");
        planner.resources.SpareModules.remove(ARM_MODULES);
        RoverSpawner.spawnRobots(arm_prefab, new_arm_location.position, 1);
        new_arm_location.position = new_arm_location.position - new Vector3(4,0,0);
        n_arms++;
    }

    private void createNewRover()
    {
        Debug.Log("creating new rover");
        planner.resources.SpareModules.remove(ROVER_MODULES);
        RoverSpawner.spawnRobots(rover_prefab, new_rover_location.position, 1);
        n_rovers++;
    }

    private void createNewRobot()
    {
        double curr_arm_rover_ratio = (double)n_arms / (double)n_rovers;
        if(curr_arm_rover_ratio > arm_rover_ratio)
        {
            if (checkSpareModules(ROVER_MODULES))
            {
                createNewRover();
            }
        } else
        {
            if (checkSpareModules(ARM_MODULES))
            {
                createNewArm();
            }
        }
    }

    private void Update()
    {
        createNewRobot();
        Simulator.Instance.doStep();
    }
}
