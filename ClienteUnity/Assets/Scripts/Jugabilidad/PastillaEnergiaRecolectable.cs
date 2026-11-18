using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PastillaEnergiaRecolectable : MonoBehaviour
{
    private EventoEnergiaAdicionalCliente controlador;
    private string bonusId;
    private bool solicitudEnviada;

    public void Inicializar(EventoEnergiaAdicionalCliente owner, string id)
    {
        controlador = owner;
        bonusId = id;
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        IntentarRecoger(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        IntentarRecoger(otro);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (controlador != null && controlador.EsJugadorLocal(otro))
            solicitudEnviada = false;
    }

    private void IntentarRecoger(Collider2D otro)
    {
        if (solicitudEnviada || controlador == null ||
            !controlador.EsJugadorLocal(otro))
            return;

        if (controlador.SolicitarRecoleccion(bonusId))
            solicitudEnviada = true;
    }
}
