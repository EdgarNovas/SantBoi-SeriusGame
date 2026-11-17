using UnityEngine;

/// <summary>
/// Ejemplo de clase hija: solo reacciona cuando el gesto ocurre sobre ESTE objeto.
/// Necesita SpriteRenderer y Collider2D.
///   Tap        -> cambia de color
///   Doble tap  -> crece un 25 %
///   Long press -> vuelve al tamaño original
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class TapReactor : GestureReceiver
{
    SpriteRenderer sr;
    Vector3 initialScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
    }

    protected override void OnTap(Vector2 screenPos)
    {
        sr.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f);
        Debug.Log($"[{name}] TAP");
    }

    protected override void OnDoubleTap(Vector2 screenPos)
    {
        transform.localScale *= 1.25f;
        Debug.Log($"[{name}] DOBLE TAP");
    }

    protected override void OnLongPress(Vector2 screenPos)
    {
        transform.localScale = initialScale;
        Debug.Log($"[{name}] LONG PRESS");
    }
}