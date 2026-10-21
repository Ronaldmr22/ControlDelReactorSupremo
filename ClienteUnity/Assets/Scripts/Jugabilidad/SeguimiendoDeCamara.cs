
using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform jugador;

    public void AsignarJugador(Transform nuevoJugador)
    {
        jugador = nuevoJugador;
    }

    private void LateUpdate()
    {
        if (jugador == null)
            return;

        transform.position = new Vector3(
            jugador.position.x,
            jugador.position.y,
            transform.position.z
        );
    }
}
