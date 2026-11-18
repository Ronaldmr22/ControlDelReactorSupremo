using System;
using System.Collections;
using UnityEngine;

// Representa la cápsula y solicita su recolección; nunca asigna puntos localmente.
public class EventoEnergiaAdicionalCliente : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ClienteWebSocket cliente;
    [SerializeField] private GeneradorJugadores generador;
    [SerializeField] private EventoEspecialUI interfazEvento;
    [SerializeField] private GameObject prefabPastilla;

    private GameObject pastillaActual;
    private string bonusActual;
    private Coroutine desaparicionLocal;

    [Serializable]
    private class MensajeBonus
    {
        public string type;
        public string roomCode;
        public string bonusId;
        public float x;
        public float y;
        public int bonusDuration;
    }

    [Serializable]
    private class SolicitudRecoleccion
    {
        public string type;
        public string bonusId;
    }

    private void OnEnable()
    {
        if (cliente != null)
            cliente.MensajeRecibido += ProcesarMensaje;
    }

    private void OnDisable()
    {
        if (cliente != null)
            cliente.MensajeRecibido -= ProcesarMensaje;

        QuitarPastilla();
    }

    private void ProcesarMensaje(string json)
    {
        MensajeBonus mensaje;

        try
        {
            mensaje = JsonUtility.FromJson<MensajeBonus>(json);
        }
        catch (Exception)
        {
            return;
        }

        if (mensaje == null || !string.Equals(
                mensaje.roomCode,
                ContextoPartida.CodigoSala,
                StringComparison.OrdinalIgnoreCase))
            return;

        if (mensaje.type == "energy_bonus_spawned")
        {
            if (mensaje.bonusId == bonusActual)
                return;

            if (prefabPastilla == null)
            {
                Debug.LogError("No se asignó el prefab de la pastilla.", this);
                return;
            }

            QuitarPastilla();

            Vector3 posicion = new Vector3(
                mensaje.x,
                mensaje.y,
                prefabPastilla.transform.position.z);

            pastillaActual = Instantiate(prefabPastilla, posicion, Quaternion.identity);
            bonusActual = mensaje.bonusId;

            var recolectable = pastillaActual.GetComponent<PastillaEnergiaRecolectable>();

            if (recolectable == null)
            {
                Debug.LogError("El prefab necesita PastillaEnergiaRecolectable.", this);
                QuitarPastilla();
                return;
            }

            recolectable.Inicializar(this, bonusActual);
            interfazEvento?.MostrarEnergiaAdicional();

            // Respaldo local: la cápsula no queda visible si se corta la conexión.
            desaparicionLocal = StartCoroutine(
                QuitarAlExpirar(mensaje.bonusDuration, bonusActual));
        }
        else if ((mensaje.type == "energy_bonus_collected" ||
                  mensaje.type == "energy_bonus_removed") &&
                 mensaje.bonusId == bonusActual)
        {
            QuitarPastilla();
        }
    }

    public bool EsJugadorLocal(Collider2D otro)
    {
        if (generador == null || generador.JugadorLocal == null)
            return false;

        MovimientoJugador jugador = otro.GetComponentInParent<MovimientoJugador>();

        return jugador != null && jugador.transform == generador.JugadorLocal;
    }

    public bool SolicitarRecoleccion(string bonusId)
    {
        if (cliente == null || !cliente.EstaConectado ||
            string.IsNullOrEmpty(bonusId) || bonusId != bonusActual)
            return false;

        cliente.EnviarMensaje(JsonUtility.ToJson(new SolicitudRecoleccion
        {
            type = "energy_bonus_collect",
            bonusId = bonusId
        }));

        return true;
    }

    private void QuitarPastilla()
    {
        if (desaparicionLocal != null)
        {
            StopCoroutine(desaparicionLocal);
            desaparicionLocal = null;
        }

        if (pastillaActual != null)
            Destroy(pastillaActual);

        pastillaActual = null;
        bonusActual = null;
    }

    private IEnumerator QuitarAlExpirar(int segundos, string id)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0, segundos));

        if (bonusActual == id)
        {
            desaparicionLocal = null;
            QuitarPastilla();
        }
    }
}
