using UnityEngine;

/// <summary>
/// Visualizes MediaPipe joints as spheres and skeleton lines.
/// </summary>
public class JointVisualizer : MonoBehaviour
{
    [System.Serializable]
    public struct SkeletonConnection
    {
        public int startJoint;
        public int endJoint;
    }

    [SerializeField] private float jointRadius = 0.02f;
    [SerializeField] private Material jointMaterial;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private SkeletonConnection[] skeletonConnections = new[]
    {
        new SkeletonConnection { startJoint = 11, endJoint = 12 }, // Shoulders
        new SkeletonConnection { startJoint = 11, endJoint = 13 }, // Left shoulder to elbow
        new SkeletonConnection { startJoint = 13, endJoint = 15 }, // Left elbow to wrist
        new SkeletonConnection { startJoint = 12, endJoint = 14 }, // Right shoulder to elbow
        new SkeletonConnection { startJoint = 14, endJoint = 16 }  // Right elbow to wrist
    };

    private GameObject[] jointSpheres = new GameObject[17];
    private LineRenderer[] skeletonLines;
    private Transform[] jointTransforms;

    private void Start()
    {
        // Get the receiver to get existing joint transforms
        MediaPipeUDPReceiver receiver = GetComponent<MediaPipeUDPReceiver>();
        if (receiver != null)
        {
            // Use reflection to get the joint transforms from receiver
            var field = typeof(MediaPipeUDPReceiver).GetField("jointTransforms", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                jointTransforms = (Transform[])field.GetValue(receiver);
            }
        }

        // If we got the transforms from receiver, use them for visuals
        if (jointTransforms != null && jointTransforms.Length == 17)
        {
            AddVisualsToExistingJoints();
        }
        else
        {
            // Fallback: create new joints (shouldn't happen)
            CreateJointVisuals();
        }

        CreateSkeletonLines();
    }

    private void AddVisualsToExistingJoints()
    {
        // Add visual components to existing joint GameObjects
        for (int i = 0; i < 17; i++)
        {
            if (jointTransforms[i] != null)
            {
                GameObject joint = jointTransforms[i].gameObject;

                // Add mesh components if not already present
                if (joint.GetComponent<MeshFilter>() == null)
                {
                    MeshFilter meshFilter = joint.AddComponent<MeshFilter>();
                    meshFilter.mesh = CreateSphereMesh();
                }

                if (joint.GetComponent<MeshRenderer>() == null)
                {
                    MeshRenderer meshRenderer = joint.AddComponent<MeshRenderer>();
                    meshRenderer.material = jointMaterial != null ? jointMaterial : new Material(Shader.Find("Standard"));
                    meshRenderer.material.color = Color.green;
                }

                jointSpheres[i] = joint;
            }
        }
    }

    private void CreateJointVisuals()
    {
        // Create joint spheres (fallback if receiver didn't set them up)
        for (int i = 0; i < 17; i++)
        {
            GameObject sphere = new GameObject($"Joint_{i}");
            sphere.transform.SetParent(transform);
            sphere.transform.localPosition = Vector3.zero;

            SphereCollider sphereCollider = sphere.AddComponent<SphereCollider>();
            sphereCollider.radius = jointRadius;

            MeshFilter meshFilter = sphere.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateSphereMesh();

            MeshRenderer meshRenderer = sphere.AddComponent<MeshRenderer>();
            meshRenderer.material = jointMaterial != null ? jointMaterial : new Material(Shader.Find("Standard"));
            meshRenderer.material.color = Color.green;

            jointSpheres[i] = sphere;
        }
    }

    private void CreateSkeletonLines()
    {
        skeletonLines = new LineRenderer[skeletonConnections.Length];

        for (int i = 0; i < skeletonConnections.Length; i++)
        {
            GameObject lineObj = new GameObject($"SkeletonLine_{i}");
            lineObj.transform.SetParent(transform);
            
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.cyan;
            lineRenderer.positionCount = 2;

            skeletonLines[i] = lineRenderer;
        }
    }

    private Mesh CreateSphereMesh()
    {
        Mesh mesh = new Mesh();
        int subdivisions = 2;
        int vertexCount = (subdivisions + 1) * (subdivisions + 1) * 6;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(subdivisions * subdivisions * 6) * 6];

        // Simple icosphere implementation
        // For now, just use Unity's built-in sphere
        mesh = Resources.Load<Mesh>("Meshes/Sphere");
        if (mesh == null)
        {
            // Create simple cube as fallback
            mesh = new Mesh();
            float s = jointRadius;
            mesh.vertices = new[] {
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
                new Vector3(-s, -s, s), new Vector3(s, -s, s), new Vector3(s, s, s), new Vector3(-s, s, s)
            };
            mesh.triangles = new[] {
                0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4, 2, 3, 7, 2, 7, 6,
                0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
            };
        }

        return mesh;
    }

    private void LateUpdate()
    {
        // Update skeleton lines based on joint positions
        for (int i = 0; i < skeletonLines.Length; i++)
        {
            int startIdx = skeletonConnections[i].startJoint;
            int endIdx = skeletonConnections[i].endJoint;

            if (startIdx < jointSpheres.Length && endIdx < jointSpheres.Length)
            {
                Vector3 startPos = jointSpheres[startIdx].transform.position;
                Vector3 endPos = jointSpheres[endIdx].transform.position;

                skeletonLines[i].SetPosition(0, startPos);
                skeletonLines[i].SetPosition(1, endPos);
            }
        }
    }
}
