
using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZonaInteraccionReactor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GeneradorJugadores generador;
    [SerializeField] private ReactorCentral reactor;
    [SerializeField] private ClienteWebSocket cliente;

    private MovimientoJugador jugadorEnZona;
    private bool extrayendo;
    private string idExtraccion;

    [Serializable]
    private class MensajeExtraccion
    {
        public string type;
        public string reactorId;
        public string extractionId;
    }

    private void Awake()
    {
        if (reactor == null)
            reactor = GetComponentInParent<ReactorCentral>();

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (generador == null || generador.JugadorLocal == null)
            return;

        MovimientoJugador jugador =
            otro.GetComponentInParent<MovimientoJugador>();

        if (jugador == null ||
            jugador.transform != generador.JugadorLocal ||
            jugadorEnZona != null)
            return;

        jugadorEnZona = jugador;

        jugadorEnZona.AccionEjecutada += IniciarExtraccion;
        jugadorEnZona.AccionFinalizada += DetenerExtraccion;
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        MovimientoJugador jugador =
            otro.GetComponentInParent<MovimientoJugador>();

        if (jugador != jugadorEnZona)
            return;

        DetenerExtraccion();
        DesvincularJugador();
    }

    private void IniciarExtraccion()
    {
        if (extrayendo || reactor == null || !reactor.Disponible)
            return;

        if (cliente == null || !cliente.EstaConectado)
        {
            Debug.LogWarning("No hay conexion con el servidor.");
            return;
        }

        extrayendo = true;
        idExtraccion = Guid.NewGuid().ToString("N");

        EnviarMensajeExtraccion("extract_start");

        Debug.Log("Solicitud de inicio de extraccion enviada.");
    }

    private void DetenerExtraccion()
    {
        if (!extrayendo)
            return;

        extrayendo = false;

        EnviarMensajeExtraccion("extract_stop");

        idExtraccion = null;

        Debug.Log("Solicitud de fin de extraccion enviada.");
    }

    private void EnviarMensajeExtraccion(string tipo)
    {
        if (cliente == null || !cliente.EstaConectado)
            return;

        MensajeExtraccion mensaje = new MensajeExtraccion
        {
            type = tipo,
            reactorId = "central",
            extractionId = idExtraccion
        };

        cliente.EnviarMensaje(JsonUtility.ToJson(mensaje));
    }

    private void OnDisable()
    {
        DetenerExtraccion();
        DesvincularJugador();
    }

    private void DesvincularJugador()
    {
        if (jugadorEnZona == null)
            return;

        jugadorEnZona.AccionEjecutada -= IniciarExtraccion;
        jugadorEnZona.AccionFinalizada -= DetenerExtraccion;

        jugadorEnZona = null;
    }
}
