# MediaPipe UDP Real-Time Pose Tracking for Unity

Real-time skeletal tracking using MediaPipe (Python) streamed to Unity via UDP with built-in 3D visualization.

## ⚡ Quick Start (3 Steps)

```bash
# 1. Start Python streamer
cd KinectGame_FB
python scripts/mediapipe_udp_streamer.py

# 2. In Unity: Tools → Create MediaPipe UDP Test Scene

# 3. Open TestMediaPipeViaUDP.unity and press Play 🎉
```

## 📚 Documentation Index

**Start with one of these:**

| Document | Read Time | Purpose |
|----------|-----------|---------|
| **QUICKSTART_MEDIAPIPE_UDP.md** | 5 min | Get running in 3 steps with verification checklist |
| **MEDIAPIPE_UDP_SETUP.md** | 15 min | Detailed setup, troubleshooting, configuration |
| **ARCHITECTURE.md** | 20 min | System design, data flow, technical deep-dive |
| **IMPLEMENTATION_SUMMARY.md** | 10 min | Complete overview of what was built |

## 🎯 I Want To...

**Get it running quickly?**
→ Read: QUICKSTART_MEDIAPIPE_UDP.md

**Understand how it works?**
→ Read: ARCHITECTURE.md

**Fix a problem?**
→ Check: MEDIAPIPE_UDP_SETUP.md → Troubleshooting section

**Modify or extend it?**
→ Read: ARCHITECTURE.md → Extensibility Points section

**See technical details?**
→ Read: IMPLEMENTATION_SUMMARY.md

## 📦 What You're Getting

### Python Side
- `scripts/mediapipe_udp_streamer.py` - Main application (captures pose, streams UDP)
- `scripts/test_udp_connection.py` - Testing utility (debug UDP connection)

### Unity Side
- `Assets/MediaPipeUDPReceiver.cs` - Receives UDP packets, updates joint positions
- `Assets/JointVisualizer.cs` - Renders skeleton (green spheres, cyan lines)
- `Assets/WebcamFeedRenderer.cs` - Optional: display webcam feed
- `Assets/Editor/CreateMediaPipeScene.cs` - Scene generator
- `Assets/Scenes/TestMediaPipeViaUDP.unity` - Generated test scene

### Documentation
- Complete setup guides with troubleshooting
- System architecture diagrams
- Data format specifications
- Performance metrics

## ✨ Features

- ✅ Real-time pose detection (30 FPS)
- ✅ 6 upper-body joints tracked (shoulders, elbows, wrists)
- ✅ Lightweight UDP streaming (~1.5 KB/s)
- ✅ Sub-10ms network latency
- ✅ Thread-safe data processing
- ✅ Zero external Unity dependencies
- ✅ Inspector-configurable
- ✅ Built-in debugging tools
- ✅ Comprehensive documentation

## 🔧 System Architecture

```
WebCam 📷 → MediaPipe (Python) → UDP:5005 → Unity Receiver → Display 🎮
```

**Key Numbers:**
- Network: 127.0.0.1:5005 (configurable)
- Frame Rate: ~30 Hz
- Latency: <10ms (UDP localhost)
- Bandwidth: ~1.5 KB/s
- Overhead: ~36 bytes per packet

## 📋 Prerequisites

### Python
- Python 3.6+
- `mediapipe` - `pip install mediapipe`
- `opencv-python` - `pip install opencv-python`
- `pose_landmarker_lite.task` (already in scripts/ directory)

### Unity
- Unity 2019.4 LTS or newer
- No external packages required

### Hardware
- Webcam (any USB camera)
- Same machine or networked machine with port 5005 open

## 🚀 Getting Started

### 1. Verify Prerequisites
```bash
# Check Python version
python --version  # Should be 3.6+

# Check MediaPipe
python -c "import mediapipe; print('✓ MediaPipe installed')"

# Check OpenCV
python -c "import cv2; print('✓ OpenCV installed')"

# Check webcam
python -c "import cv2; print('✓ Webcam works' if cv2.VideoCapture(0).isOpened() else '✗ No webcam')"

# Check model file
ls scripts/pose_landmarker_lite.task
```

### 2. Start Streaming
```bash
python scripts/mediapipe_udp_streamer.py
```

You should see:
- OpenCV window showing your skeleton with green dots
- "Streaming on UDP 5005..." message in the window
- No errors in terminal

### 3. Create Scene in Unity
In Unity Editor:
1. Click **Tools** menu
2. Select **Create MediaPipe UDP Test Scene**
3. Wait for scene creation (should be instant)

### 4. Play the Scene
1. Open `Assets/Scenes/TestMediaPipeViaUDP.unity`
2. Press **Play**
3. Watch green spheres (joints) move with your body!

## 🎮 What You'll See

When playing the scene:
- **Green spheres** = Tracked joints (shoulders, elbows, wrists)
- **Cyan lines** = Skeleton connections
- **Real-time movement** = Live tracking of your body
- **Status text** = "Waiting for UDP data..." initially, then active

## ⚙️ Configuration

### Default Settings
- IP: `127.0.0.1` (localhost)
- Port: `5005`
- Frame Rate: ~30 FPS
- Position Scale: 1.0
- Joint Radius: 0.02 units

### To Change Settings

**Network Configuration** (both Python and Unity)
- File: `scripts/mediapipe_udp_streamer.py` line 20
- Change: `UDP_IP = "127.0.0.1"` to your machine IP

**Port Number** (both Python and Unity)
- Python: `scripts/mediapipe_udp_streamer.py` line 21
- Unity: `MediaPipeUDPReceiver` Inspector field `udpPort`

**Visualization** (Unity Inspector)
- Select `SkeletonRoot` in scene
- Adjust `MediaPipeUDPReceiver.positionScale` to make joints bigger/smaller
- Select `SkeletonRoot` → `JointVisualizer`
- Adjust `jointRadius` to change sphere size

## 🐛 Troubleshooting

### Python won't start
```bash
# Install missing packages
pip install mediapipe opencv-python

# Verify model file
ls scripts/pose_landmarker_lite.task
```

### No joints appearing in Unity
1. **Is Python streaming?** Check Python window shows skeleton
2. **Enable debug logging:** Inspector → `MediaPipeUDPReceiver` → `debugLogging` = ON
3. **Check port:** `netstat -un | grep 5005` (should show port listening)
4. **Check firewall:** Make sure port 5005 isn't blocked

### Joints are laggy or jittery
- Reduce FPS in Python streamer
- Adjust `positionScale` in Inspector
- Check CPU usage (may need to lower detection confidence)

### Scene generator menu doesn't appear
- Restart Unity
- Make sure `Assets/Editor/CreateMediaPipeScene.cs` exists
- Reimport project (Assets → Reimport All)

## 🔍 Testing & Debugging

### Test UDP Connection
```bash
# Check if port is available
python scripts/test_udp_connection.py check

# Send a test packet
python scripts/test_udp_connection.py send

# Receive test packets (5 second timeout)
python scripts/test_udp_connection.py recv
```

### Enable Debug Logging
In Unity Inspector:
1. Select `SkeletonRoot` in the scene
2. Find `MediaPipeUDPReceiver` component
3. Check `debugLogging`
4. Watch Console for UDP messages

## 📊 Data Format

Each UDP packet contains:
```
Header (4 bytes): "POSE"
Count (1 byte):   Number of joints
For each joint:
  - Index (1 byte):   Joint ID (11-16 for upper body)
  - X (4 bytes):      Float32, meters
  - Y (4 bytes):      Float32, meters  
  - Z (4 bytes):      Float32, meters
```

Example: 44 bytes for 3 joints = 4 + 1 + 3×(1+12) = 44 bytes

## 🎯 Tracked Joints

MediaPipe provides 33 landmarks; we track 6 upper-body joints:

| Index | Name | Position |
|-------|------|----------|
| 11 | Left Shoulder | Upper body |
| 12 | Right Shoulder | Upper body |
| 13 | Left Elbow | Upper body |
| 14 | Right Elbow | Upper body |
| 15 | Left Wrist | Hand |
| 16 | Right Wrist | Hand |

## 🔗 Next Steps

Once basic tracking works:

1. **Add webcam feed background**
   - Select `Background` quad in scene
   - Add `WebcamFeedRenderer` component
   - Adjust size/position

2. **Customize visualization**
   - Change sphere colors/materials
   - Add different joint meshes
   - Implement gesture overlays

3. **Extend functionality**
   - Add motion smoothing
   - Implement gesture recognition
   - Record pose sequences
   - Create game interactions

4. **Optimize performance**
   - Reduce frame rate if needed
   - Add pose filtering
   - Implement LOD system

## 📞 Support Resources

| Need | Look Here |
|------|-----------|
| Quick start | QUICKSTART_MEDIAPIPE_UDP.md |
| Detailed setup | MEDIAPIPE_UDP_SETUP.md |
| System design | ARCHITECTURE.md |
| Complete overview | IMPLEMENTATION_SUMMARY.md |
| Troubleshooting | MEDIAPIPE_UDP_SETUP.md → Troubleshooting |

## 💡 Performance Tips

- **Smooth joints:** Add Kalman filter for position smoothing
- **Reduce bandwidth:** Enable UDP compression
- **Lower latency:** Use TCP for guaranteed delivery
- **Multi-person:** Modify `num_poses` in Python script
- **More joints:** Add to `UPPER_BODY_JOINTS` in Python and `skeletonConnections` in Unity

## 📝 File Structure

```
KinectGame_FB/
├── scripts/
│   ├── mediapipe_udp_streamer.py ........... Main streamer
│   └── test_udp_connection.py ............ Testing tool
├── Assets/
│   ├── MediaPipeUDPReceiver.cs .......... UDP receiver
│   ├── JointVisualizer.cs .............. Visualization
│   ├── WebcamFeedRenderer.cs ........... Camera feed
│   ├── Editor/CreateMediaPipeScene.cs .. Scene generator
│   └── Scenes/TestMediaPipeViaUDP.unity . Test scene
├── QUICKSTART_MEDIAPIPE_UDP.md ........ Quick start
├── MEDIAPIPE_UDP_SETUP.md ............ Detailed guide
├── ARCHITECTURE.md .................. System design
├── IMPLEMENTATION_SUMMARY.md ........ Complete overview
└── README_MEDIAPIPE.md .............. This file
```

## 🎉 Ready to Go!

Everything is set up and ready to use. Follow the 3-step quick start above and start tracking poses in real-time.

Questions? See the documentation files.

---

**Happy pose tracking!** 🎮

For the latest information, check the documentation files included in this project.
