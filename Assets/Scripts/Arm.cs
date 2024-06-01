using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

// public class Arm2 : MonoBehaviour
// {
//     public Transform target;
//     public Transform basePosition;
// 
//     private Serial<Message> serial;
//     private ArticulationBody root;
//     private List<float> targets = new List<float> { 0, 0, 0, 0 };
//     private List<ArticulationBody> servos;
// 
//     private static float[] directionMultipliers = { 1, -1, 1, 1 };
// 
//     Interpolate interpolate;
//     float lastTime = 0;
//     List<float> current = new List<float> { 0, 0, 0, 0 };
// 
//     void OnEnable()
//     {
//         root = GetComponentInParent<ArticulationBody>();
//         root.GetDriveTargets(targets);
//         interpolate = new Interpolate(targets, targets, 0, 1);
//         servos = GetComponentsInChildren<ArticulationBody>().Skip(1).ToList();
// 
//         var parsers = new List<Func<string, Message>>{
//           s => SetServoMessage.parse(s),
//           s => DebugMessage.parse(s)
//         };
//         serial = Serial<Message>.Default(parsers);
//         serial.MessageReceived += (_, msg) =>
//         {
//             switch (msg)
//             {
//                 case SetServoMessage m:
//                     Debug.Log($"New angles: {String.Join(", ", m.servo_state.Select(x => x.ToString()))}");
//                     for (int i = 0; i < targets.Count; ++i)
//                     {
//                         targets[i] = m.servo_state[i] * Mathf.Rad2Deg * directionMultipliers[i];
//                     }
//                     interpolate = new Interpolate(new List<float>(current), targets, lastTime, lastTime + 8f);
//                     break;
//                 case DebugMessage m:
//                     Debug.Log($"Arduino: {m.message}");
//                     break;
//             }
//         };
//     }
// 
//     void OnDisable()
//     {
//         serial.Dispose();
//     }
// 
//     public void Sync()
//     {
//         var relativePosition = Quaternion.Inverse(basePosition.rotation) * (target.position - basePosition.position);
//         var relativeRotation = Quaternion.Inverse(basePosition.rotation) * target.rotation;
// 
//         var pose = Matrix4x4.TRS(100 * relativePosition, relativeRotation, new Vector3(1, 1, 1));
// 
//         // We scale by 100 since the arduino deals in mm
//         var command = new PositionCommand(pose);
//         serial.port.WriteLine(command.Serialize());
//     }
// 
//     void Update()
//     {
//         lastTime = Time.time;
//         root.GetDriveTargets(current);
// 
//         for (int i = 0; i < servos.Count; ++i)
//         {
//             servos[i].SetDriveTarget(ArticulationDriveAxis.X, interpolate.getValue(Time.time, i));
//         }
//     }
// }
// 
// [CustomEditor(typeof(Arm))]
// public class ArmEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         base.OnInspectorGUI();
//         Arm arm = (Arm)target;
//         if (GUILayout.Button("Sync"))
//         {
//             arm.Sync();
//         }
//     }
// }


public interface Command
{
    string Serialize();
}
public record PositionCommand(Matrix4x4 pos) : Command
{
    public string Serialize() => $"p {pos.m00},{pos.m01},{pos.m02},{pos.m03},{pos.m10},{pos.m11},{pos.m12},{pos.m13},{pos.m20},{pos.m21},{pos.m22},{pos.m23},{pos.m30},{pos.m31},{pos.m32},{pos.m33}";
}

public interface Message { };
public record SetServoMessage(List<float> servo_state) : Message
{
    public static SetServoMessage? parse(string data)
    {
        var parts = data.Split(' ');
        if (parts[0] != "servo")
            return null;
        try
        {
            return new SetServoMessage(parts.Skip(1).Select(float.Parse).ToList());
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
public record DebugMessage(string message) : Message
{
    public static DebugMessage? parse(string data)
    {
        return new DebugMessage(data);
    }
}


public record Interpolate(List<float> start, List<float> end, float startTime, float endTime)
{
    public float getValue(float time, int i)
    {
        var t = Mathf.Clamp((time - startTime) / (endTime - startTime), 0, 1);
        return end[i] * t + (1 - t) * start[i];
    }
}

public class Arm : MonoBehaviour
{
    public Transform target;
    public Transform basePosition;

    private Serial<Message> serial;
    private ArticulationBody root;
    private List<float> targets = new List<float> { 0, 0, 0, 0 };
    private List<ArticulationBody> servos;

    private static float[] directionMultipliers = { 1, -1, 1, 1 };

    Interpolate interpolate;
    float lastTime = 0;
    List<float> current = new List<float> { 0, 0, 0, 0 };

    void OnEnable()
    {
        root = GetComponentInParent<ArticulationBody>();
        root.GetDriveTargets(targets);
        interpolate = new Interpolate(targets, targets, 0, 1);
        servos = GetComponentsInChildren<ArticulationBody>().Skip(1).ToList();

        var parsers = new List<Func<string, Message>>{
          s => SetServoMessage.parse(s),
          s => DebugMessage.parse(s)
        };
        serial = Serial<Message>.Default(parsers);
        serial.MessageReceived += (_, msg) =>
        {
            switch (msg)
            {
                case SetServoMessage m:
                    Debug.Log($"New angles: {String.Join(", ", m.servo_state.Select(x => x.ToString()))}");
                    for (int i = 0; i < targets.Count; ++i)
                    {
                        targets[i] = m.servo_state[i] * Mathf.Rad2Deg * directionMultipliers[i];
                    }
                    interpolate = new Interpolate(new List<float>(current), targets, lastTime, lastTime + 8f);
                    break;
                case DebugMessage m:
                    Debug.Log($"Arduino: {m.message}");
                    break;
            }
        };
    }

    void OnDisable()
    {
        serial.Dispose();
    }

    public void Sync()
    {
        var relativePosition = Quaternion.Inverse(basePosition.rotation) * (target.position - basePosition.position);
        var relativeRotation = Quaternion.Inverse(basePosition.rotation) * target.rotation;

        var pose = Matrix4x4.TRS(100 * relativePosition, relativeRotation, new Vector3(1, 1, 1));

        // We scale by 100 since the arduino deals in mm
        var command = new PositionCommand(pose);
        serial.port.WriteLine(command.Serialize());
    }

    void Update()
    {
        lastTime = Time.time;
        root.GetDriveTargets(current);

        for (int i = 0; i < servos.Count; ++i)
        {
            servos[i].SetDriveTarget(ArticulationDriveAxis.X, interpolate.getValue(Time.time, i));
        }
    }
}

[CustomEditor(typeof(Arm))]
public class ArmEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Arm arm = (Arm)target;
        if (GUILayout.Button("Sync"))
        {
            arm.Sync();
        }
    }
}

