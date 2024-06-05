using RobotDynamics.Controller;
using RobotDynamics.MathUtilities;
using RobotDynamics.Robots;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevoluteRobot : RobotBase
{
    public List<double> angles;
    public GameObject Target;


    // Start is called before the first frame update
    void Awake()
    {
        Vector[] p = {
          new Vector(0.112, 0.659, -0.121),
          new Vector(0.112, 0.881752, -0.2577171),
          new Vector(0.112, 1.646, -0.441),
          new Vector(0.112, 1.986, -0.121),
          new Vector(0.112, 2.212, 0.122),
          new Vector(0.112, 2.548, 0.431),
          new Vector(0.094, 2.774752, -0.157)
        };

        Robot = new Robot()
          .AddJoint('y', p[0])
          .AddJoint('z', p[1] - p[0])
          .AddJoint('z', p[2] - p[1])
          .AddJoint('y', p[3] - p[2])
          .AddJoint('z', p[4] - p[3])
          .AddJoint('y', p[5] - p[4])
          .AddJoint('z', p[6] - p[5]);
    }

    // Update is called once per frame
    void Update()
    {
        if (EnableInverseKinematics)
        {
            FollowTargetOneStep(Target);
        }

        if (lerp != null && Time.time <= lerp.endTime)
        {
            SetQ(lerp.Get(Time.time));
        }

        Robot.JointController.ReportNewFrame(Time.deltaTime);
    }
}
