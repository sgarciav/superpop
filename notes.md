What we proved

 Ran MoveNet SinglePose Lightning inside Unity 6000.6.0f1, on CPU, headless, with a real photo — and got a correct pose:

 ┌────────────────────────────────┬───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
 │ Metric                         │ Result                                                                                                            │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ Compiles & runs                │ ✅ exit 0, RESULT=SUCCESS                                                                                         │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ Output shape                   │ [1,1,17,3] = 51 values (17 keypoints × y,x,score) ✅                                                              │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ Keypoints                      │ Anatomically coherent (head→shoulders→elbows→wrists descend correctly; occluded legs correctly low-confidence) ✅ │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ Wrists (our targets, idx 9/10) │ detected, scores 0.50 / 0.77 ✅                                                                                   │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ First inference (warm-up)      │ 305 ms                                                                                                            │
 ├────────────────────────────────┼───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
 │ Steady-state CPU               │ avg 13.2 ms → ~76 FPS (min 10.4, max 16.7)                                                                        │
 └────────────────────────────────┴───────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

 So MoveNet Lightning is comfortably real-time on CPU — no GPU required. (Note: measured on a 20-core box with the multithreaded CPU backend; a modest 4-core machine will be slower but
 Lightning is light enough to stay real-time. GPU path will engage automatically where available.)

 Key technical findings (these shape the real integration)

 1. Sentis was renamed. On Unity 6, com.unity.sentis is a deprecated stub → the real package is com.unity.ai.inference, namespace Unity.InferenceEngine. We use 2.6.0 (2.2.1 tripped an
    analyzer error on this editor). API is otherwise identical to Sentis (Worker, Schedule, PeekOutput, Tensor<T>, ModelLoader).
 2. Model I/O contract: input [1,192,192,3] int32 NHWC (0–255 pixels, not float NCHW); output [1,1,17,3] normalized (y, x, score). Preprocessing must produce int32 NHWC — noted for the real
    webcam pipeline.
 3. Backend auto-selection needs care: SystemInfo.supportsComputeShaders returns True even headless. The robust check that works is supportsComputeShaders && graphicsDeviceType != Null → CPU
    headless, GPU on real devices. (For production I'd also add a try/GPU → catch/CPU fallback.)
 4. Model source/license: Xenova/movenet-singlepose-lightning (HuggingFace), MoveNet is Apache-2.0 — commercial-friendly.

 What's in the repo now

 An isolated Unity 6 project at PoseValidation/ (sibling of the game's Assets/, so it can't affect the game):
 - Assets/Models/movenet.onnx, Assets/TestImages/person.jpg
 - Assets/Editor/PoseValidationRunner.cs (headless runner)
 - Packages/manifest.json (adds com.unity.ai.inference)
 - .gitignore (so its 410 MB Library/ isn't tracked)

 Re-runnable anytime:

 ```bash
   ~/Unity/Hub/Editor/6000.6.0f1/Editor/Unity -batchmode -nographics -quit \
     -projectPath PoseValidation -executeMethod PoseValidationRunner.Run -logFile /tmp/pv.log
 ```

 Suggested next step (Phase 1 prep)

 The remaining unknown that headless can't cover is the webcam + live visual path. Next I'd build a small interactive test in this same PoseValidation project: WebCamTexture on a RawImage
 ("see myself") + live MoveNet + wrist dots drawn on top — which also lets us validate the int32-NHWC preprocessing from a live frame and the GPU backend on your machine, before we touch the
 game.

 Want me to build that interactive webcam test scene next?

---

## Interactive webcam test (built)

Added a live, in-editor validation scene in the same PoseValidation project:
- `Assets/Scripts/WebcamPoseTest.cs` - self-contained MonoBehaviour: builds its own
  UI at runtime (black bg + square RawImage "see myself" + 17 keypoint dots +
  skeleton bones + info text). Runs MoveNet every frame.
- `Assets/Scenes/WebcamPoseTest.unity` - one GameObject with the script + a camera.
- Added `com.unity.ugui` 2.0.0 to the manifest (UnityEngine.UI wasn't present).

What it validates on the real machine:
1. int32-NHWC preprocessing from a *live* WebCamTexture frame.
2. GPU compute backend (with try/catch CPU fallback).
3. Keypoints line up visually with the webcam.

Design note - alignment invariant: the model input and the displayed image are
kept in the SAME orientation. The webcam is stretched to a square for both (so the
model's squished 192x192 input matches the square display), and the same
mirror/flip flags are applied to both the RawImage (via uvRect) and the sampler.
So MoveNet's normalized (y,x) output is drawn straight onto the square with no
extra transform.

How to run: open `PoseValidation/Assets/Scenes/WebcamPoseTest.unity` and press
Play. Live tweaks: **M** = toggle mirror, **V** = toggle vertical flip. Defaults:
mirror on, flipV off. If the image is upside-down or mirrored wrong, toggle and
note the values.

Compile-verified headless (exit 0).

### Linux camera gotcha: Intel IPU6 (v4l2loopback) breaks WebCamTexture

On the dev laptop, Unity's WebCamTexture fails with **"device doesn't support
mmapped buffers"**. Root cause: the built-in camera is an **Intel IPU6 MIPI
camera**, not a UVC webcam. It's exposed as:
- `/dev/video0` = `card='Intel MIPI Camera' driver='v4l2 loopback'` (a v4l2loopback
  fed by a userspace daemon) - the only node the user can access.
- `/dev/video1..32` = raw "Intel IPU6 ISYS Capture" nodes (permission-denied).

v4l2loopback devices don't support mmap streaming, but Unity's WebCamTexture forces
mmap -> hard failure. ffmpeg/GStreamer work because they negotiate read()/USERPTR.

**Fix:** WebcamPoseTest.cs now has two `captureMode`s:
- `WebCamTexture` - normal path (Windows/macOS, UVC cams).
- `ExternalFfmpeg` (default in the scene) - spawns `ffmpeg -f v4l2 -i /dev/video0
  ... -pix_fmt rgb24 -f rawvideo pipe:1`, a background thread reads raw RGB24
  frames off stdout into a Texture2D, feeding the identical MoveNet pipeline.
  ffmpeg delivers top-row-first frames, so `flipVertical` defaults ON for this path.

Note: this ffmpeg fallback is a Linux-dev convenience. The game targets Windows
(CUDA/DirectML/TensorRT/onnxruntime DLLs in repo root), where WebCamTexture works
natively - so production can use captureMode=WebCamTexture.

## Latest fix

Two ways to run once the USB cam is connected

 - Keep captureMode = ExternalFfmpeg (current default) — most robust on Linux; ffmpeg handles the cam regardless of mmap quirks.
 - Or switch to captureMode = WebCamTexture in the Inspector — a real UVC cam supports mmap, so Unity's native path should work too. Worth a quick try since it's lower-overhead.

 What to check in the Console after Play

 ```
   [WebcamPoseTest] candidate /dev/videoN name='<Your USB Cam>' preferred=True
   [WebcamPoseTest] using capture device: /dev/videoN
   [WebcamPoseTest] first ffmpeg frame received (921600 bytes), avg brightness ~NNN/255
 ```

 - If brightness is ~0, the feed is black (cam covered / wrong node) — tell me and I'll adjust.
 - If you see a normal brightness and yourself on screen, tweak M/V for correct orientation and report the FPS/backend line.

Plan

 1. Plug in the USB webcam, then re-run that same command. You should see a new line like /dev/video33 : HD Webcam ... readable=yes.
 2. Press Play. Auto-detect will prefer that USB node (its name won't contain MIPI/IPU6/ISYS), and you should get a live feed + skeleton.
 3. Check the Console for:
     - [WebcamPoseTest] using capture device: /dev/videoN
     - first ffmpeg frame received … avg brightness ~NNN/255 (a non-zero brightness = real image).

 If auto somehow still picks the internal cam, just paste me the device-list output and I'll give you the exact node to type into the ffmpegDevice field (replacing auto).

 One note: a UVC USB cam supports mmap, so you can also try flipping captureMode to WebCamTexture in the Inspector — that's the lighter, native path and it's what production (Windows) will
 use. Either mode should work with real USB hardware.
