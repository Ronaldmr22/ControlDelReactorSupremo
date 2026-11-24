
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrapCongelada : MonoBehaviour
{
    private GeneradorJugadores generador;
    private MovimientoJugador jugadorDentro;

    private readonly HashSet<Collider2D> contactos = new();

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        generador = FindFirstObjectByType<GeneradorJugadores>();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        RevisarJugador(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        RevisarJugador(otro);
    }

    private void RevisarJugador(Collider2D otro)
    {
        if (generador == null ||
            generador.JugadorLocal == null)
            return;

        var jugador =
            otro.GetComponentInParent<MovimientoJugador>();

        if (jugador == null ||
            jugador.transform != generador.JugadorLocal)
            return;

        if (!contactos.Add(otro))
            return;

        jugadorDentro = jugador;

        jugadorDentro.EstablecerTrampaCongelada(true);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (!contactos.Remove(otro))
            return;

        if (contactos.Count == 0)
        {
            QuitarEfecto();
        }
    }

    private void QuitarEfecto()
    {
        if (jugadorDentro != null)
        {
            jugadorDentro.EstablecerTrampaCongelada(false);
        }

        jugadorDentro = null;
        contactos.Clear();
    }

    private void OnDisable()
    {
        QuitarEfecto();
    }
}

