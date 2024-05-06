using UnityEngine;
using RVO;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections.Generic;
using System;
using static UnityEditor.PlayerSettings;

public class RoverSpawner
{
    private static void createRobot(GameObject agentPrefab, Vector3 agent_pos)
    {
        GameObject go = UnityEngine.Object.Instantiate(agentPrefab, agent_pos, Quaternion.identity);
        GameAgent ga = go.GetComponent<GameAgent>();

        if(ga != null)
        {
            int sid = Simulator.Instance.addAgent(new Vector2(agent_pos.x, agent_pos.z));
            if(sid < 0)
            {
                throw new System.Exception("Could not add rover to simulator");
            }
            ga.sid = sid;
        }
    }

    private static Terrain getTerrain()
    {
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        if (terrain == null)
        {
            throw new System.Exception("Processing station is not found");
        }
        return terrain;
    }
    
    public static void spawnRobots(GameObject prefab, Vector3 spawn_center, int n_robots, float spawn_radius = 0.1f)
    {
        Terrain terrain = getTerrain();
        for (int i = 0; i < n_robots; i++)
        {
            float angle = i * Mathf.PI * 2 / n_robots;
            Vector3 position = getRandomSpawnLocation(terrain, spawn_center, spawn_radius, angle);
            createRobot(prefab, position); 
        }
    }

    private static Vector3 getRandomSpawnLocation(Terrain terrain, Vector2 center, float radius, float angle)
    {
        float y_offset = 0.5f;
        Vector2 position_2d = randomPositionInCircle(center, radius, angle);
        float height = getTerrainHeight(terrain, position_2d);
        Vector3 spawn_position = new Vector3(position_2d.x, height + y_offset, position_2d.y);
        return spawn_position;
    }

    private static Vector2 randomPositionInCircle(Vector3 center, float spawn_radius, float angle)
    {
        float randomAngle = angle + UnityEngine.Random.Range(-0.1f, 0.1f);
        float x = center.x + spawn_radius * Mathf.Cos(randomAngle);
        float z = center.z + spawn_radius * Mathf.Sin(randomAngle);
        return new Vector2(x, z);
    }

    private static float getTerrainHeight(Terrain terrain, Vector2 position_2d)
    {

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 worldPosition = new Vector3(position_2d.x, 0, position_2d.y);

        float height = terrain.SampleHeight(worldPosition) + terrainPosition.y;
        return height;
    }
}
