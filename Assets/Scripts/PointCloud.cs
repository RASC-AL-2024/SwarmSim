using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloud
{
    public static PointCloud g_point_cloud;

    public List<Vector2> points = new List<Vector2>();

    public PointCloud(int size=0)
    {
        for (int i = 0; i < size; i++)
        {
            points.Add(new Vector2(0f, 0f));
        }
    }
}