using UnityEngine;

/// <summary>Shows the close-up back button inside the device safe area.</summary>
[DisallowMultipleComponent]
public sealed class AnimalDetailUI : MonoBehaviour
{
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private UnityEngine.UI.Button backButton;

    private AnimalTapHandler tapHandler;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        if (backButton == null || safeArea == null)
        {
            Debug.LogError("AnimalDetailUI needs its SafeArea and BackButton references.", this);
            enabled = false;
            return;
        }

        backButton.onClick.AddListener(ReturnToJourney);
        backButton.gameObject.SetActive(false);
        ApplySafeArea();
    }

    private void Update()
    {
        if (tapHandler == null)
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera != null)
                tapHandler = sceneCamera.GetComponent<AnimalTapHandler>();
        }

        bool visible = tapHandler != null && tapHandler.isActiveAndEnabled && tapHandler.IsAnimalFocused;
        if (backButton.gameObject.activeSelf != visible)
            backButton.gameObject.SetActive(visible);

        ApplySafeArea();
    }

    private void ReturnToJourney()
    {
        if (tapHandler != null)
            tapHandler.RestoreJourneyView();
        backButton.gameObject.SetActive(false);
    }

    private void ApplySafeArea()
    {
        int width = Screen.width;
        int height = Screen.height;
        if (width <= 0 || height <= 0)
            return;

        Rect bounds = Screen.safeArea;
        Vector2Int size = new Vector2Int(width, height);
        if (bounds == lastSafeArea && size == lastScreenSize)
            return;

        safeArea.anchorMin = new Vector2(bounds.xMin / width, bounds.yMin / height);
        safeArea.anchorMax = new Vector2(bounds.xMax / width, bounds.yMax / height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
        lastSafeArea = bounds;
        lastScreenSize = size;
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(ReturnToJourney);
    }
}
