using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

public class ClientSocket : MonoBehaviour
{
    private bool socketReady = false;
    private TcpClient mySocket;
    private NetworkStream theStream;
    private StreamWriter theWriter;
    public string Host = "192.168.1.103";
    public int Port = 12345;

    void Start()
    {
        SetupSocket();
    }

    public void SetupSocket()
    {
        try
        {
            mySocket = new TcpClient(Host, Port);
            theStream = mySocket.GetStream();
            theWriter = new StreamWriter(theStream);
            socketReady = true;
            Debug.Log("Socket set up");
        }
        catch (Exception e)
        {
            Debug.LogError("Socket error: " + e);
        }
    }

    public void SendCommand(int command)
    {
        //if (!socketReady || (command < 0 || command > 2))
        //{
        //    Debug.LogError("Invalid command or socket not ready.");
        //    return;
        //}

        try
        {
            Byte[] sendBytes = Encoding.UTF8.GetBytes(command.ToString());
            theStream.Write(sendBytes, 0, sendBytes.Length);
            Debug.Log("Sent command: " + command);
        }
        catch (Exception e)
        {
            Debug.LogError("Error sending command: " + e);
        }
    }

    void OnApplicationQuit()
    {
        if (socketReady)
        {
            theWriter.Close();
            mySocket.Close();
        }
    }
}
