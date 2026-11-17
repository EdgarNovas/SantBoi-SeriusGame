using UnityEngine;

/// <summary>
/// Hace que este objeto se pueda arrastrar con el dedo.
/// Necesita un Collider2D. El drag solo empieza si el dedo toca el objeto.
/// Sobrescribe OnDropped() en una clase hija si quieres reaccionar al soltar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DraggableObject : GestureReceiver
{
    Vector2 offset;

    protected override void OnDragStart(Vector2 screenPos)
    {
        // Guardamos la distancia dedo-objeto para que no "salte" al centrarse bajo el dedo
        offset = (Vector2)transform.position - ToWorld(screenPos);
    }

    protected override void OnDrag(Vector2 screenPos, Vector2 deltaPx)
    {
        Vector2 target = ToWorld(screenPos) + offset;
        transform.position = new Vector3(target.x, target.y, transform.position.z);
    }

    protected override void OnDragEnd(Vector2 screenPos)
    {
        OnDropped(ToWorld(screenPos));
    }

    /// <summary>Se llama al soltar el objeto. Pensado para sobrescribirlo.</summary>
    protected virtual void OnDropped(Vector2 worldPos) { }
}