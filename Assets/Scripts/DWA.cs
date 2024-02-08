using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DWA : MonoBehaviour
{
    // use state instead of point

    public class Config
    {
        public float maxSpeed;
        public float minSpeed;
        public float maxYawrate;
        public float maxAccel;
        public float maxdYawrate;
        public float velocityResolution;
        public float yawrateResolution;
        public float dt;
        public float predictTime;
        public float heading;
        public float clearance;
        public float velocity;
    }

    public class Velocity
    {
        public float linear_vel;
        public float angular_vel;
    }

    public class PointCloud
    {
        public List<Vector2> points = new List<Vector2>();

        public PointCloud(int size)
        {
            for (int i = 0; i < size; i++)
            {
                points.Add(new Vector2(0f,0f));
            }
        }
    }

    public class DynamicWindow
    {
        public List<float> possible_v = new List<float>();
        public List<float> possible_w = new List<float>();
    }

    public class DWAPlanner
    {
        public static DynamicWindow CreateDynamicWindow(Velocity velocity, Config config)
        {
            DynamicWindow dynamic_window = new DynamicWindow();

            float min_v = Mathf.Max(config.minSpeed, velocity.linear_vel - config.maxAccel * config.dt);
            float max_v = Mathf.Min(config.maxSpeed, velocity.linear_vel + config.maxAccel * config.dt);
            float min_w = Mathf.Max(-config.maxYawrate, velocity.angular_vel - config.maxdYawrate * config.dt);
            float max_w = Mathf.Min(config.maxYawrate, velocity.angular_vel + config.maxdYawrate * config.dt);

            int n_possible_v = (int)((max_v - min_v) / config.velocityResolution);
            int n_possible_w = (int)((max_w - min_w) / config.yawrateResolution);

            for (int i = 0; i < n_possible_v; i++)
            {
                dynamic_window.possible_v.Add(min_v + i * config.velocityResolution);
            }

            for (int i = 0; i < n_possible_w; i++)
            {
                dynamic_window.possible_w.Add(min_w + i * config.yawrateResolution);
            }

            return dynamic_window;
        }

        public static State Motion(State old_state, Velocity velocity, float dt)
        {
            State new_state = new State();
            new_state.theta = old_state.theta + velocity.angular_vel * dt;
            new_state.pos.x = old_state.pos.x + velocity.linear_vel * Mathf.Cos(new_state.theta) * dt;
            new_state.pos.y = old_state.pos.y + velocity.linear_vel * Mathf.Sin(new_state.theta) * dt;
            return new_state;
        }

        public static float CalculateVelocityCost(Velocity velocity, Config config)
        {
            return config.maxSpeed - velocity.linear_vel;
        }

        public static float CalculateHeadingCost(State state, Vector2 goal)
        {
            float dx = goal.x - state.pos.x;
            float dy = goal.y - state.pos.y;
            float angle_err = Mathf.Atan2(dy, dx);
            float angle_cost = angle_err - state.theta;
            return Mathf.Abs(Mathf.Atan2(Mathf.Sin(angle_cost), Mathf.Cos(angle_cost)));
        }

        public static float CalculateClearanceCost(State state, Velocity velocity, PointCloud pointCloud, Config config)
        {
            State pred_state = state;
            float time = 0.0f;
            float minr = float.MaxValue;
            Vector2 delta;

            while (time < config.predictTime)
            {
                pred_state = Motion(pred_state, velocity, config.dt);

                foreach (var point in pointCloud.points)
                {
                    delta = pred_state.pos - point;
                    minr = delta.magnitude < minr ? delta.magnitude : minr;
                }
                time += config.dt;
            }
            return 1.0f / minr;
        }

        public static Velocity Planning(State pose, Velocity velocity, Vector2 goal, PointCloud pointCloud, Config config)
        {
            DynamicWindow dw = CreateDynamicWindow(velocity, config);
            Velocity pVelocity = new Velocity();
            State pred_state = pose;
            float totalCost = float.MaxValue;
            float cost;
            Velocity bestVelocity = new Velocity();

            for (int i = 0; i < dw.possible_v.Count; ++i)
            {
                for (int j = 0; j < dw.possible_w.Count; ++j)
                {
                    pred_state = pose;
                    pVelocity.linear_vel = dw.possible_v[i];
                    pVelocity.angular_vel = dw.possible_w[j];
                    pred_state = Motion(pred_state, pVelocity, config.predictTime);
                    cost =
                      config.velocity * CalculateVelocityCost(pVelocity, config) +
                      config.heading * CalculateHeadingCost(pred_state, goal) +
                      config.clearance * CalculateClearanceCost(pose, pVelocity, pointCloud, config);
                    if (cost < totalCost)
                    {
                        totalCost = cost;
                        bestVelocity = pVelocity;
                    }
                }
            }
            return bestVelocity;
        }
    }


    void Start()
    {
        Config config;

        config.maxSpeed = 5.0f;
        config.minSpeed = 0.0f;
        config.maxYawrate = 60.0f * Mathf.PI / 180.0f;
        config.maxAccel = 15.0f;
        config.maxdYawrate = 110.0f * Mathf.PI / 180.0f;
        config.velocityResolution = 0.1f;
        config.yawrateResolution = 1.0f * Mathf.PI / 180.0f;
        config.dt = 0.1f;
        config.predictTime = 3.0f;
        config.heading = 0.15f;
        config.clearance = 1.0f;
        config.velocity = 1.0f;


    }
    
    // ok, now the move is:
    // don't make everything static, make it a class, and add the config inside that?
    // then in the main controller, you would update the point cloud for every other rover using its state
    // then you can just do something

    void Update()
    {

    }
}
