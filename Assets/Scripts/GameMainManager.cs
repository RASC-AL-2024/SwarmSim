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
    GameObject processingStation;
    [SerializeField]
    Transform lander_position;
    [SerializeField]
    int n_rovers = 1;
    [SerializeField]
    float time_scale = 10f;
    [SerializeField]
    Terrain terrain;
    [SerializeField]
    UdpSocket udp_socket;

    private Plane m_hPlane = new Plane(Vector3.up, Vector3.zero);
    private Dictionary<int, GameAgent> m_agentMap = new Dictionary<int, GameAgent>();

    float radius = 10f;
    Vector3 center_position;

    GameObject[] rovers;
    
    void Start()
    {
        Physics.autoSimulation = false;
        Simulator.Instance.setTimeStep(0.25f);
        Simulator.Instance.setAgentDefaults(10.0f, 10, 5.0f, 5.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

        // add in awake
        Simulator.Instance.processObstacles();

        spawnRovers();
    }

    void Awake()
    {
        Time.timeScale = time_scale;
        center_position = lander_position.position;
    }

    private void spawnRovers()
    {
        rovers = new GameObject[n_rovers];
        for (int i = 0; i < n_rovers; i++)
        {
            float angle = i * Mathf.PI * 2 / n_rovers;
            Vector3 position = randomPositionInCircle(center_position, radius, angle);
            position.y = 5;
            GameObject next_instance = CreateAgent(position);
            next_instance.GetComponent<GameAgent>().processingStation = processingStation;
            rovers[i] = next_instance;
        }
    }

    private Vector3 randomPositionInCircle(Vector3 center, float radius, float angle)
    {
        float randomAngle = angle + UnityEngine.Random.Range(-0.1f, 0.1f);
        float x = center.x + radius * Mathf.Cos(randomAngle);
        float z = center.z + radius * Mathf.Sin(randomAngle);
        float yoffset = 0.5f;
        float y = getTerrainHeight(x, z) + yoffset;
        return new Vector3(x, y, z);
    }

    private float getTerrainHeight(float x, float z)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 worldPosition = new Vector3(x, 0, z);

        float height = terrain.SampleHeight(worldPosition) + terrainPosition.y;
        return height;
    }

    private string serializeState()
    {
        string output_string = "";
        foreach (GameObject rover in rovers)
        {
            RoverState rover_state = rover.GetComponent<MineController>().rover_state;
            string state_string = JsonConvert.SerializeObject(rover_state);
            output_string += state_string + "\n";
        }

        return output_string;
    }

    GameObject CreateAgent(Vector3 agent_pos)
    {
        int sid = Simulator.Instance.addAgent(new Vector2(agent_pos.x, agent_pos.z));
        if (sid >= 0)
        {
            GameObject go = LeanPool.Spawn(agentPrefab, agent_pos, Quaternion.identity);
            GameAgent ga = go.GetComponent<GameAgent>();
            Assert.IsNotNull(ga);
            ga.sid = sid;
            m_agentMap.Add(sid, ga);
            return go;
        }
        return null;
    }

    private void Update()
    {
        Simulator.Instance.doStep();
    }
}