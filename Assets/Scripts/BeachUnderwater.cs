using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Applies and restores underwater visibility separately for each rendered camera.</summary>
[ExecuteAlways]
public sealed class BeachUnderwater : MonoBehaviour
{
    [SerializeField] private Renderer waterSurface;
    [SerializeField] private Color waterFog = new Color(0.025f, 0.26f, 0.30f, 1f);
    [SerializeField, Min(0.001f)] private float fogDensity = 0.095f;
    private readonly Dictionary<Camera, ViewState> rendering = new Dictionary<Camera, ViewState>();

    private sealed class ViewState
    {
        public bool fog;
        public FogMode mode;
        public Color fogColor, background, ambient;
        public float density, start, end, far, ambientIntensity, sunIntensity;
        public AmbientMode ambientMode;
        public CameraClearFlags clear;
        public Light sun;
        public UniversalAdditionalCameraData data;
        public CameraOverrideOption depth;
    }

    public bool IsUnderwater => IsBelowSurface(Camera.main);

    private bool IsBelowSurface(Camera camera)
    {
        if (camera == null || waterSurface == null) return false;
        var area = waterSurface.bounds;
        var p = camera.transform.position;
        return p.x >= area.min.x && p.x <= area.max.x &&
               p.z >= area.min.z && p.z <= area.max.z &&
               p.y < waterSurface.transform.position.y - 0.04f;
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginView;
        RenderPipelineManager.endCameraRendering += EndView;
    }

    private void BeginView(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null || waterSurface == null || rendering.ContainsKey(camera)) return;
        bool below = IsBelowSurface(camera);
        var area = waterSurface.bounds;
        var p = camera.transform.position;
        bool nearSea = p.x >= area.min.x - 20f && p.x <= area.max.x + 20f &&
                       p.z >= area.min.z - 20f && p.z <= area.max.z + 20f;
        if (!below && !nearSea) return;
        var sun = RenderSettings.sun;
        if (sun == null)
            foreach (var light in FindObjectsByType<Light>())
                if (light.type == LightType.Directional) { sun = light; break; }
        var data = camera.GetComponent<UniversalAdditionalCameraData>();
        var saved = new ViewState
        {
            fog = RenderSettings.fog, mode = RenderSettings.fogMode,
            fogColor = RenderSettings.fogColor, density = RenderSettings.fogDensity,
            start = RenderSettings.fogStartDistance, end = RenderSettings.fogEndDistance,
            ambientMode = RenderSettings.ambientMode, ambient = RenderSettings.ambientLight,
            ambientIntensity = RenderSettings.ambientIntensity,
            clear = camera.clearFlags, background = camera.backgroundColor,
            far = camera.farClipPlane, sun = sun, sunIntensity = sun != null ? sun.intensity : 0f,
            data = data, depth = data != null ? data.requiresDepthOption : CameraOverrideOption.UsePipelineSettings
        };
        rendering.Add(camera, saved);
        // Shore blending also needs depth when looking at the sea from land.
        if (data != null) data.requiresDepthOption = CameraOverrideOption.On;
        if (!below) return;
        float depth = Mathf.Clamp01((waterSurface.transform.position.y - camera.transform.position.y - 1f) / 32f);
        var color = Color.Lerp(waterFog, new Color(0.018f, 0.105f, 0.15f), depth);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = color;
        RenderSettings.fogDensity = Mathf.Lerp(fogDensity, 0.085f, depth);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(new Color(0.65f, 0.78f, 0.80f), new Color(0.52f, 0.67f, 0.74f), depth);
        if (sun != null) sun.intensity = saved.sunIntensity * Mathf.Lerp(1.15f, 1.0f, depth);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = color;
        // At these distances the exponential fog has already hidden geometry.
        camera.farClipPlane = Mathf.Min(saved.far, Mathf.Lerp(65f, 55f, depth));
        if (data != null) data.requiresDepthOption = CameraOverrideOption.On;
    }

    private void EndView(ScriptableRenderContext context, Camera camera)
    {
        if (camera == null || !rendering.TryGetValue(camera, out var saved)) return;
        RenderSettings.fog = saved.fog;
        RenderSettings.fogMode = saved.mode;
        RenderSettings.fogColor = saved.fogColor;
        RenderSettings.fogDensity = saved.density;
        RenderSettings.fogStartDistance = saved.start;
        RenderSettings.fogEndDistance = saved.end;
        RenderSettings.ambientMode = saved.ambientMode;
        RenderSettings.ambientLight = saved.ambient;
        RenderSettings.ambientIntensity = saved.ambientIntensity;
        if (saved.sun != null) saved.sun.intensity = saved.sunIntensity;
        camera.clearFlags = saved.clear;
        camera.backgroundColor = saved.background;
        camera.farClipPlane = saved.far;
        if (saved.data != null) saved.data.requiresDepthOption = saved.depth;
        rendering.Remove(camera);
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginView;
        RenderPipelineManager.endCameraRendering -= EndView;
        foreach (var camera in new List<Camera>(rendering.Keys)) EndView(default, camera);
        rendering.Clear();
    }
}
