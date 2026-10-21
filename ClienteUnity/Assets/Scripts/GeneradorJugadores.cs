
using UnityEngine;

public class GeneradorJugadores : MonoBehaviour
{
    [SerializeField] private GameObject prefabJugador;
    [SerializeField] private Transform puntoJugador1;
    [SerializeField] private Transform puntoJugador2;

    public Transform JugadorLocal { get; private set; }
    public Transform JugadorRemoto { get; private set; }

    private void Start()
    {
        if (prefabJugador == null ||
            puntoJugador1 == null ||
            puntoJugador2 == null)
        {
            Debug.LogError("Faltan referencias en GeneradorJugadores.");
            return;
        }

        int idLocal = SesionUsuario.IdUsuario;

        bool soyJugador1 = idLocal == ContextoPartida.IdJugador1;
        bool soyJugador2 = idLocal == ContextoPartida.IdJugador2;

        if (!soyJugador1 && !soyJugador2)
        {
            Debug.LogError("El usuario no está asignado a un jugador de esta partida.");
            return;
        }

        Transform puntoLocal = soyJugador1 ? puntoJugador1 : puntoJugador2;

        GameObject jugador = Instantiate(
            prefabJugador,
            puntoLocal.position,
            Quaternion.identity
        );

        jugador.name = soyJugador1 ? "Jugador1_Local" : "Jugador2_Local";

        JugadorLocal = jugador.transform;

        // Crear la representación del otro jugador.
        Transform puntoRemoto = soyJugador1 ? puntoJugador2 : puntoJugador1;

        GameObject jugadorRemoto = Instantiate(
            prefabJugador,
            puntoRemoto.position,
            Quaternion.identity
        );

        jugadorRemoto.name = soyJugador1 ? "Jugador2_Remoto" : "Jugador1_Remoto";

        JugadorRemoto = jugadorRemoto.transform;

        // Evitar que el mando controle al personaje remoto.
        MovimientoJugador movimientoRemoto =
            jugadorRemoto.GetComponent<MovimientoJugador>();

        if (movimientoRemoto != null)
            movimientoRemoto.enabled = false;

        // Evitar que la física mueva al personaje remoto.
        Rigidbody2D cuerpoRemoto = jugadorRemoto.GetComponent<Rigidbody2D>();

        if (cuerpoRemoto != null)
        {
            cuerpoRemoto.linearVelocity = Vector2.zero;
            cuerpoRemoto.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log("Personaje remoto generado correctamente.");

        CamaraSeguimiento camara =
            FindAnyObjectByType<CamaraSeguimiento>();

        if (camara != null)
        {
            camara.AsignarJugador(jugador.transform);
        }
        else
        {
            Debug.LogWarning("No se encontró CamaraSeguimiento en la escena.");
        }
    }
}