using UnityEngine;

/// <summary>
/// An enum that indicates the scope in which gestures are received:
/// </summary>
[Tooltip("Anywhere: anywhere on the screen. OnThisObject: only over this object (needs a Collider2D).")]
public enum GestureScope { Anywhere, OnThisObject }

/// <summary>
/// Base class for reacting to gestures. Inherit from it and override only the methods you need. <br/>
/// It subscribes and unsubscribes itself from the GestureDetector (one is created if none exists).
/// </summary>
[Tooltip("With Scope = OnThisObject, the object needs a Collider2D.")]
public abstract class GestureReceiver : MonoBehaviour
{
    [Header("Gestures")]
    [SerializeField] GestureScope scope = GestureScope.OnThisObject;

    [Tooltip("Camera used to convert screen to world. If empty, Camera.main is used.")]
    [SerializeField] Camera gestureCamera;

    /// <summary>Default scope when the component is added in the editor. Child classes can change it.</summary>
    protected virtual GestureScope DefaultScope => GestureScope.OnThisObject;

    protected GestureScope Scope => scope;
    protected Camera Cam => gestureCamera != null ? gestureCamera : Camera.main;

    GestureDetector detector;
    bool ownsDrag;

#region Virtual methods to override
    protected virtual void OnTap(Vector2 screenPos) { }
    protected virtual void OnDoubleTap(Vector2 screenPos) { }
    protected virtual void OnLongPress(Vector2 screenPos) { }

    protected virtual void OnDragStart(Vector2 screenPos) { }
    protected virtual void OnDrag(Vector2 screenPos, Vector2 deltaPx) { }
    protected virtual void OnDragEnd(Vector2 screenPos) { }

    /// <param name="startScreenPos">Where the swipe started.</param>
    /// <param name="direction">Normalized direction.</param>
    protected virtual void OnSwipe(Vector2 startScreenPos, Vector2 direction) { }

    /// <param name="deltaPx">Positive = fingers moving apart (zoom in).</param>
    protected virtual void OnPinch(float deltaPx, Vector2 centerScreenPos) { }
    #endregion

#region Utilities for child classes
    /// <summary>Converts a screen position to 2D world coordinates.</summary>
    protected Vector2 ToWorld(Vector2 screenPos) => Cam.ScreenToWorldPoint(screenPos);

    /// <summary>Is there a collider of this object (or one of its children) under that screen position?</summary>
    protected bool IsOverThisObject(Vector2 screenPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(ToWorld(screenPos));
        return hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform));
    }

    bool Accepts(Vector2 screenPos) => scope == GestureScope.Anywhere || IsOverThisObject(screenPos);
    #endregion

#region GestureDetector event subscription
    protected virtual void Reset() => scope = DefaultScope;

    protected virtual void OnEnable()
    {
        detector = GestureDetector.Instance;

        detector.OnTap += RouteTap;
        detector.OnDoubleTap += RouteDoubleTap;
        detector.OnLongPress += RouteLongPress;
        detector.OnDragStart += RouteDragStart;
        detector.OnDrag += RouteDrag;
        detector.OnDragEnd += RouteDragEnd;
        detector.OnSwipe += RouteSwipe;
        detector.OnPinch += RoutePinch;
    }

    protected virtual void OnDisable()
    {
        ownsDrag = false;
        if (detector == null) return;

        detector.OnTap -= RouteTap;
        detector.OnDoubleTap -= RouteDoubleTap;
        detector.OnLongPress -= RouteLongPress;
        detector.OnDragStart -= RouteDragStart;
        detector.OnDrag -= RouteDrag;
        detector.OnDragEnd -= RouteDragEnd;
        detector.OnSwipe -= RouteSwipe;
        detector.OnPinch -= RoutePinch;
        detector = null;
    }
    #endregion

#region Scope filtering
    void RouteTap(Vector2 pos) { if (Accepts(pos)) OnTap(pos); }
    void RouteDoubleTap(Vector2 pos) { if (Accepts(pos)) OnDoubleTap(pos); }
    void RouteLongPress(Vector2 pos) { if (Accepts(pos)) OnLongPress(pos); }

    /// <summary>
    /// The drag "belongs" to the receiver it started on, even if the finger later leaves it.
    /// </summary>
    /// <param name="startPos">Screen position where the drag started.</param>
    void RouteDragStart(Vector2 startPos)
    {
        ownsDrag = Accepts(startPos);
        if (ownsDrag) OnDragStart(startPos);
    }

    void RouteDrag(Vector2 pos, Vector2 delta)
    {
        if (ownsDrag) OnDrag(pos, delta);
    }

    void RouteDragEnd(Vector2 pos)
    {
        if (!ownsDrag) return;
        ownsDrag = false;
        OnDragEnd(pos);
    }

    void RouteSwipe(Vector2 startPos, Vector2 dir) { if (Accepts(startPos)) OnSwipe(startPos, dir); }
    void RoutePinch(float delta, Vector2 center) { if (Accepts(center)) OnPinch(delta, center); }
#endregion
}