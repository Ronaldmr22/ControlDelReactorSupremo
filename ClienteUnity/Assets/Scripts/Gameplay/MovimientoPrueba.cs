using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoPrueba : MonoBehaviour
{
    [SerializeField] private float velocidad = 5f;

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        Vector2 direccion = Vector2.zero;

        if (teclado.upArrowKey.isPressed) direccion.y += 1;
        if (teclado.downArrowKey.isPressed) direccion.y -= 1;
        if (teclado.leftArrowKey.isPressed) direccion.x -= 1;
        if (teclado.rightArrowKey.isPressed) direccion.x += 1;

        transform.Translate(direccion * velocidad * Time.deltaTime);
    }
}
