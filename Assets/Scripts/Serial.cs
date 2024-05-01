using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
// using System.Diagnostics;
using System;
using UnityEngine;

public class Serde
{
    public static T FromByteArray<T>(byte[] bytes) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            System.IntPtr ptr = handle.AddrOfPinnedObject();
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SetServoMessage
{
    public byte n_servos;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public float[] state_radians;
}

public class Serial
{
    const string defaultPortName = "/dev/cu.usbmodem14101";
    const int defaultBaud = 9600;
    public static Serial Default = new Serial(new SerialPort(defaultPortName, defaultBaud));

    public SerialPort port;
    public event EventHandler<SetServoMessage> MessageReceived;

    private CancellationTokenSource source = new CancellationTokenSource();

    public Serial(SerialPort port)
    {
        this.port = port;
        port.Open();

        Task.Run(() => MessageReader(source.Token));
    }

    private async void MessageReader(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var size = Marshal.SizeOf(typeof(SetServoMessage));
                byte[] buffer = new byte[size];
                int offset = 0;
                while (offset < buffer.Length && !token.IsCancellationRequested)
                {
                    int bytesRead = await port.BaseStream.ReadAsync(buffer, offset, buffer.Length - offset, token);
                    offset += bytesRead;
                }

                if (offset == buffer.Length)
                {
                    SetServoMessage message = Serde.FromByteArray<SetServoMessage>(buffer);
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    void OnDisable()
    {
        source?.Cancel();
    }
}
