using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class LowEndDiagnosticExport
{
    private const string FlutterProject = "/Volumes/ExternalSSD/flutter/kids-learning-app-diagnostics-20261105";
    private const string ExportFolder = "Builds/Diagnostics/POCO_C61_LiveTests_20261105/UnityExport";
    private const string PreserveLibraryFolder = "unityLibrary-low-baseline-20261105";
    private const string StatusFile = "/Users/avneesh/Documents/ChatGPT/unity/diagnostics/poco-c61-20261105/low-end-export-status.json";

    [MenuItem("Flutter/Export Low-End Diagnostic %&L", false, 103)]
    private static void Export()
    {
        var target = BuildTarget.Android;
        var oldTarget = EditorUserBuildSettings.activeBuildTarget;
        var oldDefaultApis = PlayerSettings.GetUseDefaultGraphicsAPIs(target);
        var oldApis = PlayerSettings.GetGraphicsAPIs(target);
        var oldArchitectures = PlayerSettings.Android.targetArchitectures;
        var oldExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        var oldAndroidBuildSystem = EditorUserBuildSettings.androidBuildSystem;
        var namedTarget = NamedBuildTarget.Android;
        var oldCompiler = PlayerSettings.GetIl2CppCompilerConfiguration(namedTarget);
        var destination = Path.Combine(FlutterProject, "android", "unityLibrary");
        var preserved = Path.Combine(FlutterProject, "android", PreserveLibraryFolder);
        var movedOriginal = false;
        var started = DateTime.UtcNow;

        WriteStatus("started", "Exporting the low-end Unity profile.", null, null);
        try
        {
            if (!Directory.Exists(Path.Combine(FlutterProject, "android")) ||
                !File.Exists(Path.Combine(FlutterProject, "pubspec.yaml")))
                throw new DirectoryNotFoundException("The isolated diagnostic Flutter project was not found.");
            if (Directory.Exists(preserved))
                throw new IOException($"Refusing to overwrite the saved baseline at {preserved}.");

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, target))
                throw new InvalidOperationException("Unity could not switch to the Android build target.");

            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            PlayerSettings.SetIl2CppCompilerConfiguration(namedTarget, Il2CppCompilerConfiguration.Release);
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var exportPath = Path.Combine(projectRoot, ExportFolder);
            if (Directory.Exists(exportPath))
                throw new IOException($"Refusing to overwrite the existing Unity export at {exportPath}.");
            Directory.CreateDirectory(Path.GetDirectoryName(exportPath));

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are listed in Build Settings.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = exportPath,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Unity Android export failed: {report.summary.result}");

            var unityLibrary = Path.Combine(exportPath, "unityLibrary");
            if (!Directory.Exists(unityLibrary))
                throw new DirectoryNotFoundException($"Unity did not export unityLibrary at {unityLibrary}.");

            var shared = Path.Combine(exportPath, "shared");
            if (Directory.Exists(shared))
                CopyDirectory(shared, Path.Combine(unityLibrary, "shared"));

            ModifyUnityLibraryGradle(unityLibrary);
            var launcherResources = Path.Combine(exportPath, "launcher", "src", "main", "res");
            if (Directory.Exists(launcherResources))
                CopyDirectory(launcherResources, Path.Combine(unityLibrary, "src", "main", "res"));

            var stringsFile = Path.Combine(unityLibrary, "src", "main", "res", "values", "strings.xml");
            if (File.Exists(stringsFile))
            {
                var strings = File.ReadAllText(stringsFile);
                if (!strings.Contains("game_view_content_description"))
                    File.WriteAllText(stringsFile, strings.Replace("<resources>",
                        "<resources>\n  <string name=\"game_view_content_description\">Game view</string>"));
            }

            var appGradle = Path.Combine(FlutterProject, "android", "app", "build.gradle.kts");
            var appConfig = File.ReadAllText(appGradle)
                .Replace("com.example.kids_learning_app.diagnostic", "com.example.kids_learning_app.lowdiagnostic");
            File.WriteAllText(appGradle, appConfig);
            var manifest = Path.Combine(FlutterProject, "android", "app", "src", "main", "AndroidManifest.xml");
            var manifestText = File.ReadAllText(manifest)
                .Replace("Kids 3D Diagnostic GLES", "Kids 3D Diagnostic Low");
            File.WriteAllText(manifest, manifestText);

            Directory.Move(destination, preserved);
            movedOriginal = true;
            FileUtil.CopyFileOrDirectory(unityLibrary, destination);
            WriteStatus("succeeded", "Low-end Unity export deployed to the isolated Flutter diagnostic app.",
                report.summary.totalSize.ToString(), (DateTime.UtcNow - started).TotalSeconds.ToString("F1"));
            Debug.Log($"[LowEndDiagnosticExport] Exported to {destination}. Previous GLES baseline is preserved at {preserved}.");
        }
        catch (Exception exception)
        {
            if (movedOriginal && Directory.Exists(preserved))
            {
                if (Directory.Exists(destination))
                    Directory.Delete(destination, true);
                Directory.Move(preserved, destination);
            }
            WriteStatus("failed", exception.Message, null, (DateTime.UtcNow - started).TotalSeconds.ToString("F1"));
            Debug.LogException(exception);
        }
        finally
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExport;
            EditorUserBuildSettings.androidBuildSystem = oldAndroidBuildSystem;
            PlayerSettings.Android.targetArchitectures = oldArchitectures;
            PlayerSettings.SetIl2CppCompilerConfiguration(namedTarget, oldCompiler);
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, oldDefaultApis);
            if (!oldDefaultApis)
                PlayerSettings.SetGraphicsAPIs(target, oldApis);

            var oldGroup = BuildPipeline.GetBuildTargetGroup(oldTarget);
            if (oldTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(oldGroup, oldTarget);
        }
    }

    private static void ModifyUnityLibraryGradle(string artifactPath)
    {
        var buildFile = Path.Combine(artifactPath, "build.gradle");
        var buildText = File.ReadAllText(buildFile)
            .Replace("com.android.application", "com.android.library")
            .Replace("bundle {", "splits {")
            .Replace("enableSplit = false", "enable false")
            .Replace("enableSplit = true", "enable true")
            .Replace("implementation fileTree(dir: 'libs', include: ['*.jar'])",
                "implementation(name: 'unity-classes', ext:'jar')")
            .Replace(" + unityStreamingAssets.tokenize(', ')", "")
            .Replace("ndkPath \"", "// ndkPath \"");
        buildText = Regex.Replace(buildText, @"\.\./shared/", "./shared/");
        if (!buildText.Contains("namespace"))
            buildText = buildText.Replace("compileOptions {",
                "if (project.android.hasProperty(\"namespace\")) {\n        namespace 'com.unity3d.player'\n    }\n\n    compileOptions {");
        buildText = Regex.Replace(buildText, @"\n.*applicationId '.+'.*\n", "\n");
        File.WriteAllText(buildFile, buildText);

        var manifestFile = Path.Combine(artifactPath, "src", "main", "AndroidManifest.xml");
        var manifest = File.ReadAllText(manifestFile);
        manifest = Regex.Replace(manifest, @"<application .*>", "<application>");
        manifest = Regex.Replace(manifest, @"<activity.*>(\s|\S)+?</activity>", "", RegexOptions.Multiline);
        File.WriteAllText(manifestFile, manifest);

        var proguardFile = Path.Combine(artifactPath, "proguard-unity.txt");
        if (File.Exists(proguardFile))
        {
            var rules = File.ReadAllText(proguardFile).Replace("-ignorewarnings",
                "-keep class com.unity_kit.** { *; }\n-keep class com.unity3d.player.** { *; }\n-ignorewarnings");
            File.WriteAllText(proguardFile, rules);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(directory.Replace(source, destination));
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, file.Replace(source, destination), true);
    }

    [Serializable]
    private sealed class ExportStatus
    {
        public string state;
        public string message;
        public string unityExportBytes;
        public string elapsedSeconds;
    }

    private static void WriteStatus(string state, string message, string bytes, string seconds)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StatusFile));
        File.WriteAllText(StatusFile, JsonUtility.ToJson(new ExportStatus
        {
            state = state,
            message = message,
            unityExportBytes = bytes,
            elapsedSeconds = seconds
        }, true));
    }
}
