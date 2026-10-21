
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
        public float x;
        public float y;
    }

    private string miIdWebSocket;
    private float siguienteEnvio;

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
        else if (mensaje.type == "player_moved")
        {
            if (mensaje.playerId == miIdWebSocket)
                return;

            if (generador != null && generador.JugadorRemoto != null)
            {
                generador.JugadorRemoto.position = new Vector3(
                    mensaje.x,
                    mensaje.y,
                    generador.JugadorRemoto.position.z
                );
            }
        }
    }
}
