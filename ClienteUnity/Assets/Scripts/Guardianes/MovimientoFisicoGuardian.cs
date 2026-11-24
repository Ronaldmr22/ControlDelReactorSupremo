using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoFisicoGuardian : MonoBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void MoverHacia(Vector2 destino)
    {
        // La posición del backend es la posición oficial.
        // No volvemos a calcular el movimiento en Unity.
        rb.position = destino;
    }
}
