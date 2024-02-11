using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
using Random = System.Random;

public class GameAgent_og : MonoBehaviour
{
    [HideInInspector] public int sid = -1;

    /** Random number generator. */
    private Random m_random = new Random();

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (sid >= 0)
        {
            Vector2 pos = Simulator.Instance.getAgentPosition(sid);
            Vector2 vel = Simulator.Instance.getAgentPrefVelocity(sid);
            Vector2 actual_vel = Simulator.Instance.getAgentVelocity(sid);

            if (Math.Abs(vel.x) > 0.01f && Math.Abs(vel.y) > 0.01f)
                transform.forward = new Vector3(vel.x, 0, vel.y).normalized;

            if(sid == 1)
            {
                Debug.Log("acutal vel: " + actual_vel);
            }
            transform.position += new Vector3(actual_vel.x, 0, actual_vel.y);
            Simulator.Instance.setAgentPosition(sid, new Vector2(transform.position.x, transform.position.z));
        }

        if (!Input.GetMouseButton(1))
        {
            Simulator.Instance.setAgentPrefVelocity(sid, new Vector2(0, 0));
            return;
        }

        Vector2 goalVector = GameMainManager_og.Instance.mousePosition - Simulator.Instance.getAgentPosition(sid);
        if (RVOMath.absSq(goalVector) > 1.0f)
        {
            goalVector = RVOMath.normalize(goalVector);
        }

        Simulator.Instance.setAgentPrefVelocity(sid, goalVector);

        /* Perturb a little to avoid deadlocks due to perfect symmetry. */
        float angle = (float) m_random.NextDouble()*2.0f*(float) Math.PI;
        float dist = (float) m_random.NextDouble()*0.0001f;

        Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
                                                     dist*
                                                     new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle)));
    }
}