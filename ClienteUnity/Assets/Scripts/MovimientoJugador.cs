using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class MovimientoJugador : MonoBehaviour
{
    [SerializeField] private float velocidad = 3f;

    [SerializeField] private Sprite abajo;
    [SerializeField] private Sprite arriba;
    [SerializeField] private Sprite izquierda;
    [SerializeField] private Sprite derecha;

    private Rigidbody2D cuerpo;
    private SpriteRenderer imagen;
    private Vector2 movimiento;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        imagen = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Lee únicamente la cruceta del control.
        movimiento = Gamepad.current != null
            ? Gamepad.current.dpad.ReadValue()
            : Vector2.zero;

        movimiento = Vector2.ClampMagnitude(movimiento, 1f);

        if (movimiento == Vector2.zero)
            return;

        Sprite siguiente;

        // Muestra la dirección principal del movimiento.
        if (Mathf.Abs(movimiento.x) > Mathf.Abs(movimiento.y))
            siguiente = movimiento.x > 0 ? derecha : izquierda;
        else
            siguiente = movimiento.y > 0 ? arriba : abajo;

        if (siguiente != null)
            imagen.sprite = siguiente;
    }

    private void FixedUpdate()
    {
        cuerpo.linearVelocity = movimiento * velocidad;
    }

    private void OnDisable()
    {
        movimiento = Vector2.zero;

        if (cuerpo != null)
            cuerpo.linearVelocity = Vector2.zero;
    }
}
