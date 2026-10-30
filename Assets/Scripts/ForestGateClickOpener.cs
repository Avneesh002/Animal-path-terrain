using System.Collections;
using UnityEngine;

/// <summary>Click anywhere on the forest entrance gate to swing its leaf open or closed.</summary>
public sealed class ForestGateClickOpener : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float openAngle = 100f;
    [SerializeField, Min(0.05f)] private float animationDuration = 1.1f;
    [SerializeField, Min(1f)] private float clickDistance = 80f;
    [SerializeField] private Animator cameraTravelAnimator;
    [SerializeField] private GameObject gateHighlight;

    private Transform gateHinge;
    private Quaternion closedRotation;
    private bool isOpen;
    private Coroutine movement;
    private bool cameraTravelStarted;
    private bool journeyReturning;
    public bool CameraTravelStarted => cameraTravelStarted;

    public void SetJourneyReturning(bool value) => journeyReturning = value;

    public void ResetAfterReturn()
    {
        if (movement != null) StopCoroutine(movement);
        movement = null;
        isOpen = false;
        gateHinge.localRotation = closedRotation;
        if (gateHighlight != null) gateHighlight.SetActive(true);
        cameraTravelStarted = false;
        journeyReturning = false;
    }

    private void Awake()
    {
        gateHinge = transform.Find("GateLeaf_Hinge");
        if (gateHinge == null)
        {
            Debug.LogError("ForestGateClickOpener requires a child named GateLeaf_Hinge.", this);
            enabled = false;
            return;
        }

        closedRotation = gateHinge.localRotation;
        if (gateHighlight == null)
            gateHighlight = gateHinge.Find("GateHighlight")?.gameObject;
    }

    private void Update()
    {
        if (journeyReturning || !ForestPointerInput.TryGetPress(out Vector2 pointerPosition))
        {
            return;
        }

        if (ForestPointerInput.IsOverUI(pointerPosition)) return;

        var playerCamera = Camera.main;
        if (playerCamera == null)
            return;

        Ray clickRay = playerCamera.ScreenPointToRay(pointerPosition);
        if (Physics.Raycast(clickRay, out RaycastHit hit, clickDistance) &&
            (hit.transform == transform || hit.transform.IsChildOf(transform)))
        {
            ToggleGate();
        }
    }

    public void BeginForwardJourney()
    {
        if (journeyReturning || cameraTravelStarted || movement != null) return;
        if (!isOpen) ToggleGate();
    }

    private void ToggleGate()
    {
        isOpen = !isOpen;

        // Stop the spotlight when opening so it does not sweep across the scene.
        // Restore it if the gate is closed again.
        if (gateHighlight != null)
            gateHighlight.SetActive(!isOpen);
        if (movement != null)
            StopCoroutine(movement);

        Quaternion target = isOpen
            ? closedRotation * Quaternion.Euler(0f, openAngle, 0f)
            : closedRotation;
        movement = StartCoroutine(RotateHinge(target));
    }

    private IEnumerator RotateHinge(Quaternion target)
    {
        Quaternion start = gateHinge.localRotation;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = t * t * (3f - 2f * t);
            gateHinge.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        gateHinge.localRotation = target;
        movement = null;

        if (isOpen && !cameraTravelStarted && cameraTravelAnimator != null)
            {
                cameraTravelStarted = true;
                cameraTravelAnimator.enabled = true;
                cameraTravelAnimator.Play("CameraTravel", 0, 0f);
            }
        }
}
