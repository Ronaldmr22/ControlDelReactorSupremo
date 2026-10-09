using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoPrueba : MonoBehaviour
{
    [SerializeField] private float velocidad = 5f;

    private Rigidbody2D cuerpo;
    private Vector2 direccion;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        direccion = Vector2.zero;

        if (teclado.upArrowKey.isPressed) direccion.y += 1;
        if (teclado.downArrowKey.isPressed) direccion.y -= 1;
        if (teclado.leftArrowKey.isPressed) direccion.x -= 1;
        if (teclado.rightArrowKey.isPressed) direccion.x += 1;
    }

    private void FixedUpdate()
    {
        cuerpo.linearVelocity = direccion.normalized * velocidad;
    }
}
