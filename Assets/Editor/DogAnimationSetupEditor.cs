#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DogAnimationSetupEditor
{
    [MenuItem("Tools/Animal Setup/Fix Dog Tap Collider")]
    public static void FixCollider()
    {
        var scene = SceneManager.GetActiveScene();
        var dog = Object.FindObjectsByType<AnimalTapTarget>(FindObjectsInactive.Include)
            .FirstOrDefault(x => x.gameObject.scene == scene && x.AnimalId == "dog");
        if (dog == null) { Debug.LogError("Dog tap fix: dog not found."); return; }
        var renderers = dog.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0) { Debug.LogError("Dog tap fix: no skinned mesh found."); return; }
        Bounds world = renderers[0].bounds;
        foreach (var renderer in renderers) world.Encapsulate(renderer.bounds);
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (var renderer in renderers)
        {
            var bounds = renderer.localBounds;
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
            {
                var corner = bounds.min + Vector3.Scale(bounds.size, new Vector3(x, y, z));
                var local = dog.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                min = Vector3.Min(min, local); max = Vector3.Max(max, local);
            }
        }
        var collider = dog.GetComponent<BoxCollider>();
        if (collider == null) collider = Undo.AddComponent<BoxCollider>(dog.gameObject);
        Undo.RecordObject(collider, "Fix dog tap collider");
        collider.center = (min + max) * 0.5f;
        collider.size = (max - min) * 1.15f;
        collider.enabled = true;
        EditorUtility.SetDirty(collider);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Dog tap collider updated: local center {collider.center}, size {collider.size}; visible bounds {world.size}.");
    }

    [MenuItem("Tools/Animal Setup/Configure Dog Animations")]
    public static void Configure()
    {
        var scene = SceneManager.GetActiveScene();
        var dog = Object.FindObjectsByType<AnimalTapTarget>(FindObjectsInactive.Include)
            .FirstOrDefault(x => x.gameObject.scene == scene && x.AnimalId == "dog");
        var idle = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animals/Dog/ideal_controller.controller");
        var sound = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animals/Dog/sound_controller.controller");
        if (dog == null || idle == null || sound == null) { Debug.LogError("Dog animation setup: dog or controller not found."); return; }
        var animator = dog.GetComponentInChildren<Animator>(true);
        var idleMachine = idle.layers[0].stateMachine;
        var idleState = idleMachine.defaultState;
        var soundState = sound.layers[0].stateMachine.defaultState;
        if (animator == null || idleState == null || idleState.motion == null || soundState == null || soundState.motion == null)
        { Debug.LogError("Dog animation setup: Animator or animation clip missing."); return; }
        Undo.RecordObject(idleState, "Configure dog idle");
        Undo.RecordObject(idleMachine, "Configure dog animations");
        idleState.name = "DogIdle";
        var zoom = idleMachine.states.Select(x => x.state).FirstOrDefault(x => x.name == "DogZoom");
        if (zoom == null) zoom = idleMachine.AddState("DogZoom", new Vector3(500, 100, 0));
        Undo.RecordObject(zoom, "Configure dog zoom animation");
        zoom.motion = soundState.motion;
        idleMachine.defaultState = idleState;
        Undo.RecordObject(animator, "Assign dog animation controller");
        animator.runtimeAnimatorController = idle;
        Undo.RecordObject(dog, "Configure dog focus animations");
        dog.ConfigureFocusAnimation(animator, "DogIdle", "DogZoom");
        EditorUtility.SetDirty(idleState); EditorUtility.SetDirty(zoom); EditorUtility.SetDirty(idleMachine);
        EditorUtility.SetDirty(idle); EditorUtility.SetDirty(animator); EditorUtility.SetDirty(dog);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Dog animation setup complete: DogIdle default, DogZoom on focus, return to idle on back.");
    }
}
#endif
