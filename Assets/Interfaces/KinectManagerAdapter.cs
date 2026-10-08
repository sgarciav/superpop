using UnityEngine;
using com.rfilkov.kinect;

/// <summary>
/// Adapter that wraps KinectManager to implement the IJointTracker interface.
/// Allows KinectManager to be used interchangeably with other joint tracking implementations.
/// </summary>
public class KinectManagerAdapter : MonoBehaviour, IJointTracker
{
    private KinectManager kinectManager;
    private bool initialized = false;

    // Singleton pattern
    private static KinectManagerAdapter instance = null;
    public static KinectManagerAdapter Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<KinectManagerAdapter>();
            }
            return instance;
        }
    }

    private void Start()
    {
        if (instance == null)
            instance = this;

        Initialize();
    }

    public bool Initialize()
    {
        if (initialized) return true;

        kinectManager = KinectManager.Instance;
        if (kinectManager != null)
        {
            initialized = kinectManager.IsInitialized();
        }

        return initialized;
    }

    public bool IsInitialized()
    {
        if (kinectManager == null)
            kinectManager = KinectManager.Instance;

        return kinectManager != null && kinectManager.IsInitialized();
    }

    public bool IsUserDetected(int playerIndex)
    {
        if (!IsInitialized()) return false;
        return kinectManager.IsUserDetected(playerIndex);
    }

    public ulong GetUserIdByIndex(int playerIndex)
    {
        if (!IsInitialized()) return 0;
        return kinectManager.GetUserIdByIndex(playerIndex);
    }

    public bool IsJointTracked(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return false;
        return kinectManager.IsJointTracked(userId, jointType);
    }

    public Vector3 GetJointPosition(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return Vector3.zero;
        return kinectManager.GetJointKinectPosition(userId, jointType, true);
    }

    public Quaternion GetJointOrientation(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return Quaternion.identity;
        return kinectManager.GetJointOrientation(userId, jointType, true);
    }

    public float GetJointConfidence(ulong userId, KinectInterop.JointType jointType)
    {
        if (!IsInitialized()) return 0f;
        
        // KinectManager uses TrackingState instead of a confidence float
        // Map to 0-1 range: NotTracked=0, Inferred=0.5, Tracked=1.0
        KinectInterop.TrackingState state = kinectManager.GetJointTrackingState(userId, jointType);
        switch (state)
        {
            case KinectInterop.TrackingState.Tracked:
                return 1.0f;
            case KinectInterop.TrackingState.Inferred:
                return 0.5f;
            default:
                return 0.0f;
        }
    }

    public void Shutdown()
    {
        // KinectManager lifecycle is managed by the scene, not by this adapter
        initialized = false;
    }
}
