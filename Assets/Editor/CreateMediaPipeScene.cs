using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CreateMediaPipeScene
{
    [MenuItem("Tools/Create MediaPipe UDP Test Scene")]
    public static void CreateScene()
    {
        // Create new scene using editor API
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create main camera
        GameObject cameraObj = new GameObject("Main Camera");
        Camera mainCam = cameraObj.AddComponent<Camera>();
        cameraObj.tag = "MainCamera";
        mainCam.backgroundColor = Color.black;
        cameraObj.transform.position = new Vector3(0, 0.5f, -1f);
        cameraObj.transform.LookAt(Vector3.zero);

        // Create root object for skeleton
        GameObject skeletonRoot = new GameObject("SkeletonRoot");
        skeletonRoot.transform.position = Vector3.zero;

        // Create a simple background (quad to show video feed later)
        GameObject backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backgroundQuad.name = "Background";
        backgroundQuad.transform.position = new Vector3(0, 0, 0);
        backgroundQuad.transform.localScale = new Vector3(3.2f, 1.8f, 1);
        backgroundQuad.transform.Rotate(-90, 0, 0);
        
        MeshCollider collider = backgroundQuad.GetComponent<MeshCollider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        // Create 17 joint transforms
        Transform[] jointTransforms = new Transform[17];
        for (int i = 0; i < 17; i++)
        {
            GameObject joint = new GameObject($"Joint_{i}");
            joint.transform.SetParent(skeletonRoot.transform);
            joint.transform.localPosition = Vector3.zero;
            jointTransforms[i] = joint.transform;
        }

        // Add receiver to root
        MediaPipeUDPReceiver receiver = skeletonRoot.AddComponent<MediaPipeUDPReceiver>();
        
        // Assign joint transforms to receiver using reflection
        var field = typeof(MediaPipeUDPReceiver).GetField("jointTransforms", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(receiver, jointTransforms);
        }

        // Add visualizer
        JointVisualizer visualizer = skeletonRoot.AddComponent<JointVisualizer>();

        // Create a canvas for UI feedback
        GameObject canvasObj = new GameObject("UICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        // Add status text using TextMeshPro
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(canvasObj.transform);
        TextMeshProUGUI statusText = textObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Waiting for UDP data on port 5005...\nMake sure mediapipe_udp_streamer.py is running";
        statusText.fontSize = 36;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        statusText.color = Color.green;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(10, -10);
        textRect.offsetMax = new Vector2(-10, -10);
        textRect.sizeDelta = new Vector2(0, 100);

        // Ensure Scenes directory exists
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        
        // Save scene
        string scenePath = "Assets/Scenes/TestMediaPipeViaUDP.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        
        Debug.Log($"✓ Scene created at {scenePath}");
        Debug.Log($"✓ You can now open the scene: Assets/Scenes/TestMediaPipeViaUDP.unity");
        Debug.Log("Ready to play! Press Play in the editor.");
    }
}
