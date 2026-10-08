# Quick Setup Guide: Switching Between Kinect and MediaPipe

## Prerequisites

### For Kinect Mode
- Azure Kinect hardware connected
- Kinect SDK installed
- Existing scene setup (should work as-is)

### For MediaPipe Mode
- USB camera or webcam
- MediaPipe Python environment (from PoseValidation project)
- Python script sending UDP packets on port 5005

## Setup Steps

### 1. Add JointTrackingManager to Scene

**Option A: Manual Setup**
1. Create a new empty GameObject: `GameObject → Create Empty`
2. Name it: `JointTrackingManager`
3. Add component: `Assets/Interfaces/JointTrackingManager.cs`

**Option B: Automatic (if GameObject already exists)**
- The manager will auto-create itself if not found
- You only need to access `JointTrackingManager.Instance` from any script

### 2. Configure Tracker Type

In the Inspector, set:
- **Tracker Type**: 
  - `KinectManager` for Azure Kinect
  - `MediaPipe` for USB camera
- **Debug Logging**: Enable to see tracking status

### 3. Start the Appropriate Backend

#### For Kinect
Just run the game normally. Make sure:
- Azure Kinect is physically connected
- KinectManager is in the scene and configured
- The game should detect users automatically

#### For MediaPipe
1. **Start the Python pipeline** (from PoseValidation directory):
   ```bash
   cd PoseValidation
   python Scripts/run_mediapipe.py  # Or equivalent script
   ```
   You should see output indicating it's listening for camera input

2. **Verify UDP is sending**:
   - Check that it prints "Sending UDP packets to localhost:5005"
   - If using remote setup, adjust the Python script IP address

3. **Run the Unity game**
   - JointTrackingManager will start listening on port 5005
   - When MediaPipe sends data, you should see debug logs

### 4. Verify It's Working

#### Check Debug Logs
Enable debug logging to see initialization and frame data:

```csharp
// In any script
JointTrackingManager.Instance.SetDebugLogging(true);
```

Watch the Console for messages like:
```
[JointTrackingManager] Initialized tracker: MediaPipe
[MediaPipeJointManager] Started listening on port 5005
[MediaPipeJointManager] Frame 30: 1 users, 17 joints
```

#### Test Joint Tracking
Check that hand positions are being tracked:
```csharp
var tracker = JointTrackingManager.Instance;
if (tracker.IsUserDetected(0))
{
    ulong userId = tracker.GetUserIdByIndex(0);
    var rightWrist = tracker.GetJointPosition(userId, KinectInterop.JointType.WristRight);
    Debug.Log($"Right wrist: {rightWrist}");
}
```

## Switching Between Trackers

### At Runtime (In Code)
```csharp
var manager = JointTrackingManager.Instance;

// Switch to MediaPipe
manager.SwitchTracker(JointTrackingManager.TrackerType.MediaPipe);

// Or switch back to Kinect
manager.SwitchTracker(JointTrackingManager.TrackerType.KinectManager);
```

### In Editor
1. Select the JointTrackingManager GameObject
2. Change the "Tracker Type" dropdown in Inspector
3. Play the game - it will reinitialize with the new tracker

## Troubleshooting

### "No users detected" with MediaPipe

**Check 1: Is Python script running?**
- Look for console output from the MediaPipe Python script
- Verify it says "Sending to localhost:5005"

**Check 2: Is port 5005 open?**
```bash
# On Windows (PowerShell)
Get-NetUDPEndpoint -LocalPort 5005

# On Linux/Mac
lsof -i :5005
```

**Check 3: Camera access**
- MediaPipe script should print camera frames/status
- If no frames, the camera may not be accessible
- Try: `ffmpeg -f dshow -i video="Camera Name" -f null -`

**Check 4: Firewall**
- Ensure Windows Firewall isn't blocking port 5005
- Check any antivirus software

### "Tracker not initialized" error

**Solution:**
```csharp
// Force re-initialization
var manager = JointTrackingManager.Instance;
if (!manager.IsInitialized())
{
    manager.InitializeTracker();
}
```

### Frame drops or latency

**Possible causes:**
1. Python script running too slow - check CPU usage
2. Network congestion - UDP is unreliable over network
3. Camera resolution too high - reduce in MediaPipe settings
4. Confidence threshold too high - lower `confidenceThreshold` in MediaPipeJointManager

**Solution:**
```csharp
var mp = FindObjectOfType<MediaPipeJointManager>();
if (mp != null)
{
    mp.SetConfidenceThreshold(0.2f);  // Lower = more tolerant
}
```

## Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| No UDP packets | Python script not running | Start Python script first |
| Tracker always null | Missing JointTrackingManager | Add it to scene or access via Instance |
| Kinect mode fails | Hardware not connected | Check Azure Kinect connection |
| Hand positions wrong | Coordinate system mismatch | This is Step 3 (calibration) |
| Jerky hand movement | Low frame rate | Increase Python script FPS |

## File Locations

- **Main Manager**: Assets/Interfaces/JointTrackingManager.cs
- **MediaPipe**: Assets/MediaPipe/MediaPipeJointManager.cs
- **Kinect Adapter**: Assets/Interfaces/KinectManagerAdapter.cs
- **Documentation**: Assets/Interfaces/README.md

## Next Steps

1. **Step 3 - Coordinate Calibration**: Map MediaPipe coordinates to play area
2. **Step 4 - Depth Estimation**: Fix Z-coordinate scaling for MediaPipe
3. **Test with actual game**: Run bubble popper game with MediaPipe tracking
4. **Fine-tune thresholds**: Adjust confidence levels for your camera setup

## Support

See **IMPLEMENTATION_SUMMARY.md** for detailed architecture.
See **Assets/Interfaces/README.md** for API reference.
