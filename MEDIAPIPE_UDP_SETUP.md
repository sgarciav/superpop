# MediaPipe UDP Network Bridge Setup

This guide helps you set up a real-time pose tracking system using MediaPipe (Python) and Unity.

## Overview

- **Python side:** `scripts/mediapipe_udp_streamer.py` captures pose data from your webcam using MediaPipe and streams it via UDP
- **Unity side:** `MediaPipeUDPReceiver.cs` receives the UDP packets and updates joint positions in real-time
- **Visualization:** `JointVisualizer.cs` renders the skeleton as spheres and lines

## Setup Steps

### 1. Python Prerequisites

Make sure you have the required Python packages installed:
```bash
pip install opencv-python mediapipe
```

Ensure `pose_landmarker_lite.task` is in the `scripts/` directory (should be already downloaded).

### 2. Create Unity Scene

1. In Unity, open the **Project** window
2. Go to **Tools** → **Create MediaPipe UDP Test Scene**
3. This will create a new scene named `TestMediaPipeViaUDP` at `Assets/Scenes/TestMediaPipeViaUDP.unity`

### 3. Configure the Receiver Script

In the Inspector for `SkeletonRoot` → `MediaPipeUDPReceiver`:
- **UDP Port**: 5005 (default, matches Python side)
- **Position Scale**: 1.0 (adjust if joints look too large/small)
- **Debug Logging**: Toggle for UDP debug messages

### 4. Run the Stream

In your terminal/cmd, navigate to the project root and run:
```bash
python scripts/mediapipe_udp_streamer.py
```

You should see:
- A window showing your webcam with skeleton overlay
- "Streaming on UDP 5005..." message in the window
- Terminal output confirming the UDP socket is active

### 5. Run Unity

1. In Unity, open the `TestMediaPipeViaUDP` scene
2. Press **Play**
3. You should see:
   - The skeleton joints appear on screen (green spheres)
   - The skeleton lines connecting joints (cyan)
   - Real-time updates as you move

## Troubleshooting

### Python script won't start
- Check that `pose_landmarker_lite.task` exists in the `scripts/` directory
- Ensure your webcam is working: `python -c "import cv2; print(cv2.VideoCapture(0).isOpened())"`

### No joints appearing in Unity
- Check the Python console output - you should see pose tracking working
- Verify firewall isn't blocking UDP port 5005
- Enable **Debug Logging** in `MediaPipeUDPReceiver` to see if packets are arriving
- Try running both on `localhost` (127.0.0.1) first

### Joints not moving
- Check that MediaPipe is detecting your pose (Python window should show skeleton)
- Verify the `jointTransforms` array is properly assigned in the Inspector

### Performance Issues
- Reduce Python frame processing (lower FPS target in `mediapipe_udp_streamer.py`)
- Reduce UDP packet size if bandwidth-limited
- Disable debug logging

## Network Configuration

The default setup uses:
- **IP**: 127.0.0.1 (localhost)
- **Port**: 5005

To stream across a network:
1. In `mediapipe_udp_streamer.py`, change `UDP_IP` to your target machine's IP
2. Make sure firewall allows UDP port 5005

## Data Format

UDP packets sent from Python to Unity:

```
Header: "POSE" (4 bytes)
Joint Count: 1 byte
For each joint:
  - Joint Index: 1 byte
  - X coordinate: 4 bytes (float32, meters)
  - Y coordinate: 4 bytes (float32, meters)
  - Z coordinate: 4 bytes (float32, meters)
```

## Tracked Joints

Currently tracking upper-body joints (MediaPipe indices):
- **11**: Left Shoulder
- **12**: Right Shoulder
- **13**: Left Elbow
- **14**: Right Elbow
- **15**: Left Wrist
- **16**: Right Wrist

To track more joints, edit both:
- `UPPER_BODY_JOINTS` in `mediapipe_udp_streamer.py`
- `skeletonConnections` in `JointVisualizer.cs`

## Script Files

- `scripts/mediapipe_udp_streamer.py` - Python UDP streamer
- `Assets/MediaPipeUDPReceiver.cs` - Unity receiver
- `Assets/JointVisualizer.cs` - Skeleton visualization
- `Assets/Editor/CreateMediaPipeScene.cs` - Scene generator

## Next Steps

Once basic tracking works, you can:
1. Replace the background quad with webcam texture streaming
2. Add hand gesture recognition
3. Implement pose-based game interactions
4. Add filtering for smoother joint tracking
5. Record pose sequences for later playback
