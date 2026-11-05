#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BeachJourneySetupEditor
{
    [MenuItem("Tools/Animal Setup/Create Beach Journey")]
    private static void CreateJourney()
    {
        const string path = "Assets/Scenes/GroundPrototypeLayers/cameraTravel_beach.anim";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Main Camera.controller");
        var previous = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Scenes/GroundPrototypeLayers/cameraTravel_deepForest.anim");
        var terrain = Terrain.activeTerrain;
        if (controller == null || previous == null || terrain == null) { Debug.LogError("Beach journey setup requires camera controller, deep forest clip, and active terrain."); return; }
        var bindings = AnimationUtility.GetCurveBindings(previous);
        float EndValue(string property) {
            foreach (var b in bindings) if (b.propertyName == property) return AnimationUtility.GetEditorCurve(previous, b).Evaluate(previous.length);
            throw new System.Exception("Missing deep forest camera curve: " + property);
        }
        Vector3 start = new Vector3(EndValue("m_LocalPosition.x"), EndValue("m_LocalPosition.y"), EndValue("m_LocalPosition.z"));
        Vector3 startEuler = new Vector3(EndValue("localEulerAnglesRaw.x"), EndValue("localEulerAnglesRaw.y"), EndValue("localEulerAnglesRaw.z"));
        Vector3 Ground(float x, float z) { var p = new Vector3(x, 0, z); return new Vector3(x, terrain.SampleHeight(p) + terrain.transform.position.y + 2.1f, z); }
        var points = new[] { start, Ground(128f, 177f), Ground(136f, 151f), Ground(145f, 129f) };
        var angles = new[] { startEuler, new Vector3(7f, 192f, 0f), new Vector3(6f, 180f, 0f), new Vector3(5f, 180f, 0f) };
        var times = new[] { 0f, 1.8f, 4.4f, 6.5f };
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip { name = "cameraTravel_beach", frameRate = 60f }; AssetDatabase.CreateAsset(clip, path); }
        void Curve(string property, System.Func<int,float> value) {
            var keys = new Keyframe[times.Length];
            for (int i = 0; i < keys.Length; i++) {
                float slope = i == 0 || i == keys.Length - 1 ? 0 : (value(i + 1) - value(i - 1)) / (times[i + 1] - times[i - 1]);
                keys[i] = new Keyframe(times[i], value(i), slope, slope);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), property), new AnimationCurve(keys));
        }
        Curve("m_LocalPosition.x", i => points[i].x); Curve("m_LocalPosition.y", i => points[i].y); Curve("m_LocalPosition.z", i => points[i].z);
        Curve("localEulerAnglesRaw.x", i => angles[i].x); Curve("localEulerAnglesRaw.y", i => angles[i].y); Curve("localEulerAnglesRaw.z", i => angles[i].z);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
        var machine = controller.layers[0].stateMachine;
        AnimatorState state = null; foreach (var child in machine.states) if (child.state.name == "cameraTravel_beach") state = child.state;
        if (state == null) state = machine.AddState("cameraTravel_beach", new Vector3(460, 140, 0));
        state.motion = clip; EditorUtility.SetDirty(state); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller); EditorUtility.SetDirty(clip); AssetDatabase.SaveAssets();
        Debug.Log($"Beach journey created: start {start}, end {points[3]}, duration {clip.length}, start rotation {startEuler}.");
    }
}
#endif
