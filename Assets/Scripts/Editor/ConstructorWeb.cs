using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila la versión web (WebGL) en la carpeta "docs" para publicarla con GitHub Pages.
public static class ConstructorWeb
{
    [MenuItem("Juguetes Rebeldes/Compilar versión web (docs)", priority = 30)]
    public static void CompilarWeb()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("No se pudo cambiar a la plataforma Web. ¿Está instalado Web Build Support?");
                return;
            }
        }

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.runInBackground = true;

        string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        string[] escenas = System.Array.ConvertAll(
            System.Array.FindAll(EditorBuildSettings.scenes, e => e.enabled), e => e.path);

        BuildPlayerOptions op = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = carpeta,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport r = BuildPipeline.BuildPlayer(op);
        if (r.summary.result == BuildResult.Succeeded)
        {
            File.WriteAllText(Path.Combine(carpeta, ".nojekyll"), "");
            Debug.Log("Versión web lista en docs/ (" + (r.summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB).");
        }
        else
            Debug.LogError("Falló la compilación web: " + r.summary.result + " (" + r.summary.totalErrors + " errores)");
    }
}
