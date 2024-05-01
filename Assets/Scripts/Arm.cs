using UnityEngine;
using System.Linq;

public class Arm : MonoBehaviour
{
    private Serial serial = Serial.Default;

    void Start()
    {
        serial.MessageReceived += MessageReceivedHandler;
    }

    void MessageReceivedHandler(object sender, SetServoMessage message)
    {
        Debug.Log($"Got message: [{string.Join(", ", message.state_radians.Select(x => x.ToString()))}]");
    }
}
