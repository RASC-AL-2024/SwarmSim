using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetServoMessage
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

public class Arm : MonoBehaviour
{
    public Vector3 target;

    private Serial<SetServoMessage> serial;
    private ArticulationBody root;
    private List<float> targets = new List<float>();

    void OnEnable()
    {
        root = GetComponentInParent<ArticulationBody>();
        root.GetDriveTargets(targets);

        serial = Serial<SetServoMessage>.Default();
        serial.MessageReceived += (_, msg) =>
        {
            targets = new List<float>(msg.servo_state.Take(msg.n_servos));
        };

    }

    void OnDisable()
    {
        serial.Dispose();
    }

    public void Sync()
    {
        var command = new PositionCommand(target);
        serial.port.WriteLine(command.Serialize());
    }

    void Update()
    {
        root.SetDriveTargets(targets);
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

