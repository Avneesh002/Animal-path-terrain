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
    private Vector3 gatePosition;
    private Quaternion gateRotation;
    private bool returning;

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
        gatePosition = transform.position;
        gateRotation = transform.rotation;
        button = CreateButton(false);
        forwardButton = CreateButton(true);
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
        if (forward) result.onClick.AddListener(() => gate.BeginForwardJourney());
        else result.onClick.AddListener(BeginReturn);
        obj.SetActive(false);
        return result;
    }

    private void Update()
    {
        if (button == null || forwardButton == null || gate == null || journeyAnimator == null) return;
        if (animalHandler == null) animalHandler = GetComponent<AnimalTapHandler>();
        var state = journeyAnimator.GetCurrentAnimatorStateInfo(0);
        bool arrived = gate.CameraTravelStarted && journeyAnimator.enabled && state.IsName("CameraTravel") && state.normalizedTime >= 0.99f;
        bool visible = !returning && arrived && (animalHandler == null || !animalHandler.HasSavedView);
        bool showForward = false; // The gate itself starts the journey.
        forwardButton.gameObject.SetActive(showForward);
        if (button.gameObject.activeSelf != visible) button.gameObject.SetActive(visible);
    }

    private void BeginReturn()
    {
        if (returning || gate == null || journeyAnimator == null) return;
        StartCoroutine(ReturnToGate());
    }

    private IEnumerator ReturnToGate()
    {
        returning = true;
        button.gameObject.SetActive(false);
        if (animalHandler != null) animalHandler.enabled = false;
        gate.SetJourneyReturning(true);
        journeyAnimator.enabled = false;
        Vector3 start = transform.position;
        Quaternion rotation = transform.rotation;
        const float duration = 4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.SetPositionAndRotation(Vector3.Lerp(start, gatePosition, t), Quaternion.Slerp(rotation, gateRotation, t));
            yield return null;
        }
        transform.SetPositionAndRotation(gatePosition, gateRotation);
        gate.ResetAfterReturn();
        if (animalHandler != null) animalHandler.enabled = true;
        returning = false;
    }
}
