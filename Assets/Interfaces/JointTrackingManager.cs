using UnityEngine;
using com.rfilkov.kinect;

/// <summary>
/// Centralized manager for joint tracking.
/// Provides a single point of access to joint tracking data, abstracting away whether
/// the system is using KinectManager, MediaPipeJointManager, or another implementation.
/// 
/// Usage:
///   1. Configure which tracker to use in the Inspector (or at runtime via code)
///   2. Other components call JointTrackingManager.Instance to access joint data
///   3. Can switch trackers at runtime without affecting other components
/// </summary>
public class JointTrackingManager : MonoBehaviour
{
    public enum TrackerType
    {
        KinectManager,
        MediaPipe
    }

    [SerializeField] private TrackerType trackerType = TrackerType.KinectManager;
    [SerializeField] private bool debugLogging = false;

    private IJointTracker activeTracker = null;
    private static JointTrackingManager instance = null;

    public static JointTrackingManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JointTrackingManager>();
                if (instance == null)
                {
                    Debug.LogError("[JointTrackingManager] No instance found in scene!");
                }
            }
            return instance;
        }
    }

    public IJointTracker ActiveTracker
    {
        get { return activeTracker; }
    }

    public TrackerType CurrentTrackerType
    {
        get { return trackerType; }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("[JointTrackingManager] Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeTracker();
    }

    public bool InitializeTracker()
    {
        if (activeTracker != null && activeTracker.IsInitialized())
        {
            if (debugLogging)
                Debug.Log($"[JointTrackingManager] Tracker already initialized: {trackerType}");
            return true;
        }

        switch (trackerType)
        {
            case TrackerType.KinectManager:
                activeTracker = GetOrCreateKinectAdapter();
                break;

            case TrackerType.MediaPipe:
                activeTracker = GetOrCreateMediaPipeManager();
                break;

            default:
                Debug.LogError($"[JointTrackingManager] Unknown tracker type: {trackerType}");
                return false;
        }

        if (activeTracker == null)
        {
            Debug.LogError($"[JointTrackingManager] Failed to get tracker instance");
            return false;
        }

        bool initialized = activeTracker.Initialize();
        if (initialized && debugLogging)
        {
            Debug.Log($"[JointTrackingManager] Initialized tracker: {trackerType}");
        }
        else if (!initialized)
        {
            Debug.LogError($"[JointTrackingManager] Failed to initialize tracker: {trackerType}");
        }

        return initialized;
    }

    /// <summary>
    /// Switch the active tracker at runtime.
    /// </summary>
    public bool SwitchTracker(TrackerType newTrackerType)
    {
        if (newTrackerType == trackerType && activeTracker != null && activeTracker.IsInitialized())
        {
            if (debugLogging)
                Debug.Log($"[JointTrackingManager] Already using {newTrackerType}");
            return true;
        }

        // Shutdown current tracker
        if (activeTracker != null)
        {
            activeTracker.Shutdown();
            if (debugLogging)
                Debug.Log($"[JointTrackingManager] Shut down previous tracker");
        }

        trackerType = newTrackerType;
        return InitializeTracker();
    }

    private KinectManagerAdapter GetOrCreateKinectAdapter()
    {
        KinectManagerAdapter adapter = FindObjectOfType<KinectManagerAdapter>();
        if (adapter == null)
        {
            GameObject go = new GameObject("KinectManagerAdapter");
            adapter = go.AddComponent<KinectManagerAdapter>();
        }
        return adapter;
    }

    private MediaPipeJointManager GetOrCreateMediaPipeManager()
    {
        MediaPipeJointManager manager = FindObjectOfType<MediaPipeJointManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("MediaPipeJointManager");
            manager = go.AddComponent<MediaPipeJointManager>();
        }
        return manager;
    }

    // --- Convenience methods that delegate to active tracker ---

    public bool IsInitialized()
    {
        return activeTracker != null && activeTracker.IsInitialized();
    }

    public bool IsUserDetected(int playerIndex = 0)
    {
        if (!IsInitialized()) return false;
        return activeTracker.IsUserDetected(playerIndex);
    }

    public ulong GetUserIdByIndex(int playerIndex = 0)
    {
        if (!IsInitialized()) return 0;
        return activeTracker.GetUserIdByIndex(playerIndex);
    }

    public bool IsJointTracked(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return false;
        return activeTracker.IsJointTracked(userId, jointType);
    }

    public Vector3 GetJointPosition(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return Vector3.zero;
        return activeTracker.GetJointPosition(userId, jointType);
    }

    public Quaternion GetJointOrientation(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return Quaternion.identity;
        return activeTracker.GetJointOrientation(userId, jointType);
    }

    public float GetJointConfidence(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return 0f;
        return activeTracker.GetJointConfidence(userId, jointType);
    }

    private void OnDestroy()
    {
        if (activeTracker != null)
        {
            activeTracker.Shutdown();
        }
    }

    public void SetDebugLogging(bool enabled)
    {
        debugLogging = enabled;
        if (activeTracker is MediaPipeJointManager mpManager)
        {
            mpManager.SetDebugLogging(enabled);
        }
    }
}
