using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Serial<T> : IDisposable
{
    const string defaultPortName = "/dev/cu.usbmodem14201";
    const int defaultBaud = 9600;

    public SerialPort port;
    public event EventHandler<T> MessageReceived;

    private CancellationTokenSource source = new CancellationTokenSource();
    private List<Func<String, T>> parsers;

    public Serial(string portName, int baudRate, List<Func<string, T>> parsers)
    {
        port = new SerialPort(portName, baudRate);
        port.StopBits = StopBits.Two;
        port.Parity = Parity.None;
        port.DataBits = 8;
        port.Handshake = Handshake.None;
        port.Open();

        this.parsers = parsers;

        Task.Run(() => MessageReader(source.Token));
    }

    public static Serial<T> Default(List<Func<string, T>> parsers)
    {
        return new Serial<T>(defaultPortName, defaultBaud, parsers);
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
        var reader = new StreamReader(port.BaseStream);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync();
                foreach (var parser in parsers)
                {
                    var result = parser(line);
                    if (result != null)
                    {
                        MessageReceived?.Invoke(this, result);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }
}
