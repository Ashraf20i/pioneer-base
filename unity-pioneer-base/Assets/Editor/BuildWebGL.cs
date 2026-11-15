// Command-line tools.
//   WebGL build : Unity.exe -batchmode -nographics -quit? no: -executeMethod BuildWebGL.Build
//   Screenshots : Unity.exe -batchmode -executeMethod BuildWebGL.Shots -pbshots   (needs graphics, so no -nographics)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildWebGL
{
    static readonly string[] Shaders = {
        "Legacy Shaders/Diffuse", "Legacy Shaders/Transparent/Diffuse", "Unlit/Color",
        "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended", "Sprites/Default" };

    // The game creates its materials from code, so the shaders must be forced into the build,
    // and the fog variants must be kept (fog is switched on at runtime).
    public static void EnsureGraphics()
    {
        var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        foreach (var name in Shaders)
        {
            var sh = Shader.Find(name);
            if (sh == null) { Debug.LogWarning("Shader not found: " + name); continue; }
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) has = true;
            if (!has) { arr.InsertArrayElementAtIndex(arr.arraySize); arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh; }
        }
        so.FindProperty("m_FogStripping").intValue = 1;
        so.FindProperty("m_FogKeepLinear").boolValue = true;
        so.FindProperty("m_FogKeepExp").boolValue = true;
        so.FindProperty("m_FogKeepExp2").boolValue = true;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        EnsureGraphics();
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

    // Opens the scene and enters play mode; the game sees -pbshots, plays by itself and saves screenshots.
    public static void Shots()
    {
        EnsureGraphics();
        EditorSceneManager.OpenScene("Assets/Scenes/Base.unity");
        EditorApplication.EnterPlaymode();
    }
}
