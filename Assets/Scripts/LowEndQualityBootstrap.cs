using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace KidsWorld.Performance
{
    /// <summary>Selects a conservative rendering tier on Android devices with 4 GB RAM or less.</summary>
    internal static class LowEndQualityBootstrap
    {
        private const string LowQualityName = "Mobile Low";
        private static bool useLowTier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void SelectQualityTier()
        {
            if (Application.platform != RuntimePlatform.Android || SystemInfo.systemMemorySize > 4096)
                return;

            var names = QualitySettings.names;
            var lowIndex = Array.IndexOf(names, LowQualityName);
            if (lowIndex < 0)
            {
                Debug.LogWarning($"Low-end quality tier '{LowQualityName}' was not found. Keeping the default tier.");
                return;
            }

            QualitySettings.SetQualityLevel(lowIndex, true);
            useLowTier = true;
            Debug.Log($"Selected {LowQualityName} for Android device with {SystemInfo.systemMemorySize} MB RAM.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void DisablePostProcessingOnLowTier()
        {
            if (!useLowTier)
                return;

            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(
                         FindObjectsInactive.Include))
            {
                var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                    cameraData.renderPostProcessing = false;
            }

            Debug.Log("Disabled URP camera post-processing for Mobile Low.");
        }
    }
}
