using UnityEngine;

/// <summary>
/// Un enum para indicar el ámbito de recepción de gestos: 
/// </summary>
[Tooltip("Anywhere: en cualquier parte de la pantalla. OnThisObject: solo encima de este objeto (necesita Collider2D).")]

public enum GestureScope { Anywhere, OnThisObject }

/// <summary>
/// Clase base para reaccionar a gestos. Hereda de ella y sobrescribe (override) de
/// solo los metodos que necesites. <br/>
/// - Se suscribe y se desuscribe sola al GestureDetector (se crea uno si no existe). <br/>
/// - Con Scope = OnThisObject, el objeto necesita un Collider2D.<br/>
/// </summary>
public abstract class GestureReceiver : MonoBehaviour
{
    [Header("Gestos")]
    [SerializeField] GestureScope scope = GestureScope.OnThisObject;

    [Tooltip("Cámara para convertir pantalla a mundo. Si está vacia, se usa Camera.main.")]
    [SerializeField] Camera gestureCamera;

    /// <summary>Ambito por defecto al añadir el componente en el editor. Las clases hijas pueden cambiarlo.</summary>
    protected virtual GestureScope DefaultScope => GestureScope.OnThisObject;

    protected GestureScope Scope => scope;
    protected Camera Cam => gestureCamera != null ? gestureCamera : Camera.main;

    GestureDetector detector;
    bool ownsDrag;

#region Metodos virtuales a sobrescribir (vacios por defecto)
    protected virtual void OnTap(Vector2 screenPos) { }
    protected virtual void OnDoubleTap(Vector2 screenPos) { }
    protected virtual void OnLongPress(Vector2 screenPos) { }

    protected virtual void OnDragStart(Vector2 screenPos) { }
    protected virtual void OnDrag(Vector2 screenPos, Vector2 deltaPx) { }
    protected virtual void OnDragEnd(Vector2 screenPos) { }

    /// <param name="startScreenPos">Donde empezo el swipe.</param>
    /// <param name="direction">Direccion normalizada.</param>
    protected virtual void OnSwipe(Vector2 startScreenPos, Vector2 direction) { }

    /// <param name="deltaPx">Positivo = dedos separándose (zoom in).</param>
    protected virtual void OnPinch(float deltaPx, Vector2 centerScreenPos) { }
#endregion

#region Utilidades para las clases hijas
    /// <summary>Convierte una posicion de pantalla a mundo 2D.</summary>
    protected Vector2 ToWorld(Vector2 screenPos) => Cam.ScreenToWorldPoint(screenPos);

    /// <summary>¿Hay un collider de este objeto (o de un hijo) bajo esa posicion de pantalla?</summary>
    protected bool IsOverThisObject(Vector2 screenPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(ToWorld(screenPos));
        return hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform));
    }

    bool Accepts(Vector2 screenPos) => scope == GestureScope.Anywhere || IsOverThisObject(screenPos);
    #endregion

#region Suscripcion a eventos de GestureDetector
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

#region Filtrado por ambito
    void RouteTap(Vector2 pos) { if (Accepts(pos)) OnTap(pos); }
    void RouteDoubleTap(Vector2 pos) { if (Accepts(pos)) OnDoubleTap(pos); }
    void RouteLongPress(Vector2 pos) { if (Accepts(pos)) OnLongPress(pos); }

    /// <summary>
    /// El drag "pertenece" al receptor sobre el que empezo, aunque luego el dedo salga de el.
    /// </summary>
    /// <param name="startPos"></param>
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