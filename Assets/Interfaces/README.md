# Joint Tracking Abstraction Layer

This directory contains an abstraction layer for joint tracking systems, allowing the game to use either Azure Kinect or MediaPipe interchangeably without changing game logic.

## Architecture Overview

### Components

1. **IJointTracker** - Core interface that all tracking implementations must follow
   - Defines methods for joint position, orientation, and confidence queries
   - Abstracts away the underlying tracking system

2. **KinectManagerAdapter** - Adapter that wraps `KinectManager` to implement `IJointTracker`
   - Maintains compatibility with existing Kinect-based code
   - Maps Kinect's `TrackingState` to confidence values (0-1 range)

3. **MediaPipeJointManager** - Native implementation of `IJointTracker` for MediaPipe
   - Receives UDP packets from MediaPipe Python pipeline
   - Converts COCO 17-keypoint format to KinectInterop joint types
   - Provides confidence scores for each joint

4. **JointTrackingManager** - Singleton factory and manager
   - Single point of access for all joint tracking data
   - Handles switching between trackers at runtime
   - Provides convenience methods that delegate to active tracker

## Usage

### Setup

1. Add a **JointTrackingManager** to your scene (as a MonoBehaviour)
2. Configure which tracker to use in the Inspector:
   - **KinectManager** - For Azure Kinect sensors
   - **MediaPipe** - For USB camera + MediaPipe Python pipeline
3. The appropriate adapter/manager will be created automatically

### In Code

```csharp
// Get the active tracker
var tracker = JointTrackingManager.Instance;

// Check if tracking is active
if (tracker.IsInitialized() && tracker.IsUserDetected(playerIndex: 0))
{
    ulong userId = tracker.GetUserIdByIndex(0);
    
    // Check if a specific joint is tracked
    if (tracker.IsJointTracked(userId, KinectInterop.JointType.WristRight))
    {
        Vector3 wristPos = tracker.GetJointPosition(userId, KinectInterop.JointType.WristRight);
        float confidence = tracker.GetJointConfidence(userId, KinectInterop.JointType.WristRight);
        
        // Use the joint position...
    }
}
```

### Switching Trackers at Runtime

```csharp
var trackingManager = JointTrackingManager.Instance;

// Switch from Kinect to MediaPipe
trackingManager.SwitchTracker(JointTrackingManager.TrackerType.MediaPipe);

// Or back to Kinect
trackingManager.SwitchTracker(JointTrackingManager.TrackerType.KinectManager);
```

## COCO to Kinect Joint Mapping

MediaPipe outputs 17 COCO keypoints which are mapped to KinectInterop.JointType:

| COCO Index | COCO Name       | Kinect Joint Type |
|------------|-----------------|-------------------|
| 0          | Nose            | Nose              |
| 1          | Left Eye        | EyeLeft           |
| 2          | Right Eye       | EyeRight          |
| 3          | Left Ear        | EarLeft           |
| 4          | Right Ear       | EarRight          |
| 5          | Left Shoulder   | ShoulderLeft      |
| 6          | Right Shoulder  | ShoulderRight     |
| 7          | Left Elbow      | ElbowLeft         |
| 8          | Right Elbow     | ElbowRight        |
| 9          | **Left Wrist**  | **WristLeft**     |
| 10         | **Right Wrist** | **WristRight**    |
| 11         | Left Hip        | HipLeft           |
| 12         | Right Hip       | HipRight          |
| 13         | Left Knee       | KneeLeft          |
| 14         | Right Knee      | KneeRight         |
| 15         | Left Ankle      | AnkleLeft         |
| 16         | Right Ankle     | AnkleRight        |

*Wrist joints (bold) are most relevant for the bubble popper game*

## Coordinate Systems

### MediaPipe Coordinates
- X, Y: Normalized image coordinates (0-1 range for image width/height)
- Z: Depth estimation (if available from Python pipeline)
- Y-axis can be flipped depending on camera orientation

### Kinect Coordinates
- 3D world coordinates in meters
- Z-axis points away from sensor
- Y-axis points up

### Conversion
The `MediaPipeJointManager` handles conversion via:
- `positionScale` - Applied to normalize coordinates
- `flipYAxis` - Inverts Y to match Unity convention
- `cameraPoseDistance` - Used for depth calibration if needed

## Extending the System

To add a new tracker (e.g., OpenPose, another depth sensor):

1. **Implement IJointTracker**
   ```csharp
   public class MyCustomTracker : MonoBehaviour, IJointTracker
   {
       public bool Initialize() { /* ... */ }
       public bool IsInitialized() { /* ... */ }
       public bool IsUserDetected(int playerIndex) { /* ... */ }
       // ... implement other methods
   }
   ```

2. **Register in JointTrackingManager**
   - Add a new TrackerType enum value
   - Add a new case in `InitializeTracker()` method
   - Create `GetOrCreateMyTracker()` method

3. **Update configuration** if needed

## Testing

### With Kinect
- Ensure KinectManager is in your scene and properly configured
- Set `JointTrackingManager.trackerType = TrackerType.KinectManager`
- Game should work as before

### With MediaPipe
1. Start the MediaPipe Python pipeline (e.g., from `PoseValidation` project)
2. Ensure it's sending UDP packets on port 5005
3. Set `JointTrackingManager.trackerType = TrackerType.MediaPipe`
4. Game should receive joint data and hand positions should track camera input

## Debugging

Enable debug logging in `JointTrackingManager`:
```csharp
JointTrackingManager.Instance.SetDebugLogging(true);
```

This will log:
- Tracker initialization status
- Frame counts
- User and joint counts
- Confidence levels

For MediaPipe-specific debugging, enable logging in `MediaPipeJointManager`:
```csharp
var mediaPipe = FindObjectOfType<MediaPipeJointManager>();
if (mediaPipe != null)
    mediaPipe.SetDebugLogging(true);
```

## Next Steps

- **Step 3**: Coordinate system mapping and calibration
- **Step 4**: Depth estimation for MediaPipe (currently uses fixed z-distance)
- **Step 5**: Replace Kinect Manager startup logic in game
- **Step 6**: Test with actual game scenarios
