#!/usr/bin/env python3
"""
MediaPipe UDP Data Streamer
Streams 3D upper-body joint data via UDP to a Unity receiver.

Data Format:
- Binary format with header "POSE"
- Each joint: jointIndex (byte) + x, y, z (float32 each)
- Timestamp at the end
"""

import cv2
import mediapipe as mp
import time
import socket
import struct
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

# Configuration
MODEL_PATH = "pose_landmarker_lite.task"
UPPER_BODY_JOINTS = [11, 12, 13, 14, 15, 16]  # L/R Shoulder, Elbow, Wrist

UPPER_BODY_CONNECTIONS = [
    (11, 12), (11, 13), (13, 15), (12, 14), (14, 16)
]

# UDP Configuration
UDP_IP = "127.0.0.1"  # localhost
UDP_PORT = 5005
FRAME_RATE = 30  # Target FPS

# Initialize UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
print(f"[UDP Streamer] Sending to {UDP_IP}:{UDP_PORT}")

# Configure MediaPipe
base_options = python.BaseOptions(model_asset_path=MODEL_PATH)
options = vision.PoseLandmarkerOptions(
    base_options=base_options,
    running_mode=vision.RunningMode.VIDEO,
    num_poses=1,
    min_pose_detection_confidence=0.5,
    min_pose_presence_confidence=0.5,
    min_tracking_confidence=0.5
)

cap = cv2.VideoCapture(0)
print("Starting pose streaming... Press 'q' to exit.")

frame_count = 0
last_time = time.time()

with vision.PoseLandmarker.create_from_options(options) as landmarker:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            continue

        frame = cv2.flip(frame, 1)
        h, w, _ = frame.shape

        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        frame_timestamp_ms = int(time.time() * 1000)
        detection_result = landmarker.detect_for_video(mp_image, frame_timestamp_ms)

        # Build UDP packet
        packet = bytearray()
        packet.extend(b'POSE')  # Header magic bytes
        
        if detection_result.pose_landmarks and detection_result.pose_world_landmarks:
            pose_landmarks = detection_result.pose_landmarks[0]
            world_landmarks = detection_result.pose_world_landmarks[0]

            # Add joint count
            packet.append(len(UPPER_BODY_JOINTS))

            # Stream each tracked joint (world coordinates in meters)
            for joint_idx in UPPER_BODY_JOINTS:
                world_lm = world_landmarks[joint_idx]
                
                # Append joint index and 3D coordinates
                packet.append(joint_idx)
                packet.extend(struct.pack('fff', world_lm.x, world_lm.y, world_lm.z))

            # Draw locally for visual feedback
            pose_landmarks_list = detection_result.pose_landmarks[0]
            for connection in UPPER_BODY_CONNECTIONS:
                start = pose_landmarks_list[connection[0]]
                end = pose_landmarks_list[connection[1]]
                start_pt = (int(start.x * w), int(start.y * h))
                end_pt = (int(end.x * w), int(end.y * h))
                cv2.line(frame, start_pt, end_pt, (255, 0, 0), 2)

            for idx in UPPER_BODY_JOINTS:
                lm = pose_landmarks_list[idx]
                pt = (int(lm.x * w), int(lm.y * h))
                cv2.circle(frame, pt, 7, (0, 255, 0), -1)

        else:
            # No pose detected
            packet.append(0)

        # Send packet
        try:
            sock.sendto(bytes(packet), (UDP_IP, UDP_PORT))
        except Exception as e:
            print(f"[UDP Error] {e}")

        # FPS counter
        frame_count += 1
        current_time = time.time()
        elapsed = current_time - last_time
        if elapsed >= 1.0:
            fps = frame_count / elapsed
            cv2.putText(frame, f"FPS: {fps:.1f}", (10, 30),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
            frame_count = 0
            last_time = current_time
        else:
            cv2.putText(frame, f"Streaming on UDP {UDP_PORT}...", (10, 30),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)

        cv2.imshow('MediaPipe UDP Streamer', frame)

        if cv2.waitKey(5) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
sock.close()
print("[UDP Streamer] Closed.")
