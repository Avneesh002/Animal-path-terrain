using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

internal static class AnimalMenuFlutterExport
{
    [MenuItem("Flutter/Export Animal Menu to Main App")]
    private static void Export()
    {
        const string flutter = "/Volumes/ExternalSSD/flutter/kids-learning-app";
        const string outDir = "Builds/AnimalMenuExport";
        var destination = Path.Combine(flutter, "android", "unityLibrary");
        var backup = Path.Combine(flutter, "android", "unityLibrary-before-animal-menu-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        var target = BuildTarget.Android;
        var oldTarget = EditorUserBuildSettings.activeBuildTarget;
        var oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        var oldSystem = EditorUserBuildSettings.androidBuildSystem;
        var oldArchitectures = PlayerSettings.Android.targetArchitectures;
        try
        {
            if (!File.Exists(Path.Combine(flutter, "pubspec.yaml"))) throw new DirectoryNotFoundException(flutter);
            if (Directory.Exists(backup)) throw new IOException("Backup already exists: " + backup);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, target)) throw new Exception("Could not switch to Android target.");
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var output = Path.Combine(root, outDir);
            if (Directory.Exists(output)) Directory.Delete(output, true);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, target = target, locationPathName = output, options = BuildOptions.AcceptExternalModificationsToPlayer });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            var exported = Path.Combine(output, "unityLibrary");
            if (!Directory.Exists(exported)) throw new DirectoryNotFoundException(exported);
            Directory.Move(destination, backup);
            try { FileUtil.CopyFileOrDirectory(exported, destination); }
            catch { if (Directory.Exists(destination)) Directory.Delete(destination, true); Directory.Move(backup, destination); throw; }
            Debug.Log("Animal menu Unity library exported to " + destination + "; previous library preserved at " + backup);
        }
        catch (Exception e) { Debug.LogException(e); throw; }
        finally
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
            EditorUserBuildSettings.androidBuildSystem = oldSystem;
            PlayerSettings.Android.targetArchitectures = oldArchitectures;
            if (oldTarget != EditorUserBuildSettings.activeBuildTarget) EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(oldTarget), oldTarget);
        }
    }
}
