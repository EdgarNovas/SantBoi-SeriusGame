using System;
using UnityEngine;
using UnityEngine.InputSystem;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Detecta tap, doble tap, long press, drag, swipe y pinch.
/// Las distancias se miden en cm para que funcione igual en cualquier pantalla.
/// Es el primero en ejecutarse para que otros scripts puedan suscribirse a sus eventos en Update().
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

    // Eventos (posiciones en píxeles de pantalla)
    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDoubleTap;
    public event Action<Vector2> OnLongPress;
    public event Action<Vector2> OnDragStart;
    public event Action<Vector2, Vector2> OnDrag;
    public event Action<Vector2> OnDragEnd;
    public event Action<Vector2, Vector2> OnSwipe;
    public event Action<float, Vector2> OnPinch;

    // Fases de un contacto (dedo o ratón)
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
            Debug.LogWarning("[GestureDetector] Ya hay otro GestureDetector en la escena; se desactiva este.");
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

        // Confirmar tap simple si ya pasó la ventana del doble tap.
        // Si hay un dedo nuevo apoyado, se compara con el momento en que empezó.
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
    /// Lee los toques activos y los guarda en el array de contactos.
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

        // Sin toques reales y en PC/editor: simular con el ratón
        if (contactCount == 0 && !Application.isMobilePlatform) ReadMouse();
    }

    /// <summary>
    /// Lee el estado del ratón y lo traduce a un contacto simulado.
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

        // La rueda del ratón simula el pinch (arriba = zoom in)
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            OnPinch?.Invoke(Mathf.Sign(scroll) * mouseScrollPinchPx, pos);
    }

    /// <summary>
    /// Convierte el TouchPhase de InputSystem al GesturePhase interno.
    /// </summary>
    /// <param name="p">El TouchPhase de InputSystem.</param>
    /// <returns>El GesturePhase correspondiente.</returns>
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


    #region Lógica de gestos

    /// <summary>
    /// Maneja un toque individual (Tap).
    /// </summary>
    /// <param name="c">El contacto a manejar.</param>
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
    /// Finaliza el toque actual, disparando los eventos correspondientes según la duración y distancia del gesto.
    /// </summary>
    /// <param name="endPos">La posición final del toque.</param>
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
    /// Registra un tap y determina si es un tap simple o un doble tap, disparando los eventos correspondientes.
    /// </summary>
    /// <param name="pos">La posición del tap.</param>
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
    /// Interrumpe el toque actual cerrando el drag si lo había.
    /// </summary>
    void AbortTouch()
    {
        if (dragging) OnDragEnd?.Invoke(lastPos);
        ResetTouch();
    }

    /// <summary>
    /// Reinicia el estado de seguimiento del toque, cancelando cualquier gesto en curso.
    /// </summary>
    void ResetTouch()
    {
        tracking = false;
        dragging = false;
        longFired = false;
    }

    /// <summary>
    /// Maneja el gesto de pinch entre dos contactos, calculando la diferencia de distancia entre los dedos y disparando el 
    /// evento OnPinch.
    /// </summary>
    /// <param name="a">El primer contacto del pinch.</param>
    /// <param name="b">El segundo contacto del pinch.</param>
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
    /// Convierte una posición de pantalla (en píxeles) a coordenadas del mundo, 
    /// usando la cámara especificada o la cámara principal si no se proporciona.
    /// </summary>
    /// <param name="screenPos">La posición en pantalla (en píxeles) a convertir.</param>
    /// <param name="cam">La cámara a usar para la conversión. Si es null, se usa la cámara principal.</param>
    /// <returns>La posición en coordenadas del mundo.</returns>
    public static Vector2 ToWorld(Vector2 screenPos, Camera cam = null)
    {
        cam = cam != null ? cam : Camera.main;
        return cam.ScreenToWorldPoint(screenPos);
    }
    #endregion
}