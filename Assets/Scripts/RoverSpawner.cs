using UnityEngine;
using RVO;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class RoverSpawner
{
    private static GameObject CreateAgent(GameObject agentPrefab, Vector3 agent_pos)
    {
        int sid = Simulator.Instance.addAgent(new Vector2(agent_pos.x, agent_pos.z));
        if (sid >= 0)
        {
            GameObject go = Object.Instantiate(agentPrefab, agent_pos, Quaternion.identity);
            GameAgent ga = go.GetComponent<GameAgent>();
            Assert.IsNotNull(ga);
            ga.sid = sid;
            return go;
        }
        return null;
    }

    private static GameObject getProcessingStation()
    {
        GameObject processing_station = GameObject.Find("ProcessingStation");
        if (processing_station == null)
        {
            throw new System.Exception("Processing station is not found");
        }
        return processing_station;
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

    public static List<GameAgent> spawnRoversFromLander(GameObject agentPrefab, float spawn_radius, int n_rovers)
    {
        GameObject processing_station = getProcessingStation();
        Vector3 spawn_center = processing_station.transform.position;
        return spawnRoversFromLocation(agentPrefab, spawn_center, spawn_radius, n_rovers);
    }

    public static List<GameAgent> spawnRoversFromLocation(GameObject agentPrefab, Vector3 spawn_center, float spawn_radius, int n_rovers)
    {
        List<GameAgent> rovers = new List<GameAgent>(n_rovers);
        Terrain terrain = getTerrain();
        for (int i = 0; i < n_rovers; i++)
        {
            float angle = i * Mathf.PI * 2 / n_rovers;
            Vector3 position = getRandomSpawnLocation(terrain, spawn_center, spawn_radius, angle);
            GameObject next_instance = CreateAgent(agentPrefab, position);
            GameAgent ga = next_instance.GetComponent<GameAgent>();
            rovers.Add(ga);
        }
        return rovers;
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
