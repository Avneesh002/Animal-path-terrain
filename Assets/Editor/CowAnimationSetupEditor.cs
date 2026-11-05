#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Replaces the cow in the active forest scene with the supplied animated model.</summary>
public static class CowAnimationSetupEditor
{
    private const string ModelPath = "Assets/Animals/Cow/cow_update.glb";
    private const string ControllerPath = "Assets/Animals/Cow/Cow_Focus_Animations.controller";
    private const string IdleState = "CowIdle";
    private const string FocusState = "CowZoom";

    [MenuItem("Tools/Animal Setup/Use Animated Cow in Active Scene")]
    private static void UseAnimatedCow()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open the forest scene before running the cow setup.");
            return;
        }

        AnimalTapTarget oldTarget = Object.FindObjectsByType<AnimalTapTarget>(FindObjectsInactive.Include)
            .FirstOrDefault(target => target.AnimalId == "cow" &&
                                      target.gameObject.scene == scene);
        if (oldTarget == null)
        {
            Debug.LogError("No AnimalTapTarget with Animal ID 'cow' was found in the active scene.");
            return;
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            Debug.LogError($"Could not load the imported cow model at {ModelPath}.");
            return;
        }

        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => clip != null && clip.length > 0f)
            .GroupBy(clip => clip.name)
            .Select(group => group.First())
            .ToArray();
        if (clips.Length < 2)
        {
            clips = AnimationUtility.GetAnimationClips(model)
                .Where(clip => clip != null && clip.length > 0f)
                .GroupBy(clip => clip.name)
                .Select(group => group.First())
                .ToArray();
        }
        if (clips.Length < 2)
        {
            Debug.LogError($"Expected two animation clips in {ModelPath}, found {clips.Length}.");
            return;
        }

        Debug.Log("Cow model animation clips: " +
                  string.Join(", ", clips.Select(clip => $"{clip.name} ({clip.length:0.##} s)")));

        // The supplied GLB's skeletal clip is GLTF_created_0; the other track
        // animates mesh weights and is named after its mesh.
        AnimationClip idleClip = clips.FirstOrDefault(clip =>
            clip.name.IndexOf("GLTF_created_0", System.StringComparison.OrdinalIgnoreCase) >= 0)
            ?? clips[0];
        AnimationClip focusClip = clips.FirstOrDefault(clip =>
            clip != idleClip && clip.name.IndexOf("mesh_", System.StringComparison.OrdinalIgnoreCase) >= 0)
            ?? clips.First(clip => clip != idleClip);
        EnableLooping(idleClip);
        EnableLooping(focusClip);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState(IdleState, new Vector3(250f, 80f));
        AnimatorState focusState = stateMachine.AddState(FocusState, new Vector3(500f, 80f));
        idleState.motion = idleClip;
        focusState.motion = focusClip;
        stateMachine.defaultState = idleState;

        Transform oldTransform = oldTarget.transform;
        Transform oldParent = oldTransform.parent;
        Vector3 localPosition = oldTransform.localPosition;
        Quaternion localRotation = oldTransform.localRotation;
        Vector3 previousOffset = oldTarget.CloseUpOffset;

        GameObject instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Unity could not create an instance of the animated cow model.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Replace cow with animated model");
        instance.name = "cow";
        instance.transform.SetParent(oldParent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = Vector3.one;

        // This GLB is authored at approximately real-world size. The old cow needed
        // a large scene scale, but applying that scale to this model made it oversized.
        instance.transform.localScale = Vector3.one;
        Bounds bounds = GetRendererBounds(instance);

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null)
            animator = Undo.AddComponent<Animator>(instance);
        Undo.RecordObject(animator, "Assign cow animation controller");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        Transform focusPoint = instance.transform.Find("AnimalFocusPoint");
        if (focusPoint == null)
        {
            GameObject focusObject = new GameObject("AnimalFocusPoint");
            Undo.RegisterCreatedObjectUndo(focusObject, "Create cow focus point");
            focusPoint = focusObject.transform;
            focusPoint.SetParent(instance.transform, true);
        }
        focusPoint.position = bounds.center;

        AnimalTapTarget newTarget = instance.GetComponent<AnimalTapTarget>();
        if (newTarget == null)
            newTarget = Undo.AddComponent<AnimalTapTarget>(instance);
        Undo.RecordObject(newTarget, "Configure cow tap target");
        var targetData = new SerializedObject(newTarget);
        targetData.FindProperty("animalId").stringValue = "cow";
        targetData.FindProperty("focusPoint").objectReferenceValue = focusPoint;
        float cameraDistance = Mathf.Max(3f, Mathf.Max(bounds.size.x, bounds.size.z) * 1.6f);
        targetData.FindProperty("closeUpOffset").vector3Value =
            new Vector3(previousOffset.x, 0.25f, cameraDistance);
        targetData.ApplyModifiedProperties();
        newTarget.ConfigureFocusAnimation(animator, IdleState, FocusState);

        BoxCollider collider = instance.GetComponent<BoxCollider>();
        if (collider == null)
            collider = Undo.AddComponent<BoxCollider>(instance);
        SetLocalColliderFromBounds(collider, instance.transform, bounds);

        Undo.DestroyObjectImmediate(oldTarget.gameObject);
        EditorUtility.SetDirty(instance);
        EditorUtility.SetDirty(newTarget);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"Cow in '{scene.name}' now uses '{model.name}'. Default: {idleClip.name}; zoom: {focusClip.name}. " +
                  $"World size: {bounds.size.x:0.##} × {bounds.size.y:0.##} × {bounds.size.z:0.##} m.");
    }

    private static void EnableLooping(AnimationClip clip)
    {
        try
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Could not enable looping for '{clip.name}': {exception.Message}");
        }
    }

    private static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException($"No renderers found on {root.name}.");

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void SetLocalColliderFromBounds(BoxCollider collider, Transform root, Bounds worldBounds)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        for (int x = 0; x <= 1; x++)
        for (int y = 0; y <= 1; y++)
        for (int z = 0; z <= 1; z++)
        {
            Vector3 corner = worldBounds.min + Vector3.Scale(worldBounds.size, new Vector3(x, y, z));
            Vector3 local = root.InverseTransformPoint(corner);
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        collider.center = (min + max) * 0.5f;
        Vector3 size = (max - min) * 1.1f;
        collider.size = new Vector3(Mathf.Max(size.x, 0.1f),
                                    Mathf.Max(size.y, 0.1f),
                                    Mathf.Max(size.z, 0.1f));
        collider.isTrigger = false;
    }
}
#endif
