using System;
using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Detects tap, double tap, long press, drag, swipe and pinch.<br/>
/// Distances are measured in cm so it behaves the same on any screen.<br/>
/// It runs first so other scripts can subscribe to its events in Update().
/// </summary>
[DefaultExecutionOrder(-100)]
public class GestureDetector : MonoBehaviour
{
    static GestureDetector instance;

    public static GestureDetector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GestureDetector>();
                if (instance == null)
                    instance = new GameObject("New GestureDetector instance created").AddComponent<GestureDetector>();
            }
            return instance;
        }
    }

    [Header("Tap / Doble tap")]
    [SerializeField] float tapMaxDuration = 0.25f;
    [SerializeField] float doubleTapMaxDelay = 0.3f;
    [SerializeField] float doubleTapMaxDistanceCm = 1f;

    [Header("Long press")]
    [SerializeField] float longPressDuration = 0.6f;

    [Header("Drag / Swipe")]
    [SerializeField] float dragThresholdCm = 0.25f;
    [SerializeField] float swipeMinDistanceCm = 1.5f;
    [SerializeField] float swipeMaxDuration = 0.4f;

    float mouseScrollPinchPx = 60f;

    // Events (positions in screen pixels)
    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDoubleTap;
    public event Action<Vector2> OnLongPress;
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2, Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;
    public event Action<Vector2, Vector2> OnSwipe;
    public event Action<float, Vector2> OnPinch;

    /// <summary>
    /// Phases of a contact.<br/>
    /// Began, Moved, Stationary, Ended, Canceled.
    /// </summary>
    enum GesturePhase { Began, Moved, Stationary, Ended, Canceled }

    struct Contact
    {
        public Vector2 position;
        public Vector2 delta;
        public GesturePhase phase;
    }

    readonly Contact[] contacts = new Contact[2];
    int contactCount;
    Vector2 lastMousePos;

    [Header("Screen State")]
    [SerializeField] float defaultDPi = 160f;
    [SerializeField] float inchToCm = 2.54f;
    float pxPerCm;

    bool tracking, dragging, longFired;
    Vector2 startPos, lastPos;
    float startTime;

    bool pendingTap;
    float lastTapTime;
    Vector2 lastTapPos;

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this)
        {
            Debug.LogWarning("[GestureDetector] There is already another GestureDetector in the scene; this one is being disabled.");
            enabled = false;
            Destroy(this);
            return;
        }

        float dpi = Screen.dpi > 0 ? Screen.dpi : defaultDPi;
        pxPerCm = dpi / inchToCm;
    }

    void OnEnable() => EnhancedTouch.EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouch.EnhancedTouchSupport.Disable();

    void Update()
    {
        ReadContacts();

        // Confirm a single tap once the double tap window has passed.
        // If a new finger is already down, compare against the moment it started.
        if (pendingTap)
        {
            float reference = tracking ? startTime : Time.unscaledTime;
            if (reference - lastTapTime > doubleTapMaxDelay)
            {
                pendingTap = false;
                OnTap?.Invoke(lastTapPos);
            }
        }

        if (contactCount >= 2)
        {
            if (tracking) AbortTouch();
            HandlePinch(contacts[0], contacts[1]);
        }
        else if (contactCount == 1)
        {
            HandleSingleTouch(contacts[0]);
        }
    }


    /// <summary>
    /// Reads the active touches and stores them in the contacts array.
    /// </summary>
    void ReadContacts()
    {
        contactCount = 0;

        foreach (var t in EnhancedTouch.Touch.activeTouches)
        {
            if (contactCount >= contacts.Length) break;
            contacts[contactCount++] = new Contact
            {
                position = t.screenPosition,
                delta = t.delta,
                phase = ConvertPhase(t.phase)
            };
        }

        // No real touches on PC/editor: simulate with the mouse
        if (contactCount == 0 && !Application.isMobilePlatform) ReadMouse();
    }

    /// <summary>
    /// Reads the mouse state and translates it into a simulated contact.
    /// </summary>
    void ReadMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();
        bool down = mouse.leftButton.wasPressedThisFrame;
        bool up = mouse.leftButton.wasReleasedThisFrame;
        bool held = mouse.leftButton.isPressed;

        if (down || held || up)
        {
            GesturePhase phase = up ? GesturePhase.Ended : down ? GesturePhase.Began : 
                (pos != lastMousePos ? GesturePhase.Moved : GesturePhase.Stationary);

            contacts[0] = new Contact
            {
                position = pos,
                delta = down ? Vector2.zero : pos - lastMousePos,
                phase = phase
            };
            contactCount = 1;
        }
        lastMousePos = pos;

        // The mouse wheel simulates pinch (up = zoom in)
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            OnPinch?.Invoke(Mathf.Sign(scroll) * mouseScrollPinchPx, pos);
    }

    /// <summary>
    /// Converts the InputSystem TouchPhase to the internal GesturePhase.
    /// </summary>
    /// <param name="p">The InputSystem TouchPhase.</param>
    /// <returns>The matching GesturePhase.</returns>
    static GesturePhase ConvertPhase(UnityEngine.InputSystem.TouchPhase p)
    {
        switch (p)
        {
            case UnityEngine.InputSystem.TouchPhase.Began: return GesturePhase.Began;
            case UnityEngine.InputSystem.TouchPhase.Moved: return GesturePhase.Moved;
            case UnityEngine.InputSystem.TouchPhase.Ended: return GesturePhase.Ended;
            case UnityEngine.InputSystem.TouchPhase.Canceled: return GesturePhase.Canceled;
            default: return GesturePhase.Stationary;
        }
    }

#region Gesture logic

    /// <summary>
    /// Handles a single touch (tap, long press, drag).
    /// </summary>
    /// <param name="c">The contact to handle.</param>
    void HandleSingleTouch(Contact c)
    {
        switch (c.phase)
        {
            case GesturePhase.Began:
                tracking = true;
                dragging = false;
                longFired = false;
                startPos = lastPos = c.position;
                startTime = Time.unscaledTime;
                break;

            case GesturePhase.Moved:
            case GesturePhase.Stationary:
                if (!tracking) return;
                lastPos = c.position;

                float distCm = (c.position - startPos).magnitude / pxPerCm;

                if (!dragging && !longFired && distCm > dragThresholdCm)
                {
                    dragging = true;
                    OnDragStart?.Invoke(startPos);
                }

                if (dragging)
                {
                    OnDrag?.Invoke(c.position, c.delta);
                }
                else if (!longFired && Time.unscaledTime - startTime >= longPressDuration)
                {
                    longFired = true;
                    OnLongPress?.Invoke(c.position);
                }
                break;

            case GesturePhase.Ended:
                if (!tracking) return;
                EndTouch(c.position);
                break;

            case GesturePhase.Canceled:
                if (tracking) AbortTouch();
                break;
        }
    }

    /// <summary>
    /// Ends the current touch, firing the matching events depending on the gesture's duration and distance.
    /// </summary>
    /// <param name="endPos">The final position of the touch.</param>
    void EndTouch(Vector2 endPos)
    {
        float duration = Time.unscaledTime - startTime;
        Vector2 delta = endPos - startPos;
        float distCm = delta.magnitude / pxPerCm;

        if (dragging)
        {
            OnDragEnd?.Invoke(endPos);

            if (duration <= swipeMaxDuration && distCm >= swipeMinDistanceCm)
                OnSwipe?.Invoke(startPos, delta.normalized);
        }
        else if (!longFired && duration <= tapMaxDuration)
        {
            RegisterTap(endPos);
        }

        ResetTouch();
    }

    /// <summary>
    /// Registers a tap and determines whether it is a single tap or a double tap, firing the matching events.
    /// </summary>
    /// <param name="pos">The position of the tap.</param>
    void RegisterTap(Vector2 pos)
    {
        bool isDouble = pendingTap
            && startTime - lastTapTime <= doubleTapMaxDelay
            && (pos - lastTapPos).magnitude / pxPerCm <= doubleTapMaxDistanceCm;

        if (isDouble)
        {
            pendingTap = false;
            OnDoubleTap?.Invoke(pos);
        }
        else
        {
            if (pendingTap) OnTap?.Invoke(lastTapPos);
            pendingTap = true;
            lastTapTime = Time.unscaledTime;
            lastTapPos = pos;
        }
    }

    /// <summary>
    /// Interrupts the current touch, closing the drag if there was one.
    /// </summary>
    void AbortTouch()
    {
        if (dragging) OnDragEnd?.Invoke(lastPos);
        ResetTouch();
    }

    /// <summary>
    /// Resets the touch tracking state, cancelling any gesture in progress.
    /// </summary>
    void ResetTouch()
    {
        tracking = false;
        dragging = false;
        longFired = false;
    }

    /// <summary>
    /// Handles the pinch gesture between two contacts, computing the change in distance between the fingers
    /// and firing the OnPinch event.
    /// </summary>
    /// <param name="a">The first contact of the pinch.</param>
    /// <param name="b">The second contact of the pinch.</param>
    void HandlePinch(Contact a, Contact b)
    {
        Vector2 prevA = a.position - a.delta;
        Vector2 prevB = b.position - b.delta;

        float prevDist = (prevA - prevB).magnitude;
        float currDist = (a.position - b.position).magnitude;

        // Positivo = dedos separándose (zoom in), negativo = juntándose (zoom out)
        OnPinch?.Invoke(currDist - prevDist, (a.position + b.position) * 0.5f);
    }

    /// <summary>
    /// Converts a screen position (in pixels) to world coordinates,
    /// using the given camera or the main camera if none is provided.
    /// </summary>
    /// <param name="screenPos">The screen position (in pixels) to convert.</param>
    /// <param name="cam">The camera to use for the conversion. If null, the main camera is used.</param>
    /// <returns>The position in world coordinates.</returns>
    public static Vector2 ToWorld(Vector2 screenPos, Camera cam = null)
    {
        cam = cam != null ? cam : Camera.main;
        return cam.ScreenToWorldPoint(screenPos);
    }
#endregion
}