
using System;
using UnityEngine;

public class ReactorNetworkSync : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ClienteWebSocket clienteWebSocket;
    [SerializeField] private ReactorCentral reactor;
    [SerializeField] private EventoEspecialUI interfazEvento;

    [Serializable]
    private class ReactorMessage
    {
        public string type;
        public string roomCode;
        public int energiaRestante;
        public int overloadDuration;
    }

    private void OnEnable()
    {
        if (clienteWebSocket != null)
        {
            clienteWebSocket.MensajeRecibido += HandleMessage;
        }
    }

    private void OnDisable()
    {
        if (clienteWebSocket != null)
        {
            clienteWebSocket.MensajeRecibido -= HandleMessage;
        }
    }

    private void HandleMessage(string json)
    {
        ReactorMessage message;

        try
        {
            message = JsonUtility.FromJson<ReactorMessage>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Error al leer el estado del reactor: " +
                ex.Message);

            return;
        }

        if (message == null)
            return;

        if (message.roomCode != ContextoPartida.CodigoSala)
            return;

        if (message.type == "reactor_overload_started")
        {
            if (reactor != null)
                reactor.EstablecerSobrecarga(true);

            if (interfazEvento != null)
                interfazEvento.MostrarSobrecargaReactor(
                    message.overloadDuration > 0 ? message.overloadDuration : 10);

            return;
        }

        if (message.type == "reactor_overload_ended")
        {
            if (reactor != null)
                reactor.EstablecerSobrecarga(false);

            if (interfazEvento != null)
                interfazEvento.MostrarFinSobrecargaReactor();

            return;
        }

        bool isEnergyUpdate =
            message.type == "extraction_updated" ||
            message.type == "reactor_recharged" ||
            message.type == "reactor_state";

        if (!isEnergyUpdate)
            return;

        if (reactor != null)
        {
            reactor.ActualizarEnergia(
                message.energiaRestante);
        }
    }
}
