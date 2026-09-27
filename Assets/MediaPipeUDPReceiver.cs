using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Receives MediaPipe 3D joint data via UDP and updates joint positions in real-time.
/// </summary>
public class MediaPipeUDPReceiver : MonoBehaviour
{
    [System.Serializable]
    public class JointData
    {
        public byte index;
        public Vector3 position; // World coordinates in meters
    }

    [SerializeField] private int udpPort = 5005;
    [SerializeField] private Transform[] jointTransforms = new Transform[17]; // 17 body joints
    [SerializeField] private float positionScale = 1f; // Scale factor for positioning
    [SerializeField] private bool debugLogging = true;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;
    private Queue<JointData[]> dataQueue = new Queue<JointData[]>();
    private Vector3 bodyCenter = Vector3.zero;

    private void Start()
    {
        InitializeUDP();
    }

    private void InitializeUDP()
    {
        try
        {
            udpClient = new UdpClient(udpPort);
            udpClient.Client.ReceiveBufferSize = 8192;
            
            isRunning = true;
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            
            if (debugLogging)
                Debug.Log($"[UDP Receiver] Started listening on port {udpPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UDP Receiver] Failed to initialize: {e.Message}");
        }
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);

                // Parse packet
                if (receiveBytes.Length >= 5) // At least header + joint count
                {
                    // Check header
                    if (receiveBytes[0] == 'P' && receiveBytes[1] == 'O' &&
                        receiveBytes[2] == 'S' && receiveBytes[3] == 'E')
                    {
                        byte jointCount = receiveBytes[4];
                        List<JointData> joints = new List<JointData>();

                        // Parse joints
                        int offset = 5;
                        for (int i = 0; i < jointCount; i++)
                        {
                            if (offset + 13 > receiveBytes.Length) break; // Need 1 + 3*4 bytes

                            JointData joint = new JointData();
                            joint.index = receiveBytes[offset];
                            
                            // Read 3 floats (x, y, z)
                            float x = System.BitConverter.ToSingle(receiveBytes, offset + 1);
                            float y = System.BitConverter.ToSingle(receiveBytes, offset + 5);
                            float z = System.BitConverter.ToSingle(receiveBytes, offset + 9);
                            
                            joint.position = new Vector3(x, y, z);
                            joints.Add(joint);
                            
                            offset += 13;
                        }

                        if (joints.Count > 0)
                        {
                            lock (dataQueue)
                            {
                                dataQueue.Enqueue(joints.ToArray());
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogWarning($"[UDP Receiver] Receive error: {e.Message}");
            }
        }
    }

    private void Update()
    {
        // Process queued joint data
        lock (dataQueue)
        {
            while (dataQueue.Count > 0)
            {
                JointData[] joints = dataQueue.Dequeue();
                UpdateJointPositions(joints);
            }
        }
    }

    private void UpdateJointPositions(JointData[] joints)
    {
        foreach (JointData joint in joints)
        {
            if (joint.index < jointTransforms.Length && jointTransforms[joint.index] != null)
            {
                // Position is in meters relative to body center
                // We use it directly (adjust scale if needed)
                jointTransforms[joint.index].localPosition = joint.position * positionScale;
            }
        }
    }

    private void OnDestroy()
    {
        isRunning = false;
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(1000);
        }
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient.Dispose();
        }
    }

    public void SetDebugLogging(bool enabled)
    {
        debugLogging = enabled;
    }

    public void SetPositionScale(float scale)
    {
        positionScale = scale;
    }
}
