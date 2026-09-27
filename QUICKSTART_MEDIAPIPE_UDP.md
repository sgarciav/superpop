# Quick Start: MediaPipe UDP Real-time Pose Tracking

## 🚀 30-Second Setup

### Terminal 1: Start Python UDP Streamer
```bash
cd /path/to/KinectGame_FB
python scripts/mediapipe_udp_streamer.py
```

**Expected output:**
- A window showing your webcam with skeleton overlay
- "Streaming on UDP 5005..." message at bottom

### Terminal 2: Unity
1. Open Unity
2. Open scene: `Assets/Scenes/TestMediaPipeViaUDP.unity`
   - If scene doesn't exist, go to **Tools → Create MediaPipe UDP Test Scene**
3. Press **Play**
4. You should see green spheres (joints) moving with your body!

---

## 📋 What You're Getting

| Component | Purpose |
|-----------|---------|
| `mediapipe_udp_streamer.py` | Captures webcam, detects pose, sends UDP packets |
| `MediaPipeUDPReceiver.cs` | Listens for UDP packets, updates joint positions |
| `JointVisualizer.cs` | Renders skeleton as spheres and lines |
| `TestMediaPipeViaUDP.unity` | Scene with everything wired up |

---

## ✅ Verification Checklist

- [ ] Python script shows skeleton in OpenCV window
- [ ] Python prints "Streaming on UDP 5005..."
- [ ] Unity plays the scene without errors
- [ ] Green spheres appear on screen
- [ ] Spheres move when you move your body
- [ ] Cyan lines connect shoulder → elbow → wrist

---

## 🔧 If Something's Not Working

### Python script crashes
```bash
# Check MediaPipe is installed
pip install mediapipe opencv-python

# Check pose_landmarker_lite.task exists
ls scripts/pose_landmarker_lite.task
```

### No joints in Unity
1. Check Python window - does skeleton appear there?
2. Enable Debug Logging in `MediaPipeUDPReceiver` (Inspector)
3. Check if port 5005 is being blocked by firewall
4. Try running both on same machine (localhost)

### Joints freeze or are laggy
- **Python side:** Reduce FPS or resolution in `mediapipe_udp_streamer.py`
- **Unity side:** Adjust `positionScale` in `MediaPipeUDPReceiver`
- Check CPU usage - may need to lower MediaPipe detection confidence

---

## 📡 Network Details

**Default Configuration:**
- IP: `127.0.0.1` (localhost only)
- Port: `5005`
- Protocol: UDP

**To use over network:**
1. Find your machine's local IP: `ipconfig` (Windows) or `ifconfig` (Mac/Linux)
2. Update `UDP_IP` in `mediapipe_udp_streamer.py`
3. Make sure firewall allows port 5005 UDP traffic

---

## 🎮 Next: Add Camera Feed

To display the actual webcam feed as background:

1. In the `TestMediaPipeViaUDP` scene, select the `Background` quad
2. Add a `RawImage` component (if not present)
3. Add `WebcamFeedRenderer.cs` script
4. Press Play

The live feed will display behind the skeleton overlay!

---

## 📚 Full Documentation

See `MEDIAPIPE_UDP_SETUP.md` for:
- Detailed troubleshooting
- Data format specification
- How to track additional joints
- Performance optimization tips

---

## 💾 Data Being Streamed

Each UDP packet contains:
- Header: `"POSE"`
- 6 upper-body joints (shoulders, elbows, wrists)
- 3D coordinates in meters (relative to body center)
- Position updates at ~30 FPS

---

## 🎯 What's Next?

Once basic tracking works:

1. **Add hand gesture recognition** - Detect if hands are open/closed
2. **Replace background** - Stream actual camera feed instead of static quad
3. **Add filtering** - Smooth jerky joint movements
4. **Record sequences** - Save pose data for playback/training
5. **Game integration** - Use poses to control game characters

---

## 📞 Debugging Commands

```bash
# Test if webcam works
python -c "import cv2; print('Webcam OK' if cv2.VideoCapture(0).isOpened() else 'No webcam')"

# Check UDP port
netstat -un | grep 5005  # Linux/Mac
netstat -an | findstr 5005  # Windows

# Monitor UDP traffic (Linux/Mac)
tcpdump -i lo udp port 5005
```

---

Enjoy real-time pose tracking! 🎉
