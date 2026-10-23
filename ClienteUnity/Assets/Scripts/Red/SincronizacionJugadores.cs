
using System;
using UnityEngine;

public class SincronizacionJugadores : MonoBehaviour
{
    [SerializeField] private ClienteWebSocket cliente;
    [SerializeField] private GeneradorJugadores generador;

    [Serializable]
    private class MensajeRed
    {
        public string type;
        public string playerId;
        public string roomCode;
        public int idUsuario;
        public float x;
        public float y;
    }

    private string miIdWebSocket;
    private float siguienteEnvio;
    private Vector3 ultimaPosicionEnviada;
    private bool primeraPosicion = true;

    private void OnEnable()
    {
        if (cliente != null)
            cliente.MensajeRecibido += ProcesarMensaje;
    }

    private void OnDisable()
    {
        if (cliente != null)
            cliente.MensajeRecibido -= ProcesarMensaje;
    }

    private void Update()
    {
        if (cliente == null ||
            !cliente.EstaConectado ||
            generador == null ||
            generador.JugadorLocal == null ||
            string.IsNullOrEmpty(miIdWebSocket))
            return;

        // Enviar posición 10 veces por segundo.
        if (Time.time < siguienteEnvio)
            return;

        siguienteEnvio = Time.time + 0.1f;

        Vector3 posicion = generador.JugadorLocal.position;
        if (!primeraPosicion &&
            Vector3.Distance(posicion, ultimaPosicionEnviada) < 0.01f)
            return;

        ultimaPosicionEnviada = posicion;
        primeraPosicion = false;

        cliente.EnviarMensaje(JsonUtility.ToJson(new MensajeRed
        {
            type = "move",
            x = posicion.x,
            y = posicion.y
        }));
    }

    private void ProcesarMensaje(string json)
    {
        MensajeRed mensaje = JsonUtility.FromJson<MensajeRed>(json);

        if (mensaje.type == "room_subscribed")
        {
            miIdWebSocket = mensaje.playerId;
            Debug.Log("Jugador registrado para sincronización.");
        }
        else if (mensaje.type == "player_joined")
        {
            if (mensaje.playerId == miIdWebSocket)
                return;

            if (generador != null)
                generador.CrearJugadorRemoto(
                    mensaje.playerId,
                    mensaje.idUsuario
                );
        }
        else if (mensaje.type == "player_moved")
        {
            if (mensaje.playerId == miIdWebSocket)
                return;

            if (generador == null)
                return;

            Transform remoto = generador.ObtenerJugadorRemoto(mensaje.playerId);

            if (remoto == null)
                return;

            remoto.position = new Vector3(
                mensaje.x,
                mensaje.y,
                remoto.position.z
            );
        }
        else if (mensaje.type == "player_left")
        {
            if (generador != null)
                generador.EliminarJugadorRemoto(mensaje.playerId);
        }
    }
}
