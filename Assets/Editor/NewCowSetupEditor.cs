#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NewCowSetupEditor
{
    [MenuItem("Tools/Animal Setup/Configure New Cow")]
    static void Configure()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying) { Debug.LogError("Stop Play mode before configuring the cow."); return; }
        var cow = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "cow3");
        var idle = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animals/Cow/ideal_controller.controller");
        var sound = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animals/Cow/sound_Controller.controller");
        var animator = cow != null ? cow.GetComponentInChildren<Animator>(true) : null;
        var idleState = idle != null ? idle.layers[0].stateMachine.defaultState : null;
        var soundState = sound != null ? sound.layers[0].stateMachine.defaultState : null;
        var renderers = cow != null ? cow.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
        if (cow == null || animator == null || idleState == null || idleState.motion == null || soundState == null || soundState.motion == null || renderers.Length == 0)
        { Debug.LogError("Cow setup: cow3, animator, mesh, or one of the animation clips is missing."); return; }
        var machine = idle.layers[0].stateMachine;
        Undo.RecordObject(machine, "Set up cow animation states");
        Undo.RecordObject(idleState, "Name cow idle state");
        idleState.name = "CowIdle";
        var zoom = machine.states.Select(x => x.state).FirstOrDefault(x => x.name == "CowZoom");
        if (zoom == null) zoom = machine.AddState("CowZoom", new Vector3(500, 100, 0));
        Undo.RecordObject(zoom, "Set cow focus animation");
        zoom.motion = soundState.motion;
        machine.defaultState = idleState;
        Undo.RecordObject(animator, "Assign cow controller");
        animator.runtimeAnimatorController = idle;
        animator.applyRootMotion = false;
        var target = cow.GetComponent<AnimalTapTarget>();
        if (target == null) target = Undo.AddComponent<AnimalTapTarget>(cow);
        var collider = cow.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(cow);
        Vector3 min = Vector3.one * float.PositiveInfinity, max = Vector3.one * float.NegativeInfinity;
        foreach (var renderer in renderers)
        {
            var bounds = renderer.localBounds;
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
            {
                var point = bounds.min + Vector3.Scale(bounds.size, new Vector3(x, y, z));
                var local = cow.transform.InverseTransformPoint(renderer.transform.TransformPoint(point));
                min = Vector3.Min(min, local); max = Vector3.Max(max, local);
            }
        }
        Undo.RecordObject(collider, "Fit cow tap collider");
        collider.center = (min + max) * 0.5f;
        collider.size = (max - min) * 1.1f;
        collider.enabled = true; collider.isTrigger = false;
        var focus = cow.transform.Find("AnimalFocusPoint");
        if (focus == null) { var o = new GameObject("AnimalFocusPoint"); Undo.RegisterCreatedObjectUndo(o, "Create cow focus"); focus = o.transform; focus.SetParent(cow.transform, false); }
        focus.localPosition = collider.center;
        Undo.RecordObject(target, "Configure cow tap target");
        var data = new SerializedObject(target);
        data.FindProperty("animalId").stringValue = "cow";
        data.FindProperty("focusPoint").objectReferenceValue = focus;
        var worldSize = collider.bounds.size;
        data.FindProperty("closeUpOffset").vector3Value = new Vector3(0, 0.25f, Mathf.Max(4f, Mathf.Max(worldSize.x, worldSize.z) * 1.5f));
        data.ApplyModifiedProperties();
        target.ConfigureFocusAnimation(animator, "CowIdle", "CowZoom");
        foreach (var old in Object.FindObjectsByType<AnimalTapTarget>(FindObjectsInactive.Include))
            if (old != target && old.gameObject.scene == scene && old.AnimalId == "cow" && old.gameObject.activeSelf)
            { Undo.RecordObject(old.gameObject, "Disable old cow"); old.gameObject.SetActive(false); }
        EditorUtility.SetDirty(idleState); EditorUtility.SetDirty(zoom); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(idle);
        EditorUtility.SetDirty(animator); EditorUtility.SetDirty(target); EditorUtility.SetDirty(collider);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log($"New cow configured. Collider world size {worldSize}; idle {idleState.motion.name}; zoom {zoom.motion.name}.");
    }
}
#endif
