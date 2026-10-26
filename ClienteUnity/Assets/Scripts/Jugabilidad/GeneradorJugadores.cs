using UnityEngine;
using System.Collections.Generic;

public class GeneradorJugadores : MonoBehaviour
{

    [System.Serializable]
    private class SpritesPersonaje
    {
        public Sprite abajo;
        public Sprite arriba;
        public Sprite izquierda;
        public Sprite derecha;
    }

    [SerializeField] private SpritesPersonaje[] personajes;
    [SerializeField] private GameObject prefabJugador;
    [SerializeField] private Transform puntoJugador1;
    [SerializeField] private Transform puntoJugador2;
    [SerializeField] private Transform puntoJugador3;

    public Transform JugadorLocal { get; private set; }
    private readonly Dictionary<string, Transform> jugadoresRemotos = new();

    public Transform ObtenerJugadorRemoto(string playerId)
    {
        jugadoresRemotos.TryGetValue(playerId, out Transform jugador);
        return jugador;
    }

    private void Start()
    {
        if (prefabJugador == null ||
            puntoJugador1 == null ||
            puntoJugador2 == null ||
            puntoJugador3 == null)
        {
            Debug.LogError("Faltan referencias en GeneradorJugadores.");
            return;
        }

        int idLocal = SesionUsuario.IdUsuario;

        bool soyJugador1 = idLocal == ContextoPartida.IdJugador1;
        bool soyJugador2 = idLocal == ContextoPartida.IdJugador2;
        bool soyJugador3 = idLocal == ContextoPartida.IdJugador3;

        if (!soyJugador1 && !soyJugador2 && !soyJugador3)
        {
            Debug.LogError("El usuario no está asignado a un jugador de esta partida.");
            return;
        }
        Transform puntoLocal = soyJugador1 ? puntoJugador1 : soyJugador2 ? puntoJugador2 : puntoJugador3;

        GameObject jugador = Instantiate(
            prefabJugador,
            puntoLocal.position,
            Quaternion.identity
        );

        jugador.name = soyJugador1 ? "Jugador1_Local" : soyJugador2 ? "Jugador2_Local" : "Jugador3_Local";

        JugadorLocal = jugador.transform;
        AplicarPersonaje(jugador, idLocal);

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

    public void CrearJugadorRemoto(string playerId, int idUsuario)
    {
        if (string.IsNullOrEmpty(playerId) ||
            jugadoresRemotos.ContainsKey(playerId) ||
            prefabJugador == null)
            return;

        Transform puntoAparicion;

        if (idUsuario == ContextoPartida.IdJugador1)
            puntoAparicion = puntoJugador1;
        else if (idUsuario == ContextoPartida.IdJugador2)
            puntoAparicion = puntoJugador2;
        else if (idUsuario == ContextoPartida.IdJugador3)
            puntoAparicion = puntoJugador3;
        else
        {
            Debug.LogWarning("Usuario remoto no reconocido: " + idUsuario);
            return;
        }

        GameObject jugador = Instantiate(
            prefabJugador,
            puntoAparicion.position,
            Quaternion.identity
        );

        jugador.name = "JugadorRemoto_" + idUsuario;
        AplicarPersonaje(jugador, idUsuario);

        MovimientoJugador movimiento = jugador.GetComponent<MovimientoJugador>();

        if (movimiento != null)
            movimiento.enabled = false;

        Rigidbody2D cuerpo = jugador.GetComponent<Rigidbody2D>();

        if (cuerpo != null)
        {
            cuerpo.linearVelocity = Vector2.zero;
            cuerpo.bodyType = RigidbodyType2D.Kinematic;
        }

        jugadoresRemotos.Add(playerId, jugador.transform);

        Debug.Log("Jugador remoto creado: " + idUsuario);
    }


    public void EliminarJugadorRemoto(string playerId)
    {
        if (!jugadoresRemotos.TryGetValue(playerId, out Transform jugador))
            return;

        jugadoresRemotos.Remove(playerId);

        if (jugador != null)
            Destroy(jugador.gameObject);

        Debug.Log("Jugador remoto desconectado: " + playerId);
    }
    private void AplicarPersonaje(GameObject jugador, int idUsuario)
    {
        var participantes = ContextoPartida.Participantes;

        if (participantes == null)
        {
            Debug.LogError("No se recibieron los participantes de la partida.");
            return;
        }

        var participante = System.Array.Find(
            participantes,
            p => p != null && p.idUsuario == idUsuario);

        if (participante == null)
        {
            Debug.LogError("No se encontró al usuario: " + idUsuario);
            return;
        }

        int indice = participante.avatarSeleccionado - 1;

        if (personajes == null || indice < 0 ||
            indice >= personajes.Length || personajes[indice] == null)
        {
            Debug.LogError("Avatar no configurado: " +
                participante.avatarSeleccionado);
            return;
        }

        var sprites = personajes[indice];

        if (sprites.abajo == null || sprites.arriba == null ||
            sprites.izquierda == null || sprites.derecha == null)
        {
            Debug.LogError("Faltan sprites del avatar: " +
                participante.avatarSeleccionado);
            return;
        }

        var movimiento = jugador.GetComponent<MovimientoJugador>();

        if (movimiento == null)
        {
            Debug.LogError("El prefab no tiene MovimientoJugador.");
            return;
        }

        // Asigna las cuatro direcciones del avatar recibido del backend.
        movimiento.ConfigurarSprites(
            sprites.abajo,
            sprites.arriba,
            sprites.izquierda,
            sprites.derecha);
    }
}