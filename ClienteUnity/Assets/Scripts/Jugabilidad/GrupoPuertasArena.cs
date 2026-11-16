using UnityEngine;

public class GrupoPuertasArena : MonoBehaviour
{
    [SerializeField] private ObstaculoArena[] puertas;
    [SerializeField] private ClienteWebSocket cliente;

    [System.Serializable]
    private class MensajePuertas
    {
        public string type;
        public string roomCode;
        public bool solicitarCierrePuertas;
        public int[] puertasCerradas;
    }

    private void OnEnable()
    {
        if (cliente != null)
            cliente.MensajeRecibido += RecibirMensajePuertas;
    }

    private void OnDisable()
    {
        if (cliente != null)
            cliente.MensajeRecibido -= RecibirMensajePuertas;
    }

    private void RecibirMensajePuertas(string json)
    {
        var mensaje = JsonUtility.FromJson<MensajePuertas>(json);

        if (mensaje == null || mensaje.type != "doors_state"
            || mensaje.puertasCerradas == null || puertas == null)
            return;

        for (int i = 0; i < puertas.Length; i++)
        {
            if (puertas[i] == null)
                continue;

            bool cerrar = System.Array.IndexOf(mensaje.puertasCerradas, i) >= 0;
            puertas[i].SolicitarEstado(cerrar);
        }
    }

    [ContextMenu("Prueba/Abrir todas")]
    public void AbrirTodas() => CambiarEstado(false);

    [ContextMenu("Prueba/Cerrar todas")]
    public void CerrarTodas() => CambiarEstado(true);

    private void CambiarEstado(bool cerrar)
    {
        if (!Application.isPlaying || puertas == null)
            return;

        foreach (ObstaculoArena puerta in puertas)
        {
            if (puerta != null)
                puerta.SolicitarEstado(cerrar);
        }
    }

    [ContextMenu("Mostrar coordenadas de puertas")]
    private void MostrarCoordenadasPuertas()
    {
        for (int i = 0; i < puertas.Length; i++)
        {
            if (puertas[i] == null)
                continue;

            var collider = puertas[i].GetComponent<BoxCollider2D>();
            if (collider == null || !collider.isActiveAndEnabled)
            {
                Debug.LogWarning($"Puerta {i}: el collider debe estar activo.", puertas[i]);
                continue;
            }

            var area = collider.bounds;
            Debug.Log(
                $"Puerta {i} ({puertas[i].name}): " +
                $"X de {area.min.x:F3} a {area.max.x:F3}, " +
                $"Y de {area.min.y:F3} a {area.max.y:F3}",
                puertas[i]);
        }
    }
}