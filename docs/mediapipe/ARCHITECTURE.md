# MediaPipe UDP Bridge - Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      YOUR COMPUTER                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────┐          ┌──────────────────────┐    │
│  │   PYTHON PROCESS     │          │   UNITY PROCESS      │    │
│  │   (Port 5005 TX)     │          │   (Port 5005 RX)     │    │
│  │                      │          │                      │    │
│  │  ┌────────────────┐  │          │  ┌────────────────┐  │    │
│  │  │    WebCam 📷   │  │          │  │   Renderer 🎮  │  │    │
│  │  └────────┬───────┘  │          │  └────────▲───────┘  │    │
│  │           │          │          │           │          │    │
│  │  ┌────────▼───────┐  │          │  ┌────────┴───────┐  │    │
│  │  │  MediaPipe    │  │          │  │  UDP Receiver  │  │    │
│  │  │  Pose Detection│  │          │  │  (UDP Client)  │  │    │
│  │  └────────┬───────┘  │          │  └────────▲───────┘  │    │
│  │           │          │          │           │          │    │
│  │  ┌────────▼───────┐  │  UDP    │  ┌────────┴───────┐  │    │
│  │  │  UDP Streamer  ├──┼─Packets─┼──┤ Joint Transforms│  │    │
│  │  │  (UDP Server)  │  │ (port  │  │ (17 Transforms) │  │    │
│  │  └────────┬───────┘  │  5005)  │  └────────┬───────┘  │    │
│  │           │          │          │           │          │    │
│  │  ┌────────▼───────┐  │          │  ┌────────▼───────┐  │    │
│  │  │   OpenCV       │  │          │  │    Visualizer  │  │    │
│  │  │   Display      │  │          │  │  (Spheres&Line)│  │    │
│  │  └────────────────┘  │          │  └────────────────┘  │    │
│  │                      │          │                      │    │
│  └──────────────────────┘          └──────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │
                    127.0.0.1:5005 (localhost)
                    or network IP:5005
```

## Data Flow

### 1. Capture Phase (Python)
```
WebCam Frame (RGB) → MediaPipe Pose Landmarker → 33 Landmarks
                                                 (17 full body)
                                                 (Upper 6 tracked)
```

### 2. Stream Phase (Python)
```
Upper-body Joints (6 joints)
    ↓ Format: POSE header + joint count + [index + x,y,z]*
    ↓ Serialize to binary
    ↓ UDP packet (size ≈ 50-100 bytes)
    ↓ Send to 127.0.0.1:5005 at ~30 Hz
```

### 3. Receive Phase (Unity)
```
UDP Packet received
    ↓ Parse header "POSE"
    ↓ Read joint count
    ↓ Extract joint index and 3D coordinates
    ↓ Thread-safe queue
    ↓ Main thread processes and updates transforms
```

### 4. Visualization Phase (Unity)
```
Joint Transforms Updated (17 transforms, 6 with live data)
    ↓ JointVisualizer reads transforms
    ↓ Updates sphere positions
    ↓ Updates skeleton line endpoints
    ↓ Renders on screen
```

## Component Architecture

### Python Side: `mediapipe_udp_streamer.py`

**Responsibilities:**
- Initialize MediaPipe PoseLandmarker
- Capture frames from webcam
- Detect 33 pose landmarks
- Extract upper-body joints (indices 11-16)
- Format data as UDP packets
- Send packets at ~30 Hz
- Display local visualization

**Key Variables:**
- `UPPER_BODY_JOINTS` - Tracked joint indices (shoulders, elbows, wrists)
- `UPPER_BODY_CONNECTIONS` - Skeleton topology
- `UDP_IP` / `UDP_PORT` - Network configuration
- `MODEL_PATH` - MediaPipe model file

**Output Format:**
```
[Header] [Count] [Index₁] [X₁] [Y₁] [Z₁] ... [Index₆] [X₆] [Y₆] [Z₆]
  POSE    byte    byte   float float float    byte   float float float
  (4B)    (1B)    (1B)   (4B)  (4B)  (4B)     (1B)   (4B)  (4B)  (4B)
```

### Unity Side Components

#### 1. `MediaPipeUDPReceiver.cs`
**Responsibilities:**
- Listen on UDP port 5005
- Parse incoming packets in background thread
- Queue received joint data
- Update joint transforms on main thread
- Thread-safe data synchronization

**Inspector Settings:**
- `udpPort` (5005) - Port to listen on
- `jointTransforms[17]` - References to joint GameObjects
- `positionScale` (1.0) - Scale factor for positioning
- `debugLogging` - Enable/disable debug output

#### 2. `JointVisualizer.cs`
**Responsibilities:**
- Create spheres for each joint
- Create line renderers for skeleton connections
- Update line positions based on sphere positions
- Configure visual appearance (colors, sizes)

**Inspector Settings:**
- `jointRadius` (0.02) - Size of joint spheres
- `jointMaterial` - Material for spheres
- `lineMaterial` - Material for skeleton lines
- `skeletonConnections` - Bone topology

#### 3. `WebcamFeedRenderer.cs` (Optional)
**Responsibilities:**
- Capture WebCam texture
- Render to UI RawImage
- Provides background for overlay visualization

### Scene Structure: `TestMediaPipeViaUDP.unity`

```
TestMediaPipeViaUDP (Scene)
├── Main Camera
│   └── position: (0, 0.5, -1)
│       rotation: 0, 0, 0
│
├── SkeletonRoot (GameObject)
│   ├── MediaPipeUDPReceiver (Component)
│   ├── JointVisualizer (Component)
│   │
│   ├── Joint_0 (Transform)
│   ├── Joint_1 (Transform)
│   ├── ...
│   └── Joint_16 (Transform)
│
├── Background (Quad)
│   ├── Position: (0, 0, 0)
│   ├── Scale: (3.2, 1.8, 1) [16:9 aspect]
│   └── Optional: WebcamFeedRenderer component
│
└── Canvas (UI)
    └── StatusText (Text)
        └── "Waiting for UDP data..."
```

## Joint Indices (MediaPipe Pose Landmarker)

MediaPipe provides 33 body landmarks:

```
Full Body (0-32):
  0-4:    Face keypoints
  5-6:    Shoulders
  7-8:    Arms
  9-10:   Hands
  11-12:  Shoulders (duplicate)
  13-14:  Elbows
  15-16:  Wrists
  17-22:  Fingers
  23-24:  Hips
  25-26:  Knees
  27-28:  Ankles
  29-32:  Feet/toes

Tracked (Upper-Body Only):
  11:     Left Shoulder
  12:     Right Shoulder
  13:     Left Elbow
  14:     Right Elbow
  15:     Left Wrist
  16:     Right Wrist
```

## Coordinate Systems

### MediaPipe World Coordinates
- Origin: Center of detected person
- Units: Meters
- Range: Typically ±0.3m for upper body
- Axes: Standard 3D (X right, Y up, Z toward camera)

### Unity Local Coordinates
- Origin: SkeletonRoot position
- Units: Same as MediaPipe (meters) by default
- Scale: Adjustable via `positionScale` in receiver

## Performance Characteristics

| Metric | Value |
|--------|-------|
| Network bandwidth | ~50-100 bytes per packet |
| Update frequency | ~30 Hz (30 packets/sec) |
| Latency | <10ms (UDP localhost) |
| UDP overhead | 8 bytes (UDP header) + 28 bytes (IP header) |
| Processing per frame | Python: ~50-100ms, Unity: <1ms |

## Thread Safety

### Python
- Single-threaded; UDP send is non-blocking

### Unity
- **Receive thread:** Background thread receives UDP packets
- **Data queue:** Thread-safe queue holds parsed joints
- **Main thread:** Dequeues and updates transforms

Thread synchronization achieved via `lock(dataQueue)` to prevent race conditions.

## Error Handling

### Python
- Try-catch around UDP send (logs but continues)
- MediaPipe handles invalid frames gracefully

### Unity
- Try-catch around UDP receive (logs warning but continues)
- Graceful degradation if packets arrive out of order or malformed
- OnDestroy cleanup of threads and sockets

## Extensibility Points

1. **Add more joints**
   - Edit `UPPER_BODY_JOINTS` in Python
   - Edit `skeletonConnections` in JointVisualizer
   - Add more Transform references to receiver

2. **Customize visualization**
   - Replace spheres with custom meshes
   - Add animations/shaders
   - Implement LOD system

3. **Network features**
   - Change to TCP for reliability
   - Compress data
   - Add synchronization / time-stamping

4. **Filtering & smoothing**
   - Add Kalman filter
   - Implement exponential smoothing
   - Predict joint positions

5. **Recording & playback**
   - Log UDP packets to file
   - Replay for testing/training
   - Implement recording UI
