using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DraggableRotationObject : GestureReceiver
{
    [Header("Rotación")]
    [SerializeField] private bool rotateFromTop = true;

    [Tooltip("Punto de pivote en coordenadas locales del objeto.")]
    [SerializeField] private Vector2 pivotLocal = new Vector2(0f, 0.5f);

    private Vector2 previousPointer;
    private Vector2 pivotWorld;
    private float initialAngle;
    private bool rotating;

    protected override void OnDragStart(Vector2 screenPos)
    {
        Vector2 pointerWorld = ToWorld(screenPos);

        if (rotateFromTop)
        {
            rotating = true;

            // Calculamos el punto de pivote en el mundo.
            pivotWorld = transform.TransformPoint(pivotLocal);

            // Ángulo inicial entre el pivote y el dedo.
            Vector2 direction = pointerWorld - pivotWorld;
            initialAngle = Mathf.Atan2(
                direction.y, direction.x
            ) * Mathf.Rad2Deg;
        }
        else
        {
            rotating = false;
        }

        previousPointer = pointerWorld;
    }

    protected override void OnDrag(Vector2 screenPos, Vector2 deltaPx)
    {
        Vector2 pointerWorld = ToWorld(screenPos);

        if (rotating)
        {
            Vector2 direction = pointerWorld - pivotWorld;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            float currentAngle = Mathf.Atan2(
                direction.y, direction.x
            ) * Mathf.Rad2Deg;

            float deltaAngle = Mathf.DeltaAngle(
                initialAngle, currentAngle
            );

            transform.RotateAround(
                pivotWorld,
                Vector3.forward,
                deltaAngle
            );

            // Actualizamos las referencias para el siguiente movimiento.
            initialAngle = currentAngle;
            pivotWorld = transform.TransformPoint(pivotLocal);
        }
        else
        {
            transform.position += (Vector3)(
                pointerWorld - previousPointer
            );
        }

        previousPointer = pointerWorld;
    }

    protected override void OnDragEnd(Vector2 screenPos)
    {
        OnDropped(ToWorld(screenPos));
    }

    protected virtual void OnDropped(Vector2 worldPos) { }
}

