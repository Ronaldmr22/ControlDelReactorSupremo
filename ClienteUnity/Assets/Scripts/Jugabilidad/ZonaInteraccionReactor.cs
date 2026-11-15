
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZonaInteraccionReactor : MonoBehaviour
{
    [SerializeField] private GeneradorJugadores generador;
    [SerializeField] private ReactorCentral reactor;

    private MovimientoJugador jugadorEnZona;

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

        // Solo permitimos la interacción del jugador local.
        if (jugador == null ||
            jugador.transform != generador.JugadorLocal ||
            jugadorEnZona != null)
            return;

        jugadorEnZona = jugador;
        jugadorEnZona.AccionEjecutada += IntentarExtraer;

        Debug.Log("Jugador dentro de la zona del reactor.");
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        MovimientoJugador jugador =
            otro.GetComponentInParent<MovimientoJugador>();

        if (jugador == jugadorEnZona)
            DesvincularJugador();
    }

    private void IntentarExtraer()
    {
        if (reactor == null)
            return;

        if (!reactor.Disponible)
        {
            Debug.Log("El reactor no tiene energía disponible.");
            return;
        }

        Debug.Log("US-18: solicitud de extracción detectada.");

        // Próximo paso: enviar la solicitud al servidor.
    }

    private void OnDisable()
    {
        DesvincularJugador();
    }

    private void DesvincularJugador()
    {
        if (jugadorEnZona == null)
            return;

        jugadorEnZona.AccionEjecutada -= IntentarExtraer;
        jugadorEnZona = null;
    }
}
