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

    void Awake()
    {
        Time.timeScale = time_scale;

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));
        Simulator.is_fast = Constants.isFast;

        RobotSpawner.spawnRobots(arm_prefab, new_arm_location.position, n_arms, arm_spawn_radius);
        RobotSpawner.spawnRobots(rover_prefab, lander_location.position, n_rovers, rover_spawn_radius);
    }

    private bool checkSpareModules(int n_modules)
    {
        return planner.resources.SpareModules.current >= n_modules;
    }

    private void tryCreateNewArm()
    {
        if (!checkSpareModules(Constants.NroverModules) || n_arms >= Constants.targetArms)
        {
            return;
        }

        Vector3 delta = new Vector3(4, 0, 0);
        planner.resources.SpareModules.remove(Constants.NarmModules);
        RobotSpawner.spawnRobots(arm_prefab, new_arm_location.position, 1);
        new_arm_location.position = new_arm_location.position - delta;
        n_arms++;
    }

    private void tryCreateNewRover()
    {
        if (!checkSpareModules(Constants.NroverModules) || n_rovers >= Constants.targetRovers)
        {
            return;
        }

        planner.resources.SpareModules.remove(Constants.NroverModules);
        RobotSpawner.spawnRobots(rover_prefab, new_rover_location.position, 1);
        n_rovers++;
    }

    private void createNewRobot()
    {
        double curr_arm_rover_ratio = (double)n_arms / (double)n_rovers;
        if (n_rovers * Constants.armRoverRatio < n_arms || Constants.targetArms <= n_arms)
        {
            tryCreateNewRover();
        }
        else
        {
            tryCreateNewArm();
        }
    }

    private void Update()
    {
        createNewRobot();
        Simulator.Instance.doStep();
    }
}
