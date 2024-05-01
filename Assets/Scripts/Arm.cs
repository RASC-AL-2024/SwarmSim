using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetServoMessage
{
    public byte n_servos;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public float[] state_radians;
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

    void OnEnable()
    {
        serial = Serial<SetServoMessage>.Default();
        serial.MessageReceived += MessageReceivedHandler;
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

    void MessageReceivedHandler(object sender, SetServoMessage message)
    {
        Debug.Log($"Got message: [{string.Join(", ", message.state_radians.Select(x => x.ToString()))}]");
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

