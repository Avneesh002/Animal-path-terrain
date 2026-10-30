using UnityEngine;

/// <summary>Identifies a selectable animal and optionally provides its camera focus point.</summary>
[DisallowMultipleComponent]
public sealed class AnimalTapTarget : MonoBehaviour
{
    [SerializeField] private string animalId;
    [SerializeField] private Transform focusPoint;

    [Tooltip("Close-up offset in metres along the animal's right, up, and forward axes. Model scale is ignored.")]
    [SerializeField] private Vector3 closeUpOffset = new Vector3(1f, 0.6f, 4.5f);

    public Vector3 CloseUpPosition => FocusPoint.position + transform.TransformDirection(closeUpOffset);
    public string AnimalId => animalId;
    public Transform FocusPoint => focusPoint != null ? focusPoint : transform;
}
