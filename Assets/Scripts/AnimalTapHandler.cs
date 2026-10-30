using UnityEngine;

/// <summary>Detects taps/clicks on colliders and reports selectable animals.</summary>
[DisallowMultipleComponent]
public sealed class AnimalTapHandler : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float rayDistance = 100f;
    [SerializeField] private LayerMask animalLayers = ~0;

    [Header("Camera close-up")]
    [SerializeField, Min(0.05f)] private float zoomDuration = 0.5f;

    private Camera sceneCamera;
    private Animator journeyAnimator;
    private float savedAnimatorSpeed;
    private bool hasSavedView;
    private bool isReturning;
    private bool isMoving;
    private AnimalTapTarget selectedAnimal;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 destinationPosition;
    private Quaternion destinationRotation;
    private float moveElapsed;

    public bool HasSavedView => hasSavedView;
    public bool IsAnimalFocused => hasSavedView && !isReturning;

    // Install on the scene camera at runtime so no scene-object wiring is required.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnMainCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("AnimalTapHandler could not find a Camera tagged MainCamera.");
            return;
        }

        if (mainCamera.GetComponent<AnimalTapHandler>() == null)
            mainCamera.gameObject.AddComponent<AnimalTapHandler>();
    }

    private void Awake()
    {
        sceneCamera = GetComponent<Camera>();
        if (sceneCamera == null)
            sceneCamera = Camera.main;
    }

    private void Update()
    {
        if (!ForestPointerInput.TryGetPress(out Vector2 screenPosition) || sceneCamera == null)
            return;

        if (ForestPointerInput.IsOverUI(screenPosition))
            return;

        Ray tapRay = sceneCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(tapRay, out RaycastHit hit, rayDistance, animalLayers))
            return;

        AnimalTapTarget target = hit.collider.GetComponentInParent<AnimalTapTarget>();
        if (target == null)
            return;

        SelectAnimal(target);
    }

    /// <summary>Save the journey view once, then move only the camera to the animal.</summary>
    public void SelectAnimal(AnimalTapTarget target)
    {
        if (target == null || sceneCamera == null ||
            (selectedAnimal == target && hasSavedView && !isReturning))
            return;

        Vector3 closeUpPosition = target.CloseUpPosition;
        Vector3 lookDirection = target.FocusPoint.position - closeUpPosition;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("The animal's close-up offset must be greater than zero.", target);
            return;
        }

        if (!hasSavedView)
        {
            savedPosition = sceneCamera.transform.position;
            savedRotation = sceneCamera.transform.rotation;
            hasSavedView = true;

            // Pause the travel timeline without resetting its current playback position.
            journeyAnimator = sceneCamera.GetComponent<Animator>();
            if (journeyAnimator != null)
            {
                savedAnimatorSpeed = journeyAnimator.speed;
                journeyAnimator.speed = 0f;
            }
        }

        selectedAnimal = target;
        isReturning = false;
        BeginMove(closeUpPosition, Quaternion.LookRotation(lookDirection, Vector3.up));
        Debug.Log($"Animal selected: {target.AnimalId}", target);
        AnimalFlutterEvents.Send("animalSelected", target.AnimalId);
    }

    /// <summary>Restore the journey view and notify Flutter when the close-up ends.</summary>
    [ContextMenu("Restore Journey View")]
    public void RestoreJourneyView()
    {
        if (!hasSavedView || isReturning || sceneCamera == null)
            return;

        isReturning = true;
        BeginMove(savedPosition, savedRotation);
        if (selectedAnimal != null)
            AnimalFlutterEvents.Send("animalClosed", selectedAnimal.AnimalId);
    }

    private void BeginMove(Vector3 position, Quaternion rotation)
    {
        startPosition = sceneCamera.transform.position;
        startRotation = sceneCamera.transform.rotation;
        destinationPosition = position;
        destinationRotation = rotation;
        moveElapsed = 0f;
        isMoving = true;
    }

    private void LateUpdate()
    {
        if (!hasSavedView || sceneCamera == null)
            return;

        if (selectedAnimal == null && !isReturning)
            RestoreJourneyView();

        // Apply after animation evaluation so the paused travel clip cannot overwrite the view.
        if (isMoving)
        {
            moveElapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(moveElapsed / zoomDuration));
            sceneCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, destinationPosition, t),
                Quaternion.Slerp(startRotation, destinationRotation, t));

            if (moveElapsed < zoomDuration)
                return;

            isMoving = false;
            // Tell Flutter only after the close-up reaches its destination.
            if (!isReturning && selectedAnimal != null)
                AnimalFlutterEvents.Send("animalFocusReady", selectedAnimal.AnimalId);
        }

        sceneCamera.transform.SetPositionAndRotation(destinationPosition, destinationRotation);
        if (isReturning)
            ReleaseSavedView();
    }

    private void ReleaseSavedView()
    {
        if (journeyAnimator != null)
            journeyAnimator.speed = savedAnimatorSpeed;

        hasSavedView = false;
        isReturning = false;
        isMoving = false;
        selectedAnimal = null;
    }

    private void OnDisable()
    {
        if (!hasSavedView)
            return;

        if (sceneCamera != null)
            sceneCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
        ReleaseSavedView();
    }

}
