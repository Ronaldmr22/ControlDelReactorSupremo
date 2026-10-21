using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform jugador;

    private Vector3 desplazamiento;

    private void Awake()
    {
        desplazamiento = new Vector3(0f, 0f, transform.position.z);
    }

    public void AsignarJugador(Transform nuevoJugador)
    {
        jugador = nuevoJugador;
    }

    private void LateUpdate()
    {
        if (jugador == null)
        {
            GeneradorJugadores generador =
                FindAnyObjectByType<GeneradorJugadores>();

            if (generador != null && generador.JugadorLocal != null)
            {
                AsignarJugador(generador.JugadorLocal);
            }
        }

        if (jugador == null)
            return;

        transform.position = new Vector3(
            jugador.position.x,
            jugador.position.y,
            desplazamiento.z
        );
    }
}