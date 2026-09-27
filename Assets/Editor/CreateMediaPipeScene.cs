using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateMediaPipeScene
{
    [MenuItem("Tools/Create MediaPipe UDP Test Scene")]
    public static void CreateScene()
    {
        // Create new scene
        Scene scene = SceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
        scene.name = "TestMediaPipeViaUDP";

        // Find the main camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = Color.black;
            mainCam.transform.position = new Vector3(0, 0.5f, -1f);
            mainCam.transform.LookAt(Vector3.zero);
        }

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

        // Add status text
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(canvasObj.transform);
        Text statusText = textObj.AddComponent<Text>();
        statusText.text = "Waiting for UDP data on port 5005...\nMake sure mediapipe_udp_streamer.py is running";
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = 20;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = Color.green;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(10, -10);
        textRect.offsetMax = new Vector2(-10, -10);
        textRect.sizeDelta = new Vector2(0, 100);

        // Save scene
        string scenePath = "Assets/Scenes/TestMediaPipeViaUDP.unity";
        SceneManager.SaveScene(scene, scenePath);
        
        Debug.Log($"Scene created at {scenePath}");
        Debug.Log("Make sure to assign joint transforms in the MediaPipeUDPReceiver inspector!");
    }
}
