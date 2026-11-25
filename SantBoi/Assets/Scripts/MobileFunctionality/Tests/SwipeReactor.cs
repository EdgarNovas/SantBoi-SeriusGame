using UnityEngine;

/// <summary>
/// Script de prueba para el swipe. Hereda de GestureReceiver.
/// Necesita SpriteRenderer y Collider2D. Solo reacciona a swipes que EMPIEZAN sobre este objeto
/// (con Scope = Anywhere reaccionaría a cualquier swipe de la pantalla).
///
/// Al hacer swipe, el objeto se desliza un paso en esa dirección y cambia de color:
///   Derecha -> rojo | Arriba -> verde | Izquierda -> azul | Abajo -> amarillo
///
/// No lo combines en el mismo objeto con DraggableObject: los dos usan el mismo drag.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class SwipeReactor : GestureReceiver
{
    [Header("Swipe")]
    [Tooltip("Distancia (unidades de mundo) que se desplaza el objeto en cada swipe.")]
    [SerializeField] float stepDistance = 2f;

    [Tooltip("Velocidad del desplazamiento (unidades por segundo).")]
    [SerializeField] float moveSpeed = 12f;

    [Tooltip("Grados alrededor de cada diagonal en los que el swipe se ignora. 0 = nunca se ignora ninguno.")]
    [Range(0f, 44f)]
    [SerializeField] float diagonalDeadZoneDeg = 0f;

    // Índices: 0 = derecha, 1 = arriba, 2 = izquierda, 3 = abajo
    static readonly Vector2[] directions = { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
    static readonly string[] names = { "DERECHA", "ARRIBA", "IZQUIERDA", "ABAJO" };

    SpriteRenderer sr;
    Vector3 target;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        target = transform.position;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    protected override void OnSwipe(Vector2 startScreenPos, Vector2 direction)
    {
        // Ángulo del swipe y eje (0, 90, 180 o 270 grados) más cercano
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float nearestAxis = Mathf.Round(angle / 90f) * 90f;

        // Distancia angular al eje: 45 grados = justo en la diagonal
        if (Mathf.Abs(Mathf.DeltaAngle(angle, nearestAxis)) > 45f - diagonalDeadZoneDeg)
        {
            Debug.Log($"[{name}] SWIPE diagonal ignorado ({angle:0} grados)");
            return;
        }

        int index = ((int)(nearestAxis / 90f) % 4 + 4) % 4;

        target += (Vector3)(directions[index] * stepDistance);

        Debug.Log($"[{name}] SWIPE {names[index]} ({angle:0} grados)");
    }
}