using UnityEditor;
using UnityEngine;

// Reproducible command-line builds. Invoke via Unity in batch mode, e.g.:
//   Unity -quit -batchmode -nographics \
//         -projectPath <PoseValidation> \
//         -executeMethod BuildScript.BuildLinux \
//         -logFile -
//
// Output goes to Builds/<Platform>/ under the project root.
public static class BuildScript
{
    static readonly string[] Scenes = { "Assets/Scenes/WebcamPoseTest.unity" };

    [MenuItem("Build/Linux x64")]
    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, "Builds/Linux/PoseValidation.x86_64");
    }

    [MenuItem("Build/Windows x64")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/PoseValidation.exe");
    }

    static void Build(BuildTarget target, string outPath)
    {
        var opts = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outPath,
            target = target,
            options = BuildOptions.None, // add BuildOptions.Development for a dev build
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;
        Debug.Log($"[BuildScript] {target} -> {outPath}  result={summary.result}  " +
                  $"size={summary.totalSize} bytes  errors={summary.totalErrors}");

        if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
