using UnityEngine.InputSystem;
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

    private bool esperandoInicio;
    private bool extrayendo;
    private bool esperandoDetencion;

    private string idExtraccion;

    [Serializable]
    private class MensajeExtraccion
    {
        public string type;
        public string reactorId;
        public string extractionId;
    }

    [Serializable]
    private class RespuestaServidor
    {
        public string type;
        public string message;
    }

    private void Awake()
    {
        if (reactor == null)
            reactor = GetComponentInParent<ReactorCentral>();

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (cliente != null)
            cliente.MensajeRecibido += ProcesarRespuestaServidor;
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
        {
            return;
        }

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
        if (esperandoInicio || extrayendo || esperandoDetencion)
            return;

        if (reactor == null || !reactor.Disponible)
            return;

        if (cliente == null || !cliente.EstaConectado)
        {
            Debug.LogWarning(
                "No hay conexión con el servidor.");
            return;
        }

        idExtraccion = Guid.NewGuid().ToString("N");
        esperandoInicio = true;

        EnviarMensajeExtraccion(
            "extract_start", idExtraccion);

        Debug.Log("Solicitud de extracción enviada.");
    }

    private void DetenerExtraccion()
    {
        if (!esperandoInicio && !extrayendo)
            return;

        string extractionId = idExtraccion;

        esperandoInicio = false;
        extrayendo = false;

        if (cliente == null ||
            !cliente.EstaConectado ||
            string.IsNullOrEmpty(extractionId))
        {
            ReiniciarEstado();
            return;
        }

        esperandoDetencion = true;

        EnviarMensajeExtraccion(
            "extract_stop", extractionId);

        Debug.Log("Solicitud de detención enviada.");
    }

    private void ProcesarRespuestaServidor(string json)
    {
        RespuestaServidor respuesta;

        try
        {
            respuesta =
                JsonUtility.FromJson<RespuestaServidor>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Error interpretando respuesta del servidor: " +
                ex.Message);
            return;
        }

        if (respuesta == null ||
            string.IsNullOrEmpty(respuesta.type))
        {
            return;
        }

        switch (respuesta.type)
        {
            case "extract_started":
                if (esperandoInicio)
                {
                    esperandoInicio = false;
                    extrayendo = true;

                    Debug.Log(
                        "Extracción confirmada por el servidor.");
                }
                break;

            case "extract_stopped":
                if (esperandoInicio ||
                    extrayendo ||
                    esperandoDetencion)
                {
                    ReiniciarEstado();

                    Debug.Log(
                        "Extracción detenida: " +
                        respuesta.message);
                }
                break;

            case "error":
                if (esperandoInicio ||
                    extrayendo ||
                    esperandoDetencion)
                {
                    Debug.LogWarning(
                        "Solicitud rechazada: " +
                        respuesta.message);

                    ReiniciarEstado();
                }
                break;

            case "reactor_recharged":
                Debug.Log(
                    "El reactor volvió a estar disponible.");
                break;
        }
    }

    private void EnviarMensajeExtraccion(
        string tipo, string extractionId)
    {
        if (cliente == null || !cliente.EstaConectado)
            return;

        MensajeExtraccion mensaje =
            new MensajeExtraccion
            {
                type = tipo,
                reactorId = "central",
                extractionId = extractionId
            };

        cliente.EnviarMensaje(
            JsonUtility.ToJson(mensaje));
    }

    private void ReiniciarEstado()
    {
        esperandoInicio = false;
        extrayendo = false;
        esperandoDetencion = false;
        idExtraccion = null;
    }

    private void OnDisable()
    {
        DetenerExtraccion();

        if (cliente != null)
            cliente.MensajeRecibido -= ProcesarRespuestaServidor;

        DesvincularJugador();
        ReiniciarEstado();
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

