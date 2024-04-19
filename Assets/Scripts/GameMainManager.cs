using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;

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

    void Start()
    {
        Time.timeScale = time_scale;

        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        rovers = RoverSpawner.spawnRovers(agentPrefab, spawn_radius, n_rovers);

        // Maps broken module to rover currently repairing it
        brokenModules = new Dictionary<FailableModule, bool>();

    }

    private void Update()
    {
        Simulator.Instance.doStep();
    }
}
