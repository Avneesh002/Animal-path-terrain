using UnityEngine;

/// <summary>Moves a combined low-poly fish school with one transform update.</summary>
public sealed class UnderwaterMotion : MonoBehaviour
{
    [SerializeField] private float phase;
    private Vector3 origin;
    private void Start() { origin = transform.localPosition; phase += transform.GetSiblingIndex() * 1.7f; }
    private void Update()
    {
        float t = Time.time * 0.32f + phase;
        transform.localPosition = origin + new Vector3(Mathf.Sin(t) * 3f, Mathf.Sin(t * 1.4f) * 0.3f, Mathf.Cos(t) * 1.2f);
    }
}
