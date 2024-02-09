using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class MainController : MonoBehaviour
{
    [SerializeField]
    GameObject rover_prefab;
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

    float radius = 10f;
    Vector3 center_position;

    GameObject[] rovers;

    void Awake()
    {
        Time.timeScale = time_scale;
        center_position = lander_position.position;
        PointCloud.g_point_cloud = new PointCloud(n_rovers+1);
        spawnRovers();
    }

    private void spawnRovers()
    {
        rovers = new GameObject[n_rovers];
        for (int i = 0; i < n_rovers; i++)
        {
            float angle = i * Mathf.PI * 2 / n_rovers;
            Vector3 position = randomPositionInCircle(center_position, radius, angle);
            GameObject next_instance = Instantiate(rover_prefab, position, Quaternion.identity);
            next_instance.GetComponent<MineController>().id = i + 1;
            next_instance.GetComponent<MineController>().processingStation = processingStation;

            rovers[i] = next_instance;
        }
    }

    private Vector3 randomPositionInCircle(Vector3 center, float radius, float angle)
    {
        float randomAngle = angle + Random.Range(-0.1f, 0.1f);
        float x = center.x + radius * Mathf.Cos(randomAngle);
        float z = center.z + radius * Mathf.Sin(randomAngle);
        float y_offset = 0.5f;
        float y = getTerrainHeight(x, z) + y_offset;
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
        foreach(GameObject rover in rovers)
        {
            RoverState rover_state = rover.GetComponent<MineController>().rover_state;
            string state_string = JsonConvert.SerializeObject(rover_state);
            output_string += state_string + "\n";
        }

        return output_string;
    }

    void Update()
    {
 
    }
}


