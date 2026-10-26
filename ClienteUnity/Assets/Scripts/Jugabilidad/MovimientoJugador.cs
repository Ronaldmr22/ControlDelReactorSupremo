using System;
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
    private MensajeSobreAvatar mensaje;

    public event Action AccionEjecutada;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        imagen = GetComponent<SpriteRenderer>();
        mensaje = GetComponent<MensajeSobreAvatar>();
    }

    public void ConfigurarSprites(
    Sprite spriteAbajo,
    Sprite spriteArriba,
    Sprite spriteIzquierda,
    Sprite spriteDerecha)
    {
        abajo = spriteAbajo;
        arriba = spriteArriba;
        izquierda = spriteIzquierda;
        derecha = spriteDerecha;

        if (imagen == null)
            imagen = GetComponent<SpriteRenderer>();

        imagen.sprite = abajo;
    }

    private void Update()
    {
        Gamepad control = Gamepad.current;

        // Movimiento con la cruceta.
        movimiento = control != null
            ? control.dpad.ReadValue()
            : Vector2.zero;

        movimiento = Vector2.ClampMagnitude(movimiento, 1f);

        // Acción con el botón X del control.
        if (control != null && control.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("Acción ejecutada");

            if (mensaje != null)
                mensaje.Mostrar("Acción");
            else
                Debug.LogWarning("Falta MensajeSobreAvatar en el personaje.");

            // Notificar al sistema multijugador.
            AccionEjecutada?.Invoke();
        }

        ActualizarDireccionVisual(movimiento);
    }
    public void ActualizarDireccionVisual(Vector2 direccion)
    {
        // Al detenerse conserva la última orientación.
        if (direccion.sqrMagnitude < 0.000001f)
            return;

        if (imagen == null)
            imagen = GetComponent<SpriteRenderer>();

        Sprite siguiente;

        if (Mathf.Abs(direccion.x) > Mathf.Abs(direccion.y))
            siguiente = direccion.x > 0 ? derecha : izquierda;
        else
            siguiente = direccion.y > 0 ? arriba : abajo;

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

