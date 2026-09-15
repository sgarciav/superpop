using System;
using System.Collections;
using System.Text;
using System.Threading;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Phase-1 prep: live interactive validation of the MoveNet pipeline.
///
/// Shows the camera as a full square on screen ("see myself"), runs MoveNet every
/// frame, and draws the 17 keypoints + skeleton on top (wrists highlighted).
///
/// TWO capture backends (see <see cref="captureMode"/>):
///   * WebCamTexture   - normal path; works on Windows/macOS and UVC cams on Linux.
///   * ExternalFfmpeg  - spawns ffmpeg to stream raw RGB frames into a Texture2D.
///     Needed on this laptop: its Intel IPU6 "MIPI Camera" is exposed as a
///     v4l2loopback device (/dev/video0) that does NOT support mmap buffers, so
///     Unity's WebCamTexture fails with "device doesn't support mmapped buffers".
///     ffmpeg negotiates read()/USERPTR and works fine.
///
/// Usage: open Assets/Scenes/WebcamPoseTest.unity and press Play. UI is built at
/// runtime. Live tweaks: M = mirror, V = flip vertical.
/// </summary>
public class WebcamPoseTest : MonoBehaviour
{
    public enum CaptureMode { WebCamTexture, ExternalFfmpeg }

    [Header("Capture")]
    [Tooltip("ExternalFfmpeg is required for Intel IPU6 / v4l2loopback cameras on Linux.")]
    public CaptureMode captureMode = CaptureMode.ExternalFfmpeg;

    [Header("Model")]
    public string modelPath = "Assets/Models/movenet.onnx";
    const int Size = 192; // MoveNet Lightning input: 192x192x3 int32 NHWC (0..255)

    [Header("WebCamTexture options")]
    public int requestedWidth = 1280;
    public int requestedHeight = 720;
    public int requestedFps = 30;

    [Header("ExternalFfmpeg options")]
    public string ffmpegPath = "ffmpeg";
    [Tooltip("'auto' scans /dev/video* and prefers a USB/UVC cam over the IPU6 node.")]
    public string ffmpegDevice = "auto";
    public int captureWidth = 640;
    public int captureHeight = 480;
    public int captureFps = 30;

    [Header("Orientation (tweak live: M = mirror, V = flipV)")]
    public bool mirrorHorizontal = true;
    [Tooltip("ffmpeg delivers top-row-first frames, so this defaults ON for that path.")]
    public bool flipVertical = true;

    [Header("Overlay")]
    [Range(0f, 1f)] public float scoreThreshold = 0.30f;
    public float dotRadius = 9f;

    static readonly string[] KP =
    {
        "nose","leftEye","rightEye","leftEar","rightEar",
        "leftShoulder","rightShoulder","leftElbow","rightElbow",
        "leftWrist","rightWrist","leftHip","rightHip",
        "leftKnee","rightKnee","leftAnkle","rightAnkle"
    };
    static readonly int[,] Bones =
    {
        {0,1},{0,2},{1,3},{2,4},           // face
        {5,6},{5,7},{7,9},{6,8},{8,10},    // shoulders + arms
        {5,11},{6,12},{11,12},             // torso
        {11,13},{13,15},{12,14},{14,16}    // legs
    };

    // --- inference ---
    Worker worker;
    Model model;
    BackendType backend;

    // --- WebCamTexture path ---
    WebCamTexture cam;

    // --- ExternalFfmpeg path ---
    System.Diagnostics.Process ffmpeg;
    Thread readThread;
    volatile bool reading;
    readonly object frameLock = new object();
    byte[] latestFrame;          // full RGB24 frame, top-row-first
    volatile bool hasNewFrame;
    volatile bool loggedFirstFrame;
    Texture2D extTex;

    // --- shared source view ---
    Texture srcTexture;          // what the RawImage shows
    Color32[] srcPixels;         // cached pixels for preprocessing
    int srcW, srcH;
    bool sourceReady;

    // --- UI ---
    RawImage videoImage;
    RectTransform squareRT;
    Image[] dots;
    RectTransform[] dotRT;
    Image[] boneImg;
    RectTransform[] boneRT;
    Text infoText;

    int[] nhwc;
    float[] lastKp;

    double emaMs = 0;
    int inferCount = 0;

    void Start()
    {
        BuildUI();
        nhwc = new int[Size * Size * 3];
        LoadModel();
        if (captureMode == CaptureMode.ExternalFfmpeg) StartFfmpeg();
        else StartCoroutine(StartWebcam());
    }

    // ---------------- WebCamTexture path ----------------
    IEnumerator StartWebcam()
    {
        infoText.text = "requesting webcam authorization...";
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[WebcamPoseTest] WebCamTexture.devices is EMPTY.");
            infoText.text = "NO WEBCAM DEVICES (see Console)";
            yield break;
        }
        var sb = new StringBuilder("[WebcamPoseTest] Unity sees " + devices.Length + " device(s):\n");
        foreach (var d in devices) sb.AppendLine($"   name='{d.name}'");
        Debug.Log(sb.ToString());

        string name = devices[0].name;
        cam = new WebCamTexture(name, requestedWidth, requestedHeight, requestedFps);
        cam.Play();
        float t = 0f;
        while (t < 5f && !(cam.isPlaying && cam.width > 16 && cam.didUpdateThisFrame))
        {
            infoText.text = $"starting webcam '{name}'...\nisPlaying={cam.isPlaying} size={cam.width}x{cam.height} t={t:F1}s";
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (cam.isPlaying && cam.width > 16)
        {
            srcTexture = cam;
            videoImage.texture = cam;
            sourceReady = true;
            Debug.Log($"[WebcamPoseTest] Webcam RUNNING {cam.width}x{cam.height}");
        }
        else
        {
            infoText.text = "WEBCAM WOULD NOT START\nTry captureMode=ExternalFfmpeg (see Console).";
            Debug.LogError("[WebcamPoseTest] Webcam failed to start; switch to ExternalFfmpeg.");
        }
    }

    // ---------------- ExternalFfmpeg path ----------------
    // Picks a capture device. Honours an explicit /dev/videoN; otherwise scans and
    // prefers a readable node that is NOT the Intel IPU6 raw/loopback camera.
    string ResolveFfmpegDevice()
    {
        if (!string.IsNullOrEmpty(ffmpegDevice) && ffmpegDevice != "auto")
            return ffmpegDevice;

        var candidates = new System.Collections.Generic.List<(string path, string name, bool preferred)>();
        for (int i = 0; i < 64; i++)
        {
            string path = "/dev/video" + i;
            if (!System.IO.File.Exists(path)) continue;
            bool readable;
            try { using (System.IO.File.OpenRead(path)) readable = true; }
            catch { readable = false; }
            if (!readable) continue;

            string name = "";
            try { name = System.IO.File.ReadAllText($"/sys/class/video4linux/video{i}/name").Trim(); } catch { }
            if (name.Contains("ISYS")) continue; // raw IPU6 nodes, not usable directly

            // Prefer a genuine USB/UVC cam over the built-in MIPI/loopback.
            bool preferred = !(name.Contains("MIPI") || name.Contains("IPU6") || name.ToLower().Contains("loopback"));
            candidates.Add((path, name, preferred));
            Debug.Log($"[WebcamPoseTest] candidate {path} name='{name}' preferred={preferred}");
        }
        foreach (var c in candidates) if (c.preferred) return c.path;
        if (candidates.Count > 0) return candidates[0].path;
        Debug.LogWarning("[WebcamPoseTest] auto-detect found no readable device; falling back to /dev/video0");
        return "/dev/video0";
    }

    void StartFfmpeg()
    {
        ffmpegDevice = ResolveFfmpegDevice();
        Debug.Log("[WebcamPoseTest] using capture device: " + ffmpegDevice);
        int fb = captureWidth * captureHeight * 3;
        latestFrame = new byte[fb];
        extTex = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        srcTexture = extTex;
        videoImage.texture = extTex;

        string args =
            $"-hide_banner -loglevel warning -f v4l2 -framerate {captureFps} " +
            $"-i {ffmpegDevice} -an -vf scale={captureWidth}:{captureHeight} " +
            $"-pix_fmt rgb24 -f rawvideo pipe:1";

        var psi = new System.Diagnostics.ProcessStartInfo(ffmpegPath, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        try
        {
            ffmpeg = new System.Diagnostics.Process { StartInfo = psi };
            ffmpeg.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.Log("[ffmpeg] " + e.Data); };
            ffmpeg.Start();
            ffmpeg.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Debug.LogError("[WebcamPoseTest] Failed to start ffmpeg: " + e.Message);
            infoText.text = "FFMPEG FAILED TO START\nCheck ffmpegPath / device (see Console).";
            return;
        }

        Debug.Log($"[WebcamPoseTest] ffmpeg started: {ffmpegPath} {args}");
        reading = true;
        readThread = new Thread(ReadLoop) { IsBackground = true, Name = "ffmpeg-reader" };
        readThread.Start();
        infoText.text = "starting ffmpeg capture...";
    }

    void ReadLoop()
    {
        int fb = captureWidth * captureHeight * 3;
        byte[] buf = new byte[fb];
        var stream = ffmpeg.StandardOutput.BaseStream;
        try
        {
            while (reading)
            {
                int off = 0;
                while (off < fb)
                {
                    int n = stream.Read(buf, off, fb - off);
                    if (n <= 0) { reading = false; break; } // EOF / ffmpeg died
                    off += n;
                }
                if (off == fb)
                {
                    lock (frameLock)
                    {
                        Buffer.BlockCopy(buf, 0, latestFrame, 0, fb);
                        hasNewFrame = true;
                    }
                    if (!loggedFirstFrame)
                    {
                        loggedFirstFrame = true;
                        long sum = 0; for (int k = 0; k < fb; k += 997) sum += buf[k];
                        Debug.Log($"[WebcamPoseTest] first ffmpeg frame received ({fb} bytes), avg brightness ~{sum / (fb / 997 + 1)}/255");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WebcamPoseTest] ffmpeg read loop ended: " + e.Message);
        }
    }

    // ---------------- Model ----------------
    void LoadModel()
    {
        ModelAsset modelAsset = null;
#if UNITY_EDITOR
        modelAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<ModelAsset>(modelPath);
#endif
        if (modelAsset == null) modelAsset = Resources.Load<ModelAsset>("movenet");
        if (modelAsset == null)
        {
            Debug.LogError("[WebcamPoseTest] Could not load ModelAsset at " + modelPath);
            infoText.text = "MODEL NOT FOUND";
            return;
        }
        model = ModelLoader.Load(modelAsset);

        bool gpuUsable = SystemInfo.supportsComputeShaders
            && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
        backend = gpuUsable ? BackendType.GPUCompute : BackendType.CPU;
        try { worker = new Worker(model, backend); }
        catch (Exception e)
        {
            Debug.LogWarning($"[WebcamPoseTest] GPU backend failed ({e.Message}); falling back to CPU.");
            backend = BackendType.CPU;
            worker = new Worker(model, backend);
        }
        Debug.Log($"[WebcamPoseTest] backend={backend} gfx={SystemInfo.graphicsDeviceType}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) mirrorHorizontal = !mirrorHorizontal;
        if (Input.GetKeyDown(KeyCode.V)) flipVertical = !flipVertical;

        LayoutSquare();
        ApplyDisplayOrientation();

        RefreshSource();
        if (!sourceReady || worker == null) return;

        Preprocess();

        using (var input = new Tensor<int>(new TensorShape(1, Size, Size, 3), nhwc))
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            worker.Schedule(input);
            var output = worker.PeekOutput() as Tensor<float>; // [1,1,17,3] = (y,x,score)
            lastKp = output.DownloadToArray();
            sw.Stop();
            double ms = sw.Elapsed.TotalMilliseconds;
            emaMs = inferCount == 0 ? ms : emaMs * 0.9 + ms * 0.1;
            inferCount++;
        }

        DrawOverlay(lastKp);
        UpdateInfo(lastKp);
    }

    // Pull the newest frame into srcPixels/srcW/srcH.
    void RefreshSource()
    {
        if (captureMode == CaptureMode.ExternalFfmpeg)
        {
            if (!hasNewFrame) return;
            lock (frameLock)
            {
                extTex.LoadRawTextureData(latestFrame);
                hasNewFrame = false;
            }
            extTex.Apply(false);
            srcPixels = extTex.GetPixels32();
            srcW = extTex.width; srcH = extTex.height;
            sourceReady = true;
        }
        else
        {
            if (cam == null || !cam.didUpdateThisFrame || cam.width <= 16) return;
            srcPixels = cam.GetPixels32();
            srcW = cam.width; srcH = cam.height;
            sourceReady = true;
        }
    }

    // ---- Preprocess: build model input to EXACTLY match what's displayed. ----
    // Same mirror/flip flags drive both the RawImage (uvRect) and this sampler, so
    // MoveNet's normalized output maps straight onto the square with no extra transform.
    // srcPixels convention: index [y*srcW + x], y=0 = bottom (Unity's GetPixels32).
    void Preprocess()
    {
        for (int r = 0; r < Size; r++)          // r = 0 -> top of corrected image
        {
            float vTop = (r + 0.5f) / Size;
            float vSrc = flipVertical ? vTop : (1f - vTop);
            int sy = Mathf.Clamp(Mathf.RoundToInt(vSrc * (srcH - 1)), 0, srcH - 1);
            int rowBase = sy * srcW;
            int outRow = r * Size * 3;
            for (int c = 0; c < Size; c++)
            {
                float u = (c + 0.5f) / Size;
                float uSrc = mirrorHorizontal ? (1f - u) : u;
                int sx = Mathf.Clamp(Mathf.RoundToInt(uSrc * (srcW - 1)), 0, srcW - 1);
                Color32 p = srcPixels[rowBase + sx];
                int o = outRow + c * 3;
                nhwc[o + 0] = p.r;
                nhwc[o + 1] = p.g;
                nhwc[o + 2] = p.b;
            }
        }
    }

    // ---------------- Overlay ----------------
    void DrawOverlay(float[] kp)
    {
        float w = squareRT.rect.width;
        float h = squareRT.rect.height;

        for (int b = 0; b < Bones.GetLength(0); b++)
        {
            int a = Bones[b, 0], c = Bones[b, 1];
            if (kp[a * 3 + 2] < scoreThreshold || kp[c * 3 + 2] < scoreThreshold) { boneImg[b].enabled = false; continue; }
            boneImg[b].enabled = true;
            Vector2 pa = new Vector2(kp[a * 3 + 1] * w, -kp[a * 3 + 0] * h);
            Vector2 pc = new Vector2(kp[c * 3 + 1] * w, -kp[c * 3 + 0] * h);
            Vector2 dir = pc - pa;
            var rt = boneRT[b];
            rt.anchoredPosition = (pa + pc) * 0.5f;
            rt.sizeDelta = new Vector2(dir.magnitude, 3f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        for (int i = 0; i < 17; i++)
        {
            if (kp[i * 3 + 2] < scoreThreshold) { dots[i].enabled = false; continue; }
            dots[i].enabled = true;
            dotRT[i].anchoredPosition = new Vector2(kp[i * 3 + 1] * w, -kp[i * 3 + 0] * h);
        }
    }

    void UpdateInfo(float[] kp)
    {
        float lw = kp[9 * 3 + 2], rw = kp[10 * 3 + 2];
        float fps = emaMs > 0 ? (float)(1000.0 / emaMs) : 0;
        infoText.text =
            $"mode: {captureMode}   backend: {backend}   src: {srcW}x{srcH}\n" +
            $"infer: {emaMs:F1} ms  ({fps:F0} FPS)   frames: {inferCount}\n" +
            $"wrists  L(9)={lw:F2}  R(10)={rw:F2}\n" +
            $"mirror(M)={mirrorHorizontal}  flipV(V)={flipVertical}  thr={scoreThreshold:F2}";
    }

    // ---------------- UI ----------------
    void BuildUI()
    {
        var canvasGO = new GameObject("PoseCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        var bg = NewImage("BG", canvasGO.transform, Color.black);
        Stretch(bg.rectTransform);

        var sq = new GameObject("Square", typeof(RectTransform), typeof(RawImage));
        sq.transform.SetParent(canvasGO.transform, false);
        squareRT = sq.GetComponent<RectTransform>();
        squareRT.anchorMin = squareRT.anchorMax = new Vector2(0.5f, 0.5f);
        squareRT.pivot = new Vector2(0f, 1f);
        videoImage = sq.GetComponent<RawImage>();
        videoImage.raycastTarget = false;

        int nb = Bones.GetLength(0);
        boneImg = new Image[nb]; boneRT = new RectTransform[nb];
        for (int b = 0; b < nb; b++)
        {
            var img = NewImage("bone" + b, sq.transform, new Color(0.1f, 1f, 0.6f, 0.9f));
            boneImg[b] = img; boneRT[b] = img.rectTransform;
            boneRT[b].anchorMin = boneRT[b].anchorMax = new Vector2(0f, 1f);
            boneRT[b].pivot = new Vector2(0.5f, 0.5f);
            img.enabled = false;
        }

        dots = new Image[17]; dotRT = new RectTransform[17];
        for (int i = 0; i < 17; i++)
        {
            bool wrist = (i == 9 || i == 10);
            var col = wrist ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(1f, 0.9f, 0.1f, 1f);
            var img = NewImage("kp" + i, sq.transform, col);
            dots[i] = img; dotRT[i] = img.rectTransform;
            dotRT[i].anchorMin = dotRT[i].anchorMax = new Vector2(0f, 1f);
            dotRT[i].pivot = new Vector2(0.5f, 0.5f);
            float d = wrist ? dotRadius * 1.8f : dotRadius;
            dotRT[i].sizeDelta = new Vector2(d, d);
            img.enabled = false;
        }

        var textGO = new GameObject("Info", typeof(Text));
        textGO.transform.SetParent(canvasGO.transform, false);
        infoText = textGO.GetComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 20;
        infoText.color = Color.white;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.raycastTarget = false;
        var trt = infoText.rectTransform;
        trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
        trt.anchoredPosition = new Vector2(12f, -12f);
        trt.sizeDelta = new Vector2(760f, 120f);
        infoText.text = "starting...";
    }

    void LayoutSquare()
    {
        float side = Mathf.Min(Screen.width, Screen.height) * 0.95f;
        squareRT.sizeDelta = new Vector2(side, side);
        squareRT.anchoredPosition = new Vector2(-side * 0.5f, side * 0.5f);
    }

    void ApplyDisplayOrientation()
    {
        if (videoImage == null) return;
        float x = mirrorHorizontal ? 1f : 0f;
        float w = mirrorHorizontal ? -1f : 1f;
        float y = flipVertical ? 1f : 0f;
        float h = flipVertical ? -1f : 1f;
        videoImage.uvRect = new Rect(x, y, w, h);
    }

    Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    void StopFfmpeg()
    {
        reading = false;
        try
        {
            if (ffmpeg != null && !ffmpeg.HasExited) ffmpeg.Kill();
        }
        catch { }
        try { readThread?.Join(500); } catch { }
        ffmpeg?.Dispose();
        ffmpeg = null;
    }

    void OnDisable() { StopFfmpeg(); }

    void OnDestroy()
    {
        StopFfmpeg();
        worker?.Dispose();
        if (cam != null && cam.isPlaying) cam.Stop();
    }
}
