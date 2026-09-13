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
