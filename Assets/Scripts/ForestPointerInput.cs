using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using TrackedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>Shared buffered touch input for world interactions, including short mobile taps.</summary>
public static class ForestPointerInput
{
    private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    public static bool TryGetPress(out Vector2 position)
    {
        Initialize();
        // EnhancedTouch keeps a Began event visible even when a quick tap ends
        // between rendered frames. All consumers can read the same event.
        foreach (var touch in TrackedTouch.activeTouches)
        {
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) continue;
            position = touch.screenPosition;
            return true;
        }
        // Do not treat a synthesized mouse event as a second mobile touch.
        var mouse = Mouse.current;
        if (TrackedTouch.activeTouches.Count == 0 && mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            position = mouse.position.ReadValue();
            return true;
        }
        position = default;
        return false;
    }

    public static bool IsOverUI(Vector2 position)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        UiHits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = position }, UiHits);
        foreach (var hit in UiHits)
        {
            // Physics raycasters report world hits too; those must not block animals.
            if (hit.module is GraphicRaycaster) return true;
        }
        return false;
    }
}
