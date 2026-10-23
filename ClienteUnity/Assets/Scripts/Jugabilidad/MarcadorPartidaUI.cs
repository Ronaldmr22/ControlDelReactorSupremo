using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarcadorPartidaUI : MonoBehaviour
{
    [Header("Conexión")]
    [SerializeField] private ClienteWebSocket clienteWebSocket;

    [Header("Marcador (hasta 3 jugadores)")]
    [SerializeField] private TMP_Text[] textosJugadores;

    private readonly Dictionary<int, int> puntajes = new();

    [Serializable]
    private class MensajePuntaje
    {
        public string type;
        public int idUsuario;
        public int puntaje;
    }

    private void OnEnable()
    {
        if (clienteWebSocket != null)
            clienteWebSocket.MensajeRecibido += ProcesarMensaje;
    }

    private void Start()
    {
        InicializarMarcador();
    }

    private void OnDisable()
    {
        if (clienteWebSocket != null)
            clienteWebSocket.MensajeRecibido -= ProcesarMensaje;
    }

    private void InicializarMarcador()
    {
        puntajes.Clear();

        var participantes = ContextoPartida.Participantes;

        if (participantes == null)
        {
            Debug.LogWarning("No hay participantes para el marcador.");
            return;
        }

        foreach (var participante in participantes)
            puntajes[participante.idUsuario] = 0;

        ActualizarMarcador();
    }

    private void ProcesarMensaje(string json)
    {
        var mensaje = JsonUtility.FromJson<MensajePuntaje>(json);

        if (mensaje.type != "score_updated")
            return;

        if (!puntajes.ContainsKey(mensaje.idUsuario))
            return;

        puntajes[mensaje.idUsuario] = mensaje.puntaje;
        ActualizarMarcador();
    }

    private void ActualizarMarcador()
    {
        var participantes = ContextoPartida.Participantes;

        if (participantes == null)
            return;

        for (int i = 0; i < textosJugadores.Length; i++)
        {
            if (textosJugadores[i] == null)
                continue;

            if (i >= participantes.Length)
            {
                textosJugadores[i].gameObject.SetActive(false);
                continue;
            }

            textosJugadores[i].gameObject.SetActive(true);

            var jugador = participantes[i];

            int puntaje = puntajes.TryGetValue(
                jugador.idUsuario, out int valor) ? valor : 0;

            textosJugadores[i].text =
                $"{jugador.nombreUsuario}: {puntaje} pts";
        }
    }
}
