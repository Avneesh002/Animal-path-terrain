using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Returns the journey camera to its starting view after visiting the animals.</summary>
public sealed class ForestReturnToGate : MonoBehaviour
{
    private Camera journeyCamera;
    private Animator journeyAnimator;
    private ForestGateClickOpener gate;
    private AnimalTapHandler animalHandler;
    private UnityEngine.UI.Button button;
    private UnityEngine.UI.Button forwardButton;
    private bool returning;
    private int lastNavigationFrame = -1;
    public bool CanFocusAnimal => !returning && gate != null && gate.CameraTravelStarted &&
        journeyAnimator != null && journeyAnimator.enabled &&
        !journeyAnimator.IsInTransition(0) &&
        journeyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var camera = Camera.main;
        var gate = Object.FindFirstObjectByType<ForestGateClickOpener>();
        if (camera == null || gate == null || camera.GetComponent<ForestReturnToGate>() != null)
            return;
        camera.gameObject.AddComponent<ForestReturnToGate>();
    }

    private void Start()
    {
        journeyCamera = GetComponent<Camera>();
        journeyAnimator = GetComponent<Animator>();
        gate = Object.FindFirstObjectByType<ForestGateClickOpener>();
        animalHandler = GetComponent<AnimalTapHandler>();
        button = CreateButton(false);
        forwardButton = CreateButton(true);
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(BeginReturn);
            button.onClick.AddListener(BeginReturn);
        }
        if (forwardButton != null)
        {
            forwardButton.onClick.RemoveListener(BeginForwardJourney);
            forwardButton.onClick.AddListener(BeginForwardJourney);
        }
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(BeginReturn);
        if (forwardButton != null) forwardButton.onClick.RemoveListener(BeginForwardJourney);
    }

    private UnityEngine.UI.Button CreateButton(bool forward)
    {
        var texture = Resources.Load<Texture2D>("ForestNavigationClean");
        if (texture == null) { Debug.LogError("Return-to-gate button image is missing."); return null; }
        var canvasObject = new GameObject(forward ? "ForwardJourneyCanvas" : "ReturnToGateCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        var obj = new GameObject(forward ? "ForwardJourneyButton" : "BackToGateButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        obj.transform.SetParent(canvasObject.transform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(180f, 180f);
        rect.anchoredPosition = new Vector2(forward ? 105f : -105f, 35f);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.sprite = Sprite.Create(texture, new Rect(forward ? texture.width / 2f : 0f, 0f, texture.width / 2f, texture.height), new Vector2(0.5f, 0.5f));
        image.preserveAspect = true;
        image.color = new Color(0.65f, 0.65f, 0.65f, 1f);
        var result = obj.GetComponent<UnityEngine.UI.Button>();
        if (forward) result.onClick.AddListener(BeginForwardJourney);
        else result.onClick.AddListener(BeginReturn);
        obj.SetActive(false);
        return result;
    }

    private void Update()
    {
        if (button == null || forwardButton == null || gate == null || journeyAnimator == null) return;
        if (animalHandler == null) animalHandler = GetComponent<AnimalTapHandler>();
        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        bool canNavigate = !returning && gate.CameraTravelStarted && journeyAnimator.enabled &&
                           (animalHandler == null || !animalHandler.HasSavedView);
        bool atAnimals = canNavigate && state.IsName("CameraTravel") && state.normalizedTime >= 1f;
        bool atDeepForest = canNavigate && state.IsName("cameraTravel_deepForest") && state.normalizedTime >= 1f;
        bool atBeach = canNavigate && state.IsName("cameraTravel_beach") && state.normalizedTime >= 1f;
        bool atShallowSea = canNavigate && state.IsName("cameraTravel_shallowSea") && state.normalizedTime >= 1f;
        bool atDeepSea = canNavigate && state.IsName("cameraTravel_deepSea") && state.normalizedTime >= 1f;
        bool showForward = atAnimals || atDeepForest || atBeach || atShallowSea;
        if (forwardButton.gameObject.activeSelf != showForward) forwardButton.gameObject.SetActive(showForward);
        bool showBack = atAnimals || atDeepForest || atBeach || atShallowSea || atDeepSea;
        if (button.gameObject.activeSelf != showBack) button.gameObject.SetActive(showBack);
    }

    private void BeginForwardJourney()
    {
        if (lastNavigationFrame == Time.frameCount || returning || gate == null || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;
        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        if (!gate.CameraTravelStarted || state.normalizedTime < 1f) return;
        string next = state.IsName("CameraTravel") ? "cameraTravel_deepForest" :
                      state.IsName("cameraTravel_deepForest") ? "cameraTravel_beach" :
                      state.IsName("cameraTravel_beach") ? "cameraTravel_shallowSea" :
                      state.IsName("cameraTravel_shallowSea") ? "cameraTravel_deepSea" : null;
        if (next == null) return;
        if (!journeyAnimator.HasState(0, Animator.StringToHash(next)))
        {
            Debug.LogError($"The {next} travel animation is missing from the camera Animator.", this);
            return;
        }
        lastNavigationFrame = Time.frameCount;
        forwardButton.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
        journeyAnimator.speed = 1f;
        journeyAnimator.Play(next, 0, 0f);
        journeyAnimator.Update(0f);
    }

    private void BeginReturn()
    {
        if (lastNavigationFrame == Time.frameCount || returning || gate == null || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;

        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime < 1f) return;
        lastNavigationFrame = Time.frameCount;
        if (state.IsName("cameraTravel_deepSea") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_deepSea") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The deep sea travel clip is missing; cannot return to shallow water.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_shallowSea"));
        }
        else if (state.IsName("cameraTravel_shallowSea") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_shallowSea") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The shallow sea travel clip is missing; cannot return to the beach.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_beach"));
        }
        else if (state.IsName("cameraTravel_beach") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_beach") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The beach travel clip is missing; cannot return to deep forest.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_deepForest"));
        }
        else if (state.IsName("cameraTravel_deepForest") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_deepForest") { clip = candidate; break; }
            if (clip == null)
            {
                Debug.LogError("The deep forest travel clip is missing; cannot return to the animal stop.", this);
                return;
            }
            StartCoroutine(ReturnAlongClip(clip, "CameraTravel"));
        }
        else if (state.IsName("CameraTravel") && state.normalizedTime >= 1f)
        {
            StartCoroutine(ReturnToGate());
        }
    }

    private IEnumerator ReturnAlongClip(AnimationClip clip, string previousState)
    {
        returning = true;
        button.gameObject.SetActive(false);
        forwardButton.gameObject.SetActive(false);
        if (animalHandler != null) animalHandler.enabled = false;
        gate.SetJourneyReturning(true);
        journeyAnimator.enabled = false;

        float elapsed = 0f;
        while (elapsed < clip.length)
        {
            elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, clip.length);
            clip.SampleAnimation(gameObject, clip.length - elapsed);
            yield return null;
        }
        clip.SampleAnimation(gameObject, 0f);
        journeyAnimator.enabled = true;
        journeyAnimator.speed = 1f;
        journeyAnimator.Play(previousState, 0, 1f);
        journeyAnimator.Update(0f);
        gate.SetJourneyReturning(false);
        if (animalHandler != null) animalHandler.enabled = true;
        returning = false;
    }

    private IEnumerator ReturnToGate()
    {
        returning = true;
        button.gameObject.SetActive(false);
        forwardButton.gameObject.SetActive(false);
        if (animalHandler != null) animalHandler.enabled = false;
        gate.SetJourneyReturning(true);
        journeyAnimator.enabled = false;
        // Evaluate the same Animator state backwards at explicit times. This
        // preserves the authored path and works with player animation bindings.
        // Keep speed zero so normal Animator updates cannot advance the route.
        journeyAnimator.enabled = true;
        journeyAnimator.speed = 0f;
        const float duration = 4f;
        float elapsed = 0f;
        journeyAnimator.Play("CameraTravel", 0, 1f);
        journeyAnimator.Update(0f);
        while (elapsed < duration)
        {
            yield return null;
            elapsed = Mathf.Min(elapsed + Time.unscaledDeltaTime, duration);
            journeyAnimator.Play("CameraTravel", 0, 1f - elapsed / duration);
            journeyAnimator.Update(0f);
        }
        journeyAnimator.Play("CameraTravel", 0, 0f);
        journeyAnimator.Update(0f);
        journeyAnimator.enabled = false;
        journeyAnimator.speed = 1f;
        yield return gate.CloseAndResetAfterReturn();
        if (animalHandler != null) animalHandler.enabled = true;
        returning = false;
    }
}
