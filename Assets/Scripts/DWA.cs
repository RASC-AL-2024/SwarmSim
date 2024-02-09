// #define DEBUG
#undef DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class DWA
{
    public class DynamicWindow
    {
        public List<float> possible_v = new List<float>();
        public List<float> possible_w = new List<float>();
    }

    public class DWAPlanner
    {

        static float maxSpeed = 0.5f;
        static float dt = 0.1f;
        static float predictTime = 9.0f;
        static float heading_weight = 0.3f;
        static float collision_weight = 60f;
        static float velocity_weight = 6.0f;
        static float goal_weight = 0;//2f;

        public static DynamicWindow CreateDynamicWindow(Velocity velocity)
        {
            DynamicWindow dynamic_window = new DynamicWindow();

            dynamic_window.possible_v.Add(-0.5f);
            dynamic_window.possible_v.Add(-0.3f);
            dynamic_window.possible_v.Add(0f);
            dynamic_window.possible_v.Add(0.3f);
            dynamic_window.possible_v.Add(0.5f);


            dynamic_window.possible_w.Add(-0.8f);
            dynamic_window.possible_w.Add(-0.5f);
            dynamic_window.possible_w.Add(-0.3f);
            dynamic_window.possible_w.Add(0f);
            dynamic_window.possible_w.Add(0.3f);
            dynamic_window.possible_w.Add(0.5f);
            dynamic_window.possible_w.Add(0.8f);

            return dynamic_window;
        }

        public static State Motion(State old_state, Velocity velocity, float dt)
        {
            State new_state = new State();
            Quaternion delta = Quaternion.Euler(0, velocity.angular_vel * dt, 0);
            new_state.rot = old_state.rot * delta;
            Vector3 forward_direction = new_state.rot * Vector3.forward;
            Vector3 travel_delta = forward_direction * velocity.linear_vel * dt;
            new_state.pos.x = old_state.pos.x + travel_delta.x;
            new_state.pos.y = old_state.pos.y + travel_delta.z;
            return new_state;
        }

        public static float CalculateVelocityCost(Velocity velocity)
        {
            return maxSpeed - Math.Abs(velocity.linear_vel);
        }

        public static float CalculateGoalCost(State state, Vector2 goal)
        {
            Vector2 delta = goal - state.pos;
            float final_cost = delta.magnitude;
            return final_cost;
        }

        private static Vector2 direction(float theta)
        {
            return new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
        }

    public static float CalculateHeadingCost(State state, Vector2 goal)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(goal.x - state.pos.x, 0, goal.y - state.pos.y));
            float thing = Quaternion.Angle(state.rot, targetRotation);
            float final_cost = Math.Abs(thing);
            return final_cost;
        }

        public static float CalculateClearanceCost(State state, Velocity velocity, PointCloud point_cloud, int id)
        {
            State pred_state = state;
            float time = 0.0f;
            float minr = float.MaxValue;
            Vector2 delta;

            while (time < predictTime)
            {
                pred_state = Motion(pred_state, velocity, dt);

                for(int i = 0; i < point_cloud.points.Count; i++)
                {
                    if (i == id) continue;
                    var point = point_cloud.points[i];
                    delta = pred_state.pos - point;
                    float curr_cost = delta.magnitude;
                    minr =  curr_cost < minr ? delta.magnitude : minr;
                }
                time += dt;
            }
            float final_cost = 1.0f / minr;
            return final_cost;
        }

        private static void PRINT(string str, int id)
        {
            Debug.Log("Rover " + id + ": " + str);
        }

        public static Velocity Planning(State pose, Velocity velocity, Vector2 goal, PointCloud point_cloud, int id)
        {
            DynamicWindow dw = CreateDynamicWindow(velocity);
            Velocity pVelocity = new Velocity();
            State pred_state = pose;
            float totalCost = float.MaxValue;
            float cost;
            Velocity bestVelocity = new Velocity();
            for (int i = 0; i < dw.possible_v.Count; ++i)
            {
                for (int j = 0; j < dw.possible_w.Count; ++j)
                {
                    pVelocity.linear_vel = dw.possible_v[i];
                    pVelocity.angular_vel = dw.possible_w[j];
                    pred_state = Motion(pose, pVelocity, predictTime);
                    float heading_cost = heading_weight * CalculateHeadingCost(pred_state, goal);
                    float goal_cost = goal_weight * CalculateGoalCost(pred_state, goal);
                    float velocity_cost = velocity_weight * CalculateVelocityCost(pVelocity);
                    float collision_cost = collision_weight * CalculateClearanceCost(pose, pVelocity, point_cloud, id);

#if DEBUG
                    PRINT("heading_cost: " + heading_cost, id);
                    PRINT("goal_cost: " + goal_cost, id);
                    PRINT("velocity_cost: " + velocity_cost, id);
                    PRINT("collision_cost: " + collision_cost, id);
#endif
                    cost = heading_cost + goal_cost + velocity_cost + collision_cost;
                    if (cost < totalCost)
                    {
                        totalCost = cost;
                        bestVelocity.linear_vel = pVelocity.linear_vel;
                        bestVelocity.angular_vel = pVelocity.angular_vel;
                    }
                }
            }
            return bestVelocity;
        }
    }


}
