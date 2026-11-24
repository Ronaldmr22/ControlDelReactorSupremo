using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrapCeldaElectrica : MonoBehaviour
{
    [SerializeField] private float duracion = 4f;

    private GeneradorJugadores generador;
    private MovimientoJugador jugadorAtrapado;
    private Coroutine penalizacion;

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

        if (contactos.Count > 1 || penalizacion != null)
            return;

        jugadorAtrapado = jugador;

        penalizacion = StartCoroutine(InmovilizarJugador());
    }

    private IEnumerator InmovilizarJugador()
    {
        jugadorAtrapado.EstablecerCeldaElectrica(true);

        yield return new WaitForSeconds(duracion);

        LiberarJugador();

        penalizacion = null;
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        contactos.Remove(otro);
    }

    private void LiberarJugador()
    {
        if (jugadorAtrapado != null)
        {
            jugadorAtrapado.EstablecerCeldaElectrica(false);
        }

        jugadorAtrapado = null;
    }

    private void OnDisable()
    {
        if (penalizacion != null)
        {
            StopCoroutine(penalizacion);
            penalizacion = null;
        }

        LiberarJugador();
        contactos.Clear();
    }
}
