using System;
using System.Collections.Generic;
using UnityEngine;

public class ControladorGuardianes : MonoBehaviour
{
    [Header("Conexión WebSocket")]
    [SerializeField] private ClienteWebSocket clienteWebSocket;
    [Header("Guardianes")]
    [SerializeField] private GameObject prefabGuardian;
    [Header("Jugador local")]
    [SerializeField] private GeneradorJugadores generador;
    [Header("Interfaz de eventos especiales (CanvasMapa)")]
    [SerializeField] private EventoEspecialUI interfazEvento;

    private readonly Dictionary<int, GameObject> guardianes = new();
    private readonly Dictionary<int, Vector2> destinosGuardianes = new();
    private float ultimoMensajeGuardianes = -1f;

    private void OnEnable()
    {
        if (clienteWebSocket != null)
        {
            clienteWebSocket.MensajeRecibido += RecibirMensaje;
        }
    }

    private void OnDisable()
    {
        if (clienteWebSocket != null)
        {
            clienteWebSocket.MensajeRecibido -= RecibirMensaje;
        }
    }

    private void RecibirMensaje(string json)
    {
        MensajeGuardianes mensaje =
            JsonUtility.FromJson<MensajeGuardianes>(json);

        if (mensaje == null)
            return;

        if (mensaje.type == "guardian_surge_started")
        {
            if (interfazEvento != null)
                interfazEvento.MostrarOleadaGuardianes(
                    mensaje.guardianSurgeDuration > 0 ? mensaje.guardianSurgeDuration : 10);
            else
                Debug.LogWarning("Falta asignar EventoEspecialUI en ControladorGuardianes.");

            return;
        }

        if (mensaje.type == "guardian_surge_ended")
        {
            if (interfazEvento != null)
                interfazEvento.MostrarFinOleadaGuardianes();

            return;
        }

        if (mensaje != null && mensaje.type == "player_frozen")
        {
            MostrarDistanciasAlCongelar();
            return;
        }

        if (mensaje == null ||
            mensaje.type != "guardians_updated" ||
            mensaje.guardians == null)
        {
            return;
        }
        float ahora = Time.realtimeSinceStartup;

        if (ultimoMensajeGuardianes >= 0f)
        {
            float diferencia = ahora - ultimoMensajeGuardianes;

            if (diferencia > 0.3f)
            {
                Debug.LogWarning(
                    $"RETRASO GUARDIANES UNITY: {diferencia:F3} segundos"
                );
            }
        }

        ultimoMensajeGuardianes = ahora;

        if (prefabGuardian == null)
        {
            Debug.LogWarning("No se asignó el Prefab del guardián.");
            return;
        }

        var idsEnServidor = new HashSet<int>();

        foreach (var datos in mensaje.guardians)
        {
            idsEnServidor.Add(datos.id);
            Vector3 posicion = new Vector3(
                datos.x,
                datos.y,
                0f
            );

            // Si el guardián todavía no existe, crearlo.
            if (!guardianes.TryGetValue(datos.id, out GameObject objeto))
            {
                objeto = Instantiate(
                    prefabGuardian,
                    posicion,
                    Quaternion.identity,
                    transform
                );

                objeto.name = "Guardian_" + datos.id;

                guardianes.Add(datos.id, objeto);
                destinosGuardianes[datos.id] = new Vector2(datos.x, datos.y);
            }
            else
            {
                // Guardar la posición recibida desde el backend.
                // Todavía no mover físicamente al guardián.
                destinosGuardianes[datos.id] = new Vector2(
                    datos.x,
                    datos.y
                );
            }
        }

        // El servidor es la autoridad: los cinco guardianes temporales deben
        // desaparecer de Unity cuando ya no figuren en guardians_updated.
        var idsEliminados = new List<int>();

        foreach (int id in guardianes.Keys)
        {
            if (!idsEnServidor.Contains(id))
                idsEliminados.Add(id);
        }

        foreach (int id in idsEliminados)
        {
            if (guardianes.TryGetValue(id, out GameObject objeto))
                Destroy(objeto);

            guardianes.Remove(id);
            destinosGuardianes.Remove(id);
        }

    }
    public bool GuardianEstaCerca(int idGuardian, Vector2 posicionJugador, float distanciaMaxima = 0.45f)
    {
        if (!guardianes.TryGetValue(idGuardian, out GameObject guardian))
            return false;

        Rigidbody2D rb = guardian.GetComponent<Rigidbody2D>();

        if (rb == null)
            return false;

        float distancia = Vector2.Distance(
            rb.position,
            posicionJugador
        );

        return distancia <= distanciaMaxima;
    }
    private void MostrarDistanciasAlCongelar()
    {
        if (generador == null || generador.JugadorLocal == null)
        {
            Debug.LogWarning("No se encontró el jugador local.");
            return;
        }

        Vector2 posicionJugador = generador.JugadorLocal.position;

        Debug.LogWarning(
            $"ATAQUE - Jugador Unity: {posicionJugador}"
        );

        foreach (var guardian in guardianes)
        {
            int id = guardian.Key;
            GameObject objeto = guardian.Value;

            Rigidbody2D rb = objeto.GetComponent<Rigidbody2D>();

            if (rb == null)
                continue;

            Vector2 posicionFisica = rb.position;

            float distanciaReal = Vector2.Distance(
                posicionJugador,
                posicionFisica
            );

            if (destinosGuardianes.TryGetValue(id, out Vector2 posicionBackend))
            {
                float distanciaBackend = Vector2.Distance(
                    posicionJugador,
                    posicionBackend
                );

                Debug.LogWarning(
                    $"ATAQUE GUARDIAN {id} | " +
                    $"Jugador Unity: {posicionJugador} | " +
                    $"Guardian Unity: {posicionFisica} | " +
                    $"Guardian Backend: {posicionBackend} | " +
                    $"Distancia REAL: {distanciaReal:F2} | " +
                    $"Distancia con Backend: {distanciaBackend:F2}"
                );
            }
        }
    }

    private void FixedUpdate()
    {
        foreach (var guardian in guardianes)
        {
            int id = guardian.Key;
            GameObject objeto = guardian.Value;

            if (!destinosGuardianes.TryGetValue(
                id, out Vector2 destino))
            {
                continue;
            }

            MovimientoFisicoGuardian movimiento =
                objeto.GetComponent<MovimientoFisicoGuardian>();

            if (movimiento != null)
            {
                Rigidbody2D rb = objeto.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    float diferencia = Vector2.Distance(
                        rb.position,
                        destino
                    );

                    if (diferencia > 0.5f)
                    {
                        Debug.LogWarning(
                            $"GUARDIAN DESINCRONIZADO {id} | " +
                            $"Unity: {rb.position} | " +
                            $"Backend: {destino} | " +
                            $"Diferencia: {diferencia:F2}"
                        );
                    }
                }

                movimiento.MoverHacia(destino);
            }
        }
    }



    [Serializable]
    private class MensajeGuardianes
    {
        public string type;
        public int guardianSurgeDuration;
        public PosicionGuardian[] guardians;
    }

    [Serializable]
    private class PosicionGuardian
    {
        public int id;
        public float x;
        public float y;
    }
}
