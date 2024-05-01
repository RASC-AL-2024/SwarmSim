using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

public class Serial<T> : IDisposable where T : struct
{
    const string defaultPortName = "/dev/cu.usbmodem14101";
    const int defaultBaud = 9600;

    public SerialPort port;
    public event EventHandler<T> MessageReceived;

    private CancellationTokenSource source = new CancellationTokenSource();

    public Serial(string portName, int baudRate)
    {
        port = new SerialPort(portName, baudRate);
        port.Open();

        Task.Run(() => MessageReader(source.Token));
    }

    public static Serial<T> Default()
    {
        return new Serial<T>(defaultPortName, defaultBaud);
    }

    public void Dispose()
    {
        source?.Cancel();
        port?.Close();
        port?.Dispose();
        source?.Dispose();
    }

    private async void MessageReader(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                byte[] buffer = new byte[size];
                int offset = 0;
                while (offset < buffer.Length && !token.IsCancellationRequested)
                {
                    int bytesRead = await port.BaseStream.ReadAsync(buffer, offset, buffer.Length - offset, token);
                    offset += bytesRead;
                }

                if (offset == buffer.Length)
                {
                    var message = Serde.FromByteArray<T>(buffer);
                    MessageReceived?.Invoke(this, message);
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }
}
