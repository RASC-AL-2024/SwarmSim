using RobotDynamics.Controller;
using RobotDynamics.MathUtilities;
using RobotDynamics.Robots;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearRobot : RobotBase
{
    public GameObject Target;


    // Start is called before the first frame update
    void Awake()
    {
        Robot = new RobotDynamics.Robots.Robot();
        Robot
            .AddJoint('y', new Vector(0, 0.03293461, 0))
            .AddLinearJoint(new Vector(0, 0, 0), new Vector(0, 1,0 ))
            .AddJoint('y', new Vector(0.2241943, 0.4691764, 0))
            .AddJoint('y', new Vector(0.1998537, -0.05284593, 0));
    }

    // Update is called once per frame
    void Update()
    {
        if (EnableForwardKinematics)
        {
            SetQ(ForwardKinematicsQ);
        }

        if (EnableInverseKinematics)
        {
            FollowTargetOneStep(Target);
        }

        Robot.JointController.ReportNewFrame(Time.deltaTime);
    }
}
