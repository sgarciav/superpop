import cv2
import mediapipe as mp
import time
from mediapipe.tasks import python
from mediapipe.tasks.python import vision

# 1. Define model path and targeted upper-body joint indices
MODEL_PATH = "pose_landmarker_lite.task"
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
    running_mode=vision.RunningMode.VIDEO, # Required for tracking motion across video frames
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

        # Convert framework colorspace and construct a MediaPipe image wrapper
        rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_frame)

        # The Tasks API requires a monotonically increasing timestamp in milliseconds
        frame_timestamp_ms = int(time.time() * 1000)

        # Run inference frame
        detection_result = landmarker.detect_for_video(mp_image, frame_timestamp_ms)

        # 3. Draw the skeleton structure if a body is detected
        if detection_result.pose_landmarks:
            for pose_landmarks in detection_result.pose_landmarks:

                # Draw structural connection lines
                for connection in UPPER_BODY_CONNECTIONS:
                    start_joint = pose_landmarks[connection[0]]
                    end_joint = pose_landmarks[connection[1]]

                    # Convert normalized positions back into visual pixel spaces
                    start_point = (int(start_joint.x * w), int(start_joint.y * h))
                    end_point = (int(end_joint.x * w), int(end_joint.y * h))

                    cv2.line(frame, start_point, end_point, (255, 0, 0), 2)

                # Draw joint points
                for idx in UPPER_BODY_JOINTS:
                    landmark = pose_landmarks[idx]
                    pixel_x = int(landmark.x * w)
                    pixel_y = int(landmark.y * h)

                    # Bright neon dot indicator for tracking focus
                    cv2.circle(frame, (pixel_x, pixel_y), 7, (0, 255, 0), -1)

        # Present the output display pipeline
        cv2.imshow('MediaPipe Tasks - Upper Body Tracking', frame)

        if cv2.waitKey(5) & 0xFF == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
