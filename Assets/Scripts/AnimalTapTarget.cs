using UnityEngine;

/// <summary>Identifies a selectable animal and optionally provides its camera focus point.</summary>
[DisallowMultipleComponent]
public sealed class AnimalTapTarget : MonoBehaviour
{
    [SerializeField] private string animalId;
    [SerializeField] private Transform focusPoint;

    [Tooltip("Close-up offset in metres along the animal's right, up, and forward axes. Model scale is ignored.")]
    [SerializeField] private Vector3 closeUpOffset = new Vector3(1f, 0.6f, 4.5f);

    [Header("Optional focus animations")]
    [SerializeField] private Animator animalAnimator;
    [SerializeField] private string idleStateName = "CowIdle";
    [SerializeField] private string focusStateName = "CowZoom";

    public Vector3 CloseUpPosition => FocusPoint.position + transform.TransformDirection(closeUpOffset);
    public string AnimalId => animalId;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
    public Vector3 CloseUpOffset => closeUpOffset;

    /// <summary>Assigns an animal's focused and idle animation states.</summary>
    public void ConfigureFocusAnimation(Animator animator, string idleState, string focusState)
    {
        animalAnimator = animator;
        idleStateName = idleState;
        focusStateName = focusState;
    }

    /// <summary>Plays the zoom reaction while focused, then returns to idle on exit.</summary>
    public void SetZoomed(bool zoomed)
    {
        Animator animator = animalAnimator != null
            ? animalAnimator
            : GetComponentInChildren<Animator>(true);
        string stateName = zoomed ? focusStateName : idleStateName;
        if (animator == null || string.IsNullOrEmpty(stateName))
            return;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
            return;

        animator.CrossFadeInFixedTime(stateHash, 0.2f, 0, 0f);
    }
}
