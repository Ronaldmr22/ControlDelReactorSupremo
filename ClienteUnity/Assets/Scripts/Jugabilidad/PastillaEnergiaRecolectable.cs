using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PastillaEnergiaRecolectable : MonoBehaviour
{
    private EventoEnergiaAdicionalCliente controlador;
    private string bonusId;

    private float siguienteIntento;

    private const float IntervaloIntento = 0.25f;

    public void Inicializar(
        EventoEnergiaAdicionalCliente owner,
        string id)
    {
        controlador = owner;
        bonusId = id;
        siguienteIntento = 0f;

        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Debug.Log(
            "[US22] Collider detectado: " + otro.name);

        IntentarRecoger(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        IntentarRecoger(otro);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (controlador != null &&
            controlador.EsJugadorLocal(otro))
        {
            siguienteIntento = 0f;
        }
    }

    private void IntentarRecoger(Collider2D otro)
    {
        if (controlador == null ||
            !controlador.EsJugadorLocal(otro))
        {
            return;
        }

        if (Time.unscaledTime < siguienteIntento)
            return;

        siguienteIntento =
            Time.unscaledTime + IntervaloIntento;

        if (controlador.SolicitarRecoleccion(bonusId))
        {
            Debug.Log(
                "[US22] Solicitud de recolección enviada.");
        }
    }
}