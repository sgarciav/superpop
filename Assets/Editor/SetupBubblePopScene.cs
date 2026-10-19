using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using com.rfilkov.components;
using com.rfilkov.kinect;

public class SetupBubblePopScene
{
    [MenuItem("Tools/Setup BubblePopper Scene with MediaPipe")]
    public static void SetupScene()
    {
        // Open the BubblePopper scene
        string scenePath = "Assets/BubblePopper/KinectBubblePop.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "Could not open KinectBubblePop.unity scene", "OK");
            return;
        }

        // Find existing components
        var gameManager = Object.FindObjectOfType<GameManager>();
        var jointTrackingManager = Object.FindObjectOfType<JointTrackingManager>();
        var leftHand = gameManager?.leftHand;
        var rightHand = gameManager?.rightHand;

        if (gameManager == null)
        {
            EditorUtility.DisplayDialog("Error", "GameManager not found in scene", "OK");
            return;
        }

        if (leftHand == null || rightHand == null)
        {
            EditorUtility.DisplayDialog("Error", "leftHand or rightHand not found in GameManager", "OK");
            return;
        }

        Debug.Log($"[SetupBubblePopScene] Found GameManager, leftHand, and rightHand");

        // 1. Add JointTrackingManager if not present
        if (jointTrackingManager == null)
        {
            GameObject jtmObject = new GameObject("JointTrackingManager");
            jointTrackingManager = jtmObject.AddComponent<JointTrackingManager>();
            Debug.Log("[SetupBubblePopScene] Created JointTrackingManager");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        
        // Switch to MediaPipe (this will initialize on Start)
        jointTrackingManager.SwitchTracker(JointTrackingManager.TrackerType.MediaPipe);
        Debug.Log("[SetupBubblePopScene] JointTrackingManager set to MediaPipe mode");

        // 2. Add MediaPipeJointManager if not present
        var mediaPipeManager = Object.FindObjectOfType<MediaPipeJointManager>();
        if (mediaPipeManager == null)
        {
            GameObject mpmObject = new GameObject("MediaPipeJointManager");
            mediaPipeManager = mpmObject.AddComponent<MediaPipeJointManager>();
            Debug.Log("[SetupBubblePopScene] Created MediaPipeJointManager");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            Debug.Log("[SetupBubblePopScene] MediaPipeJointManager already exists");
        }

        // 3. Add FollowUserJointPose to leftHand
        var leftHandFollower = leftHand.GetComponent<FollowUserJointPose>();
        if (leftHandFollower == null)
        {
            leftHandFollower = leftHand.AddComponent<FollowUserJointPose>();
            leftHandFollower.followJoint = KinectInterop.JointType.WristLeft;
            leftHandFollower.playerIndex = 0;
            leftHandFollower.moveTransform = true;
            leftHandFollower.rotateTransform = false;
            leftHandFollower.smoothFactor = 10f;
            Debug.Log("[SetupBubblePopScene] Added FollowUserJointPose to leftHand (WristLeft)");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            leftHandFollower.followJoint = KinectInterop.JointType.WristLeft;
            Debug.Log("[SetupBubblePopScene] FollowUserJointPose on leftHand already exists, updated to WristLeft");
        }

        // 4. Add FollowUserJointPose to rightHand
        var rightHandFollower = rightHand.GetComponent<FollowUserJointPose>();
        if (rightHandFollower == null)
        {
            rightHandFollower = rightHand.AddComponent<FollowUserJointPose>();
            rightHandFollower.followJoint = KinectInterop.JointType.WristRight;
            rightHandFollower.playerIndex = 0;
            rightHandFollower.moveTransform = true;
            rightHandFollower.rotateTransform = false;
            rightHandFollower.smoothFactor = 10f;
            Debug.Log("[SetupBubblePopScene] Added FollowUserJointPose to rightHand (WristRight)");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            rightHandFollower.followJoint = KinectInterop.JointType.WristRight;
            Debug.Log("[SetupBubblePopScene] FollowUserJointPose on rightHand already exists, updated to WristRight");
        }

        // Save the scene
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Success", 
            "BubblePopper scene setup complete!\n\n" +
            "✓ JointTrackingManager configured (MediaPipe mode)\n" +
            "✓ MediaPipeJointManager added\n" +
            "✓ leftHand tracking WristLeft\n" +
            "✓ rightHand tracking WristRight\n\n" +
            "Scene saved. Press Play to test!", 
            "OK");

        Debug.Log("[SetupBubblePopScene] ===== SETUP COMPLETE =====");
        Debug.Log("[SetupBubblePopScene] Make sure MediaPipe Python script is running:");
        Debug.Log("[SetupBubblePopScene] python3 scripts/mediapipe_udp_streamer.py");
    }
}
