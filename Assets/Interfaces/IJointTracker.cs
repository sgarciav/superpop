using UnityEngine;
using com.rfilkov.kinect;

/// <summary>
/// Interface for joint tracking systems (Kinect, MediaPipe, etc.)
/// Abstracts the source of joint position data to allow switching between different tracking backends.
/// </summary>
public interface IJointTracker
{
    /// <summary>
    /// Initializes the joint tracker. Returns true if initialization was successful.
    /// </summary>
    bool Initialize();

    /// <summary>
    /// Checks if the tracker is initialized and ready to provide data.
    /// </summary>
    bool IsInitialized();

    /// <summary>
    /// Checks if a user is currently detected at the given player index.
    /// </summary>
    bool IsUserDetected(int playerIndex);

    /// <summary>
    /// Gets the user ID for the given player index. Returns 0 if no user found.
    /// </summary>
    ulong GetUserIdByIndex(int playerIndex);

    /// <summary>
    /// Checks if a joint is tracked for the given user and joint type.
    /// </summary>
    bool IsJointTracked(ulong userId, KinectInterop.JointType jointType);

    /// <summary>
    /// Gets the position of a joint in Kinect/world coordinates.
    /// </summary>
    Vector3 GetJointPosition(ulong userId, KinectInterop.JointType jointType);

    /// <summary>
    /// Gets the rotation/orientation of a joint.
    /// </summary>
    Quaternion GetJointOrientation(ulong userId, KinectInterop.JointType jointType);

    /// <summary>
    /// Gets the tracking confidence (0-1) for a joint. 1.0 = fully tracked, 0.0 = not tracked.
    /// </summary>
    float GetJointConfidence(ulong userId, KinectInterop.JointType jointType);

    /// <summary>
    /// Cleanup and shutdown the tracker.
    /// </summary>
    void Shutdown();
}
