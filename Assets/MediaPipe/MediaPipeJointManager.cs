using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using com.rfilkov.kinect;

/// <summary>
/// MediaPipe-based joint tracking implementation.
/// Receives 3D joint data via UDP from the MediaPipe Python pipeline and provides
/// joint positions through the IJointTracker interface.
/// 
/// COCO 17-keypoint format is converted to KinectInterop.JointType mappings.
/// </summary>
public class MediaPipeJointManager : MonoBehaviour, IJointTracker
{
    [Header("UDP Configuration")]
    [SerializeField] private int udpPort = 5005;
    [SerializeField] private bool debugLogging = false;

    [Header("Coordinate Mapping")]
    [SerializeField] private Vector3 positionScale = Vector3.one;
    [SerializeField] private bool flipYAxis = true;  // MediaPipe Y often needs flipping for Unity
    [SerializeField] private float confidenceThreshold = 0.3f;

    [Header("Calibration")]
    [Tooltip("Distance from camera to play area (used for depth estimation if needed)")]
    [SerializeField] private float cameraPoseDistance = 1.5f;

    // Singleton pattern
    private static MediaPipeJointManager instance = null;
    public static MediaPipeJointManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MediaPipeJointManager>();
            }
            return instance;
        }
    }

    /// <summary>
    /// Maps COCO 17-keypoint indices to KinectInterop.JointType.
    /// Reference COCO keypoints:
    /// 0:Nose, 1:LeftEye, 2:RightEye, 3:LeftEar, 4:RightEar,
    /// 5:LeftShoulder, 6:RightShoulder, 7:LeftElbow, 8:RightElbow,
    /// 9:LeftWrist, 10:RightWrist, 11:LeftHip, 12:RightHip,
    /// 13:LeftKnee, 14:RightKnee, 15:LeftAnkle, 16:RightAnkle
    /// </summary>
    private static readonly Dictionary<int, KinectInterop.JointType> CocoToKinectMapping = new Dictionary<int, KinectInterop.JointType>
    {
        { 0, KinectInterop.JointType.Nose },
        { 1, KinectInterop.JointType.EyeLeft },
        { 2, KinectInterop.JointType.EyeRight },
        { 3, KinectInterop.JointType.EarLeft },
        { 4, KinectInterop.JointType.EarRight },
        { 5, KinectInterop.JointType.ShoulderLeft },
        { 6, KinectInterop.JointType.ShoulderRight },
        { 7, KinectInterop.JointType.ElbowLeft },
        { 8, KinectInterop.JointType.ElbowRight },
        { 9, KinectInterop.JointType.WristLeft },
        { 10, KinectInterop.JointType.WristRight },
        { 11, KinectInterop.JointType.HipLeft },
        { 12, KinectInterop.JointType.HipRight },
        { 13, KinectInterop.JointType.KneeLeft },
        { 14, KinectInterop.JointType.KneeRight },
        { 15, KinectInterop.JointType.AnkleLeft },
        { 16, KinectInterop.JointType.AnkleRight },
    };

    // UDP networking
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    // Joint data storage (per user/player)
    private struct JointSnapshot
    {
        public Vector3 position;
        public float confidence;
        public long timestamp;
    }

    private Dictionary<ulong, Dictionary<KinectInterop.JointType, JointSnapshot>> trackedUsers =
        new Dictionary<ulong, Dictionary<KinectInterop.JointType, JointSnapshot>>();

    private Queue<Dictionary<ulong, Dictionary<KinectInterop.JointType, JointSnapshot>>> dataQueue =
        new Queue<Dictionary<ulong, Dictionary<KinectInterop.JointType, JointSnapshot>>>();

    private bool initialized = false;
    private object dataLock = new object();
    private long frameCount = 0;

    // --- IJointTracker Implementation ---

    public bool Initialize()
    {
        if (initialized) return true;

        try
        {
            udpClient = new UdpClient(udpPort);
            udpClient.Client.ReceiveBufferSize = 65536;

            isRunning = true;
            receiveThread = new Thread(ReceiveDataLoop);
            receiveThread.IsBackground = true;
            receiveThread.Name = "MediaPipe-UDP-Receiver";
            receiveThread.Start();

            initialized = true;
            if (debugLogging)
                Debug.Log($"[MediaPipeJointManager] Initialized on port {udpPort}");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MediaPipeJointManager] Failed to initialize: {e.Message}");
            return false;
        }
    }

    public bool IsInitialized()
    {
        return initialized && udpClient != null;
    }

    public bool IsUserDetected(int playerIndex)
    {
        if (playerIndex != 0) return false;  // MediaPipe currently tracks single user
        lock (dataLock)
        {
            return trackedUsers.Count > 0;
        }
    }

    public ulong GetUserIdByIndex(int playerIndex)
    {
        if (playerIndex != 0) return 0;
        lock (dataLock)
        {
            if (trackedUsers.Count == 0) return 0;
            // Return the first (only) user ID
            foreach (ulong userId in trackedUsers.Keys)
            {
                return userId;
            }
        }
        return 0;
    }

    public bool IsJointTracked(ulong userId, KinectInterop.JointType jointType)
    {
        lock (dataLock)
        {
            if (!trackedUsers.ContainsKey(userId)) return false;
            var userJoints = trackedUsers[userId];
            if (!userJoints.ContainsKey(jointType)) return false;

            var joint = userJoints[jointType];
            return joint.confidence >= confidenceThreshold;
        }
    }

    public Vector3 GetJointPosition(ulong userId, KinectInterop.JointType jointType)
    {
        lock (dataLock)
        {
            if (!trackedUsers.ContainsKey(userId))
                return Vector3.zero;

            var userJoints = trackedUsers[userId];
            if (!userJoints.ContainsKey(jointType))
                return Vector3.zero;

            return userJoints[jointType].position;
        }
    }

    public Quaternion GetJointOrientation(ulong userId, KinectInterop.JointType jointType)
    {
        // MediaPipe doesn't provide joint rotations, only positions
        return Quaternion.identity;
    }

    public float GetJointConfidence(ulong userId, KinectInterop.JointType jointType)
    {
        lock (dataLock)
        {
            if (!trackedUsers.ContainsKey(userId))
                return 0f;

            var userJoints = trackedUsers[userId];
            if (!userJoints.ContainsKey(jointType))
                return 0f;

            return userJoints[jointType].confidence;
        }
    }

    public void Shutdown()
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
        initialized = false;
    }

    // --- UDP Receive Loop ---

    private void ReceiveDataLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);

                if (receiveBytes.Length >= 5)
                {
                    // Check header: "POSE"
                    if (receiveBytes[0] == 'P' && receiveBytes[1] == 'O' &&
                        receiveBytes[2] == 'S' && receiveBytes[3] == 'E')
                    {
                        byte jointCount = receiveBytes[4];
                        var userJoints = new Dictionary<KinectInterop.JointType, JointSnapshot>();

                        // Parse COCO keypoints
                        // Packet format: index(1) + x(4) + y(4) + z(4) = 13 bytes per joint
                        int offset = 5;
                        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();

                        for (int cocoIndex = 0; cocoIndex < jointCount && cocoIndex < 17; cocoIndex++)
                        {
                            if (offset + 13 > receiveBytes.Length) break;  // Need 1 + 3*4 bytes

                            byte index = receiveBytes[offset];
                            float x = System.BitConverter.ToSingle(receiveBytes, offset + 1);
                            float y = System.BitConverter.ToSingle(receiveBytes, offset + 5);
                            float z = System.BitConverter.ToSingle(receiveBytes, offset + 9);
                            
                            // All sent joints are considered tracked with high confidence
                            float confidence = 1.0f;

                            offset += 13;

                            // Map COCO index to Kinect joint type
                            if (CocoToKinectMapping.TryGetValue(cocoIndex, out KinectInterop.JointType kinectJoint))
                            {
                                // Apply transformations
                                Vector3 position = new Vector3(x, flipYAxis ? -y : y, z) * positionScale;

                                userJoints[kinectJoint] = new JointSnapshot
                                {
                                    position = position,
                                    confidence = confidence,
                                    timestamp = timestamp
                                };
                            }
                        }

                        if (userJoints.Count > 0)
                        {
                            lock (dataLock)
                            {
                                var update = new Dictionary<ulong, Dictionary<KinectInterop.JointType, JointSnapshot>>
                                {
                                    { 1, userJoints }  // User ID 1 for single user tracking
                                };
                                dataQueue.Enqueue(update);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (isRunning)
                    Debug.LogWarning($"[MediaPipeJointManager] UDP receive error: {e.Message}");
            }
        }
    }

    // --- Update Loop (process queued data) ---

    private void Update()
    {
        if (!initialized) return;

        lock (dataLock)
        {
            while (dataQueue.Count > 0)
            {
                trackedUsers = dataQueue.Dequeue();
                frameCount++;
            }
        }

        // Optional: periodic debug logging
        if (debugLogging && frameCount % 30 == 0)
        {
            int userCount = 0;
            int jointCount = 0;
            lock (dataLock)
            {
                userCount = trackedUsers.Count;
                if (userCount > 0)
                {
                    foreach (var joints in trackedUsers.Values)
                    {
                        jointCount = joints.Count;
                        break;
                    }
                }
            }
            Debug.Log($"[MediaPipeJointManager] Frame {frameCount}: {userCount} users, {jointCount} joints");
        }
    }

    private void Start()
    {
        if (instance == null)
            instance = this;

        Initialize();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    public void SetDebugLogging(bool enabled)
    {
        debugLogging = enabled;
    }

    public void SetPositionScale(Vector3 scale)
    {
        positionScale = scale;
    }

    public void SetConfidenceThreshold(float threshold)
    {
        confidenceThreshold = Mathf.Clamp01(threshold);
    }
}
