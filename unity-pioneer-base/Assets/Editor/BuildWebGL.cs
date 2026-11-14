// Command-line WebGL build: Unity.exe -batchmode -quit -projectPath <p> -executeMethod BuildWebGL.Build
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildWebGL
{
    public static void Build()
    {
        PlayerSettings.productName = "Pioneer Base";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:PioneerMin";
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Base.unity" },
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("WebGL build result: " + report.summary.result + ", errors: " + report.summary.totalErrors);
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
