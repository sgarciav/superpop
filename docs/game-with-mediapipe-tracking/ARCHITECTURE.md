# Architecture Diagram: Joint Tracking Abstraction Layer

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         GAME CODE                               │
│  (FollowUserJointPose, Hand Tracking, etc.)                     │
│                                                                  │
│  Minimal changes needed - uses unified interface               │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────────┐
        │  JointTrackingManager (Singleton)   │
        │  ┌──────────────────────────────┐  │
        │  │ trackerType: KinectManager   │  │
        │  │  or MediaPipe              │  │
        │  │ activeTracker: IJointTracker │  │
        │  └──────────────────────────────┘  │
        │  • Initialize()                     │
        │  • SwitchTracker()                  │
        │  • GetJointPosition()               │
        │  • IsJointTracked()                 │
        └────────────┬───────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
         ▼                       ▼
    ┌─────────────┐       ┌──────────────────┐
    │ IJointTracker│       │ IJointTracker    │
    │  Interface   │       │  Interface       │
    └─────────────┘       └──────────────────┘
         △                       △
         │                       │
         │ implements            │ implements
         │                       │
    ┌────┴──────────┐       ┌────┴──────────────────┐
    │                       │                       │
    ▼                       ▼                       ▼
┌──────────────┐    ┌───────────────────┐    ┌─────────────────┐
│ KinectManager│    │KinectManagerAdapter│    │MediaPipeJoint   │
│ (Existing)   │    │(Wrapper)          │    │Manager          │
├──────────────┤    ├───────────────────┤    ├─────────────────┤
│ GetJointPos()│    │ Wraps existing    │    │ Receives UDP    │
│ GetJointOr() │    │ KinectManager     │    │ data            │
│ IsJointTr()  │    │                   │    │                 │
│ ...          │    │ Maps TrackingState│    │ Parses COCO     │
└──────────────┘    │ to confidence     │    │ 17 keypoints    │
                    │                   │    │                 │
                    │ Single-user only  │    │ Converts to     │
                    │ when wrapped      │    │ Kinect JointType│
                    └───────────────────┘    │                 │
                                            │ Port: 5005      │
                                            │                 │
                                            └─────────────────┘
```

## Data Flow with MediaPipe

```
┌──────────────────┐
│ USB Camera       │
│ Feed             │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────┐
│ MediaPipe Python Pipeline        │
│ • Detect 17 COCO keypoints       │
│ • Calculate positions (x,y,z)    │
│ • Estimate confidence            │
└────────┬─────────────────────────┘
         │
         │ UDP Packet (Port 5005)
         │ Format: "POSE" + keypoints
         │
         ▼
┌──────────────────────────────────┐
│ MediaPipeJointManager            │
│ • UDP Receiver Thread            │
│ • Parse COCO format              │
│ • Map to KinectInterop.JointType │
│ • Store in dictionary            │
└────────┬─────────────────────────┘
         │
         ▼ (lock protected)
┌──────────────────────────────────┐
│ Joint Data Storage               │
│ Dictionary<userId, Dictionary   │
│  <JointType, JointSnapshot>>     │
│ • position: Vector3              │
│ • confidence: float              │
│ • timestamp: long                │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ Game Components                  │
│ (FollowUserJointPose, etc)       │
│                                  │
│ GetJointPosition(userId, joint)  │
└──────────────────────────────────┘
```

## Sequence Diagram: Switching Trackers

```
Player Code          JointTrackingManager    Old Tracker    New Tracker
    │                      │                     │               │
    ├──SwitchTracker()─────>│                     │               │
    │                      │                     │               │
    │                      ├─Shutdown()────────>│               │
    │                      │                     │               │
    │                      │<──Shutdown OK───────┤               │
    │                      │                     │               │
    │                      ├───────────────────────────Initialize()────>│
    │                      │                     │               │
    │                      │<──────────────────────────OK (true)──┤
    │                      │                     │               │
    │<─ bool (success)──────                     │               │
    │                      │                     │               │
    └──GetJointPosition()──>│                     │               │
                            ├──GetJointPosition()───────────────>│
                            │                     │               │
                            │<────────────────Vector3────────────┤
                            │                     │               │
                            └─────────Vector3────────>            │
```

## Component Interaction: Game Frame Update

```
Frame N:
  │
  ├─> FollowUserJointPose.Update()
  │   ├─ JointTrackingManager.Instance
  │   ├─ IsInitialized() ────> check activeTracker
  │   ├─ IsUserDetected(0) ──> delegate to IJointTracker
  │   ├─ GetUserIdByIndex(0) ─> either:
  │   │  • KinectManager.GetUserIdByIndex()
  │   │  • or MediaPipeJointManager returns stored userId
  │   │
  │   ├─ IsJointTracked(userId, WristRight)
  │   │  • Check confidence >= threshold
  │   │
  │   └─ GetJointPosition(userId, WristRight)
  │      • Returns Vector3 from active tracker
  │
  ├─> Hand GameObject.transform.position = wristPos
  │
  └─> Game logic uses hand position

Frame N+1:
  (repeat)
```

## Class Dependencies

```
FollowUserJointPose
    └─> uses
        └─> JointTrackingManager (singleton)
            ├─> has activeTracker: IJointTracker
            └─> can switch between:
                ├─> KinectManagerAdapter
                │   └─> wraps KinectManager
                └─> MediaPipeJointManager
                    └─> owns UDP receiver thread

IJointTracker
    ├─ implemented by
    │  ├─ KinectManagerAdapter
    │  └─ MediaPipeJointManager
    └─ used by
       └─ JointTrackingManager
```

## State Machine: Tracker Initialization

```
                   ┌─────────────┐
                   │   Created   │
                   └──────┬──────┘
                          │
                   Start() called
                          │
                          ▼
          ┌───────────────────────────────┐
          │  InitializeTracker()          │
          │  • Find or create adapter     │
          │  • Call adapter.Initialize()  │
          └───────────────────────────────┘
                    │           │
        Initialize  │           │  Initialize
        Succeeds    │           │  Fails
                    ▼           ▼
              ┌──────────┐   ┌──────────┐
              │Initialized│   │ Failed   │
              │ Ready     │   │ Waiting  │
              └────┬──────┘   └──────────┘
                   │               │
            GetJointPosition()      │ Retry
                   │               │
                   ▼               ▼
            ┌──────────────┐    ┌──────────┐
            │ Data Ready   │    │ Waiting  │
            │ to Game      │    │ for Init │
            └──────────────┘    └──────────┘
```

## Thread Model

```
Main Thread                         Background Thread
    │                                      │
    ├─ Update()                            │
    │  ├─ Check activeTracker             │
    │  ├─ Call GetJointPosition()         │
    │  └─ Update game objects             │
    │                                     │
    │  (lock) Access trackedUsers ◄──────┼─ Receive UDP packet
    │         queue ────────────────────>┤ (non-blocking)
    │  (unlock)                          │
    │                                     │
    │  Process queue (dequeue)            │
    │  Update trackedUsers                │
    │  (unlocked, main thread)            │
    │                                      │
    └──────────────────────────────────────┘
```

## Error Handling Flow

```
Initialize()
    │
    ├─ Try create UDP socket
    │  ├─ Success ──> start receive thread
    │  └─ Failure ──> catch exception
    │               └─ Log error
    │               └─ return false
    │
    ├─ Try start receive thread
    │  ├─ Success ──> set initialized=true
    │  └─ Failure ──> catch exception
    │               └─ Log error
    │               └─ return false
    │
    └─ IsInitialized() checks both:
       └─ initialized flag AND
       └─ udpClient != null
```

## Memory Model

```
Per-Frame Memory:
├─ Queue<DataUpdate> (queued UDP packets)
│  └─ each has Dictionary<JointType, JointSnapshot>
│
├─ Dictionary<ulong, Dictionary<JointType, JointSnapshot>>
│  └─ trackedUsers (current frame data)
│
└─ JointSnapshot (per joint)
   ├─ Vector3 position (12 bytes)
   ├─ float confidence (4 bytes)
   └─ long timestamp (8 bytes)
   = 24 bytes per joint
   
17 joints × 24 bytes = 408 bytes per user
~1KB per user (with overhead)
```

## Extensibility Points

```
To add a new tracker (e.g., OpenPose):

1. Create new class implementing IJointTracker
   class OpenPoseTracker : MonoBehaviour, IJointTracker { }

2. Add to JointTrackingManager.TrackerType enum
   OpenPose = 2

3. Add case in InitializeTracker()
   case TrackerType.OpenPose:
       activeTracker = GetOrCreateOpenPoseManager();
       break;

4. Add helper method
   private OpenPoseTracker GetOrCreateOpenPoseManager() { }

5. Everything else works without changes!
```
