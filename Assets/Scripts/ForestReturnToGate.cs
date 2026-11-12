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
    private UnityEngine.UI.Button birdButton;
    private bool returning;
    private bool holdReturnedTree2View;
    private Vector3 returnedTree2Position;
    private Quaternion returnedTree2Rotation;
    private string beachReturnState = "cameraTravel_directBeach";
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
        birdButton = CreateBirdButton();
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
        var texture = Resources.Load<Texture2D>("ForestNavigationLight");
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 180f);
        rect.anchoredPosition = new Vector2(forward ? 105f : -105f, 125f);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        float cellWidth = texture.width / 3f;
        image.sprite = Sprite.Create(texture, new Rect(forward ? cellWidth : 0f, 0f, cellWidth, texture.height), new Vector2(0.5f, 0.5f));
        image.preserveAspect = true;
        var result = obj.GetComponent<UnityEngine.UI.Button>();
        if (forward) result.onClick.AddListener(BeginForwardJourney);
        else result.onClick.AddListener(BeginReturn);
        obj.SetActive(false);
        return result;
    }

    private UnityEngine.UI.Button CreateBirdButton()
    {
        var texture = Resources.Load<Texture2D>("ForestNavigationLight");
        if (texture == null) { Debug.LogError("Bird navigation icon is missing."); return null; }
        var canvasObject = new GameObject("BirdJourneyCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        var obj = new GameObject("BirdJourneyButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        obj.transform.SetParent(canvasObject.transform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(180f, 180f);
        rect.anchoredPosition = new Vector2(315f, 125f);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        float cellWidth = texture.width / 3f;
        image.sprite = Sprite.Create(texture, new Rect(cellWidth * 2f, 0f, cellWidth, texture.height), new Vector2(0.5f, 0.5f));
        image.preserveAspect = true;
        var result = obj.GetComponent<UnityEngine.UI.Button>();
        result.onClick.AddListener(BeginBirdJourney);
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
        bool atBirdTree = canNavigate && state.IsName("cameraTravel_birds") && state.normalizedTime >= 1f;
        bool atBirdTree2 = canNavigate && state.IsName("cameraTravel_birds2") && state.normalizedTime >= 1f;
        bool atBeach = canNavigate && (state.IsName("cameraTravel_beach") || state.IsName("cameraTravel_directBeach") || state.IsName("cameraTravel_birds2Beach")) && state.normalizedTime >= 1f;
        bool atShallowSea = canNavigate && state.IsName("cameraTravel_shallowSea") && state.normalizedTime >= 1f;
        bool atDeepSea = canNavigate && state.IsName("cameraTravel_deepSea") && state.normalizedTime >= 1f;
        if (birdButton != null && birdButton.gameObject.activeSelf != atDeepForest) birdButton.gameObject.SetActive(atDeepForest);
        // Handle the buffered press as well as Button.onClick: quick mobile taps can
        // finish between frames before the UI input module sends its click event.
        if (atDeepForest && birdButton != null && birdButton.gameObject.activeInHierarchy &&
            ForestPointerInput.TryGetPress(out Vector2 birdPress) &&
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)birdButton.transform, birdPress))
            BeginBirdJourney();
        bool showForward = atAnimals || atDeepForest || atBirdTree || atBirdTree2 || atBeach || atShallowSea;
        if (forwardButton.gameObject.activeSelf != showForward) forwardButton.gameObject.SetActive(showForward);
        bool showBack = atAnimals || atDeepForest || atBirdTree || atBirdTree2 || atBeach || atShallowSea || atDeepSea;
        if (button.gameObject.activeSelf != showBack) button.gameObject.SetActive(showBack);
    }

    private void LateUpdate()
    {
        if (!holdReturnedTree2View || returning || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;
        if (!journeyAnimator.GetCurrentAnimatorStateInfo(0).IsName("cameraTravel_birds2"))
        {
            holdReturnedTree2View = false;
            return;
        }
        transform.SetPositionAndRotation(returnedTree2Position, returnedTree2Rotation);
    }

    private void BeginForwardJourney()
    {
        if (lastNavigationFrame == Time.frameCount || returning || gate == null || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;
        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        if (!gate.CameraTravelStarted || state.normalizedTime < 1f) return;
        string next = state.IsName("CameraTravel") ? "cameraTravel_deepForest" :
                      state.IsName("cameraTravel_deepForest") ? "cameraTravel_directBeach" :
                      state.IsName("cameraTravel_birds") ? "cameraTravel_birds2" :
                      state.IsName("cameraTravel_birds2") ? "cameraTravel_birds2Beach" :
                      (state.IsName("cameraTravel_beach") || state.IsName("cameraTravel_directBeach") || state.IsName("cameraTravel_birds2Beach")) ? "cameraTravel_shallowSea" :
                      state.IsName("cameraTravel_shallowSea") ? "cameraTravel_deepSea" : null;
        if (next == null) return;
        if (!journeyAnimator.HasState(0, Animator.StringToHash(next)))
        {
            Debug.LogError($"The {next} travel animation is missing from the camera Animator.", this);
            return;
        }
        holdReturnedTree2View = false;
        if (state.IsName("cameraTravel_deepForest")) beachReturnState = "cameraTravel_directBeach";
        else if (state.IsName("cameraTravel_birds2")) beachReturnState = "cameraTravel_birds2Beach";
        lastNavigationFrame = Time.frameCount;
        if (birdButton != null) birdButton.gameObject.SetActive(false);
        forwardButton.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
        journeyAnimator.speed = 1f;
        journeyAnimator.Play(next, 0, 0f);
        journeyAnimator.Update(0f);
    }

    private void BeginBirdJourney()
    {
        if (lastNavigationFrame == Time.frameCount || returning || gate == null || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;
        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        if (!gate.CameraTravelStarted || !state.IsName("cameraTravel_deepForest") || state.normalizedTime < 1f) return;
        if (!journeyAnimator.HasState(0, Animator.StringToHash("cameraTravel_birds")))
        {
            Debug.LogError("The bird travel animation is missing from the camera Animator.", this);
            return;
        }
        lastNavigationFrame = Time.frameCount;
        birdButton.gameObject.SetActive(false);
        forwardButton.gameObject.SetActive(false);
        button.gameObject.SetActive(false);
        journeyAnimator.speed = 1f;
        journeyAnimator.Play("cameraTravel_birds", 0, 0f);
        journeyAnimator.Update(0f);
    }

    private void BeginReturn()
    {
        if (lastNavigationFrame == Time.frameCount || returning || gate == null || journeyAnimator == null || !journeyAnimator.enabled ||
            (animalHandler != null && animalHandler.HasSavedView)) return;

        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime < 1f) return;
        holdReturnedTree2View = false;
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
            StartCoroutine(ReturnAlongClip(clip, beachReturnState));
        }
        else if (state.IsName("cameraTravel_birds2Beach") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_birds2Beach") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The second tree to beach travel clip is missing.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_birds2"));
        }
        else if (state.IsName("cameraTravel_birds2") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_birds2") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The second tree travel clip is missing.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_birds"));
        }
        else if (state.IsName("cameraTravel_directBeach") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_directBeach") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The direct beach travel clip is missing; cannot return to deep forest.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_deepForest"));
        }
        else if (state.IsName("cameraTravel_beach") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_beach") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The beach travel clip is missing; cannot return to the crow tree.", this); return; }
            StartCoroutine(ReturnAlongClip(clip, "cameraTravel_birds"));
        }
        else if (state.IsName("cameraTravel_birds") && state.normalizedTime >= 1f)
        {
            AnimationClip clip = null;
            foreach (var candidate in journeyAnimator.runtimeAnimatorController.animationClips)
                if (candidate.name == "cameraTravel_birds") { clip = candidate; break; }
            if (clip == null) { Debug.LogError("The crow-tree travel clip is missing; cannot return to deep forest.", this); return; }
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
        bool returnToTree2 = clip.name == "cameraTravel_birds2Beach" && previousState == "cameraTravel_birds2";
        journeyAnimator.enabled = true;
        journeyAnimator.speed = 1f;
        journeyAnimator.Play(previousState, 0, 1f);
        journeyAnimator.Update(0f);
        if (returnToTree2)
        {
            returnedTree2Position = transform.position;
            returnedTree2Rotation = transform.rotation;
            holdReturnedTree2View = true;
            transform.SetPositionAndRotation(returnedTree2Position, returnedTree2Rotation);
        }
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
