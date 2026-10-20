using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform jugador;

    private void LateUpdate()
    {
        if (jugador == null)
            return;

        // Sigue al jugador conservando la profundidad de la cámara.
        transform.position = new Vector3(
            jugador.position.x,
            jugador.position.y,
            transform.position.z
        );
    }
}