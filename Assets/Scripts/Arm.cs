using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;
using System.Linq;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetServoMessage2
{
    public byte n_servos;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public float[] servo_state;
}

public interface Command
{
    string Serialize();
}
public record PositionCommand(Vector3 position) : Command
{
    public string Serialize() => $"p {position.x},{position.y},{position.z}";
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

public class Arm : MonoBehaviour
{
    public Transform target;
    public Transform basePosition;

    private Serial<Message> serial;
    private ArticulationBody root;
    private List<float> targets = new List<float> { 0, 0, 0, 0 };
    private List<ArticulationBody> servos;

    // Base turns counter-clockwise around +y, fabrik seems to give cursed directions???
    private static float[] directionMultipliers = { 1, -1, -1, -1 };

    void OnEnable()
    {
        root = GetComponentInParent<ArticulationBody>();
        root.GetDriveTargets(targets);
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

        // We scale by 100 since the arduino deals in mm
        var command = new PositionCommand(relativePosition * 100);
        Debug.Log(command.Serialize());
        serial.port.WriteLine(command.Serialize());
    }

    void Update()
    {
        for (int i = 0; i < servos.Count; ++i)
        {
            servos[i].SetDriveTarget(ArticulationDriveAxis.X, targets[i]);
        }
        // Unity is garbage and this gives garbage values????
        // root.SetDriveTargets(targets);
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

