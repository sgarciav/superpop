using System;
using System.IO;
using System.Text;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase-0 validation: loads MoveNet SinglePose Lightning, runs one inference on a
/// static test image, and logs the 17 keypoints. Run headless via:
///   Unity -batchmode -nographics -quit -projectPath &lt;proj&gt; -executeMethod PoseValidationRunner.Run
/// </summary>
public static class PoseValidationRunner
{
    const string ModelPath = "Assets/Models/movenet.onnx";
    const string ImagePath = "Assets/TestImages/person.jpg";
    const int Size = 192; // MoveNet Lightning input is 192x192x3 (int32, NHWC)

    static readonly string[] KP =
    {
        "nose","leftEye","rightEye","leftEar","rightEar",
        "leftShoulder","rightShoulder","leftElbow","rightElbow",
        "leftWrist","rightWrist","leftHip","rightHip",
        "leftKnee","rightKnee","leftAnkle","rightAnkle"
    };

    public static void Run()
    {
        int exitCode = 0;
        Worker worker = null;
        Tensor<int> input = null;
        try
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
            if (modelAsset == null) throw new Exception("ModelAsset not found/imported at " + ModelPath);
            Model model = ModelLoader.Load(modelAsset);

            // ---- Backend selection: GPU if a real compute-capable device is present, else CPU ----
            bool gpuUsable = SystemInfo.supportsComputeShaders
                && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            BackendType backend = gpuUsable ? BackendType.GPUCompute : BackendType.CPU;
            Debug.Log($"[PoseValidation] supportsComputeShaders={SystemInfo.supportsComputeShaders} gfxDevice={SystemInfo.graphicsDeviceType} -> backend={backend}");

            // ---- Preprocess image -> int32 NHWC [1,192,192,3], values 0..255 ----
            int[] data = LoadImageAsNHWCInt(ImagePath, Size);
            input = new Tensor<int>(new TensorShape(1, Size, Size, 3), data);

            // ---- Inference (warm-up + timed steady-state loop) ----
            worker = new Worker(model, backend);

            var swWarm = System.Diagnostics.Stopwatch.StartNew();
            worker.Schedule(input);
            var output = worker.PeekOutput() as Tensor<float>;   // [1,1,17,3] = (y,x,score)
            float[] kp = output.DownloadToArray();
            swWarm.Stop();
            Debug.Log($"[PoseValidation] first inference (warm-up) {swWarm.ElapsedMilliseconds} ms; output len={kp.Length} (expect 51)");

            const int iters = 15;
            double total = 0, min = double.MaxValue, max = 0;
            for (int i = 0; i < iters; i++)
            {
                var t = System.Diagnostics.Stopwatch.StartNew();
                worker.Schedule(input);
                kp = (worker.PeekOutput() as Tensor<float>).DownloadToArray();
                t.Stop();
                double ms = t.Elapsed.TotalMilliseconds;
                total += ms; min = Math.Min(min, ms); max = Math.Max(max, ms);
            }
            double avg = total / iters;
            Debug.Log($"[PoseValidation] steady-state over {iters} runs: avg={avg:F1} ms ({1000.0/avg:F1} FPS)  min={min:F1}  max={max:F1}");

            var sb = new StringBuilder();
            sb.AppendLine("[PoseValidation] keypoints (name: x, y, score):");
            for (int i = 0; i < 17 && (i * 3 + 2) < kp.Length; i++)
            {
                float y = kp[i * 3 + 0];
                float x = kp[i * 3 + 1];
                float s = kp[i * 3 + 2];
                sb.AppendLine($"  {i,2} {KP[i],-14} x={x:F3} y={y:F3} score={s:F3}");
            }
            Debug.Log(sb.ToString());

            float lw = kp[9 * 3 + 2], rw = kp[10 * 3 + 2];
            Debug.Log($"[PoseValidation] WRISTS  left(idx9) score={lw:F3}  right(idx10) score={rw:F3}");
            Debug.Log("[PoseValidation] RESULT=SUCCESS");
        }
        catch (Exception e)
        {
            Debug.LogError("[PoseValidation] RESULT=FAILURE " + e);
            exitCode = 1;
        }
        finally
        {
            input?.Dispose();
            worker?.Dispose();
        }
        EditorApplication.Exit(exitCode);
    }

    // Decodes a jpg/png on the CPU and nearest-neighbour resizes to size x size,
    // emitting NHWC int32 RGB (top row first).
    static int[] LoadImageAsNHWCInt(string path, int size)
    {
        byte[] bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) throw new Exception("Failed to decode image " + path);
        int sw = tex.width, sh = tex.height;
        Color32[] src = tex.GetPixels32(); // (0,0) is bottom-left, row-major bottom-up

        int[] data = new int[size * size * 3];
        for (int r = 0; r < size; r++)          // r = 0 -> top of output
        {
            int syTop = Mathf.Clamp(Mathf.RoundToInt((r + 0.5f) / size * sh - 0.5f), 0, sh - 1);
            int syBottomUp = sh - 1 - syTop;
            for (int c = 0; c < size; c++)
            {
                int sx = Mathf.Clamp(Mathf.RoundToInt((c + 0.5f) / size * sw - 0.5f), 0, sw - 1);
                Color32 p = src[syBottomUp * sw + sx];
                int o = (r * size + c) * 3;
                data[o + 0] = p.r;
                data[o + 1] = p.g;
                data[o + 2] = p.b;
            }
        }
        UnityEngine.Object.DestroyImmediate(tex);
        return data;
    }
}
