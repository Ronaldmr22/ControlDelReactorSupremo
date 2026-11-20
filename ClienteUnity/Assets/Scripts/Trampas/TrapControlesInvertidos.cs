using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrampaControlesInvertidos : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float duracion = 5f;

    private bool activada;
    private BoxCollider2D zona;

    private void Awake()
    {
        zona = GetComponent<BoxCollider2D>();
        zona.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (activada)
            return;

        MovimientoJugador jugador =
            otro.GetComponentInParent<MovimientoJugador>();

        if (jugador == null)
            return;

        // Solo el jugador local debe modificar sus controles.
        if (!jugador.enabled)
            return;

        activada = true;

        StartCoroutine(InvertirTemporalmente(jugador));
    }

    private IEnumerator InvertirTemporalmente(
        MovimientoJugador jugador)
    {
        jugador.EstablecerControlesInvertidos(true);

        Debug.Log("¡Controles invertidos!");

        yield return new WaitForSecondsRealtime(duracion);

        if (jugador != null)
            jugador.EstablecerControlesInvertidos(false);

        activada = false;
    }
}
