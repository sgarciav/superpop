import cv2
import mediapipe as mp
import time
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

# 1. Define model path and targeted upper-body joint indices
MODEL_PATH = "pose_landmarker_lite.task" # Make sure this matches your downloaded file name
UPPER_BODY_JOINTS = [11, 12, 13, 14, 15, 16] # Left/Right: Shoulder (11/12), Elbow (13/14), Wrist (15/16)

# Hardcoded skeleton lines between the targeted upper-body joints
UPPER_BODY_CONNECTIONS = [
    (11, 12), # Shoulder to Shoulder
    (11, 13), # Left Shoulder to Left Elbow
    (13, 15), # Left Elbow to Left Wrist
    (12, 14), # Right Shoulder to Right Elbow
    (14, 16)  # Right Elbow to Right Wrist
]

# 2. Configure the Pose Landmarker for live webcam streaming
base_options = python.BaseOptions(model_asset_path=MODEL_PATH)
options = vision.PoseLandmarkerOptions(
    base_options=base_options,
    running_mode=vision.RunningMode.VIDEO,
    num_poses=1,
    min_pose_detection_confidence=0.5,
    min_pose_presence_confidence=0.5,
    min_tracking_confidence=0.5
)

# Open webcam frame pipeline
cap = cv2.VideoCapture(0)
print("Starting tracking... Press 'q' to exit.")

# Initialize landmarker instance
with vision.PoseLandmarker.create_from_options(options) as landmarker:
    while cap.isOpened():
        success, frame = cap.read()
        if not success:
            continue

        # Flip horizontally for natural mirror behavior
        frame = cv2.flip(frame, 1)
        h, w, _ = frame.shape

        # Convert colorspace and wrap for MediaPipe
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        frame_timestamp_ms = int(time.time() * 1000)
        detection_result = landmarker.detect_for_video(mp_image, frame_timestamp_ms)

        # 3. Draw the skeleton structure and extract 3D World data
        if detection_result.pose_landmarks and detection_result.pose_world_landmarks:
            # We assume a single person (index 0) is being tracked
            pose_landmarks = detection_result.pose_landmarks[0]
            world_landmarks = detection_result.pose_world_landmarks[0]

            # Draw structural connection lines
            for connection in UPPER_BODY_CONNECTIONS:
                start_joint = pose_landmarks[connection[0]]
                end_joint = pose_landmarks[connection[1]]

                start_point = (int(start_joint.x * w), int(start_joint.y * h))
                end_point = (int(end_joint.x * w), int(end_joint.y * h))

                cv2.line(frame, start_point, end_point, (255, 0, 0), 2)

            # Draw joint points
            for idx in UPPER_BODY_JOINTS:
                landmark = pose_landmarks[idx]
                pixel_x = int(landmark.x * w)
                pixel_y = int(landmark.y * h)
                cv2.circle(frame, (pixel_x, pixel_y), 7, (0, 255, 0), -1)

            # --- Extract & Overlay 3D World Coordinates (Meters) ---
            # Indices: 15 is Left Wrist, 16 is Right Wrist
            for wrist_idx, name in [(15, "L Wrist"), (16, "R Wrist")]:
                w_lm = world_landmarks[wrist_idx]
                img_lm = pose_landmarks[wrist_idx]

                # Get the pixel location to display the text next to the hand
                text_x = int(img_lm.x * w) + 15
                text_y = int(img_lm.y * h)

                # Format string showing coordinates in meters relative to the body's center [1]
                coord_text = f"{name}: X:{w_lm.x:.2f} Y:{w_lm.y:.2f} Z:{w_lm.z:.2f} (m)"

                # Draw text shadow for better visibility on moving backgrounds
                cv2.putText(frame, coord_text, (text_x+1, text_y+1), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 0, 0), 2)
                cv2.putText(frame, coord_text, (text_x, text_y), cv2.FONT_HERSHEY_SIMPLEX, 0.4, (0, 255, 255), 1)

        # Present the output display pipeline
        cv2.imshow('MediaPipe Tasks - 3D Coordinate Overlay', frame)

        if cv2.waitKey(5) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
