using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UdpSocket : MonoBehaviour
{
    [SerializeField] string IP = "127.0.0.1";
    [SerializeField] int rxPort = 8000;
    [SerializeField] int txPort = 8001;

    UdpClient client;
    IPEndPoint remoteEndPoint;
    Thread receiveThread;

    public void SendData(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }

    void Awake()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), txPort);
        client = new UdpClient(rxPort);
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                NotifySubs(text);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    public delegate void ActionTriggeredEventHandler(string data);

    public event ActionTriggeredEventHandler OnPlannerInput;

    public void NotifySubs(string data)
    {
        if (OnPlannerInput != null)
        {
            OnPlannerInput(data);
        }
    }

    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();

        client.Close();
    }

}