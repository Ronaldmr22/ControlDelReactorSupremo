using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ArenaMovedizaZona : MonoBehaviour
{
    private EventosCreativosCliente controlador;
    private bool jugadorDentro;

    public void Inicializar(EventosCreativosCliente owner)
    {
        controlador = owner;
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro) => RevisarJugador(otro);
    private void OnTriggerStay2D(Collider2D otro) => RevisarJugador(otro);

    private void RevisarJugador(Collider2D otro)
    {
        if (jugadorDentro || controlador == null || !controlador.EsJugadorLocal(otro))
            return;

        jugadorDentro = true;
        controlador.EntrarArena(this);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (!jugadorDentro || controlador == null || !controlador.EsJugadorLocal(otro))
            return;

        jugadorDentro = false;
        controlador.SalirArena(this);
    }

    private void OnDisable()
    {
        if (!jugadorDentro)
            return;

        jugadorDentro = false;
        controlador?.SalirArena(this);
    }
}
