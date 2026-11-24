using System;
using System.Collections.Generic;
using UnityEngine;

public class TrampasCliente : MonoBehaviour
{
    [Header("Conexión")]
    [SerializeField] private ClienteWebSocket cliente;

    [Header("Prefabs")]
    [SerializeField] private GameObject prefabControlesInvertidos;
    [SerializeField] private GameObject prefabCeldaElectrica;
    [SerializeField] private GameObject prefabTrampaCongelada;

    private readonly List<GameObject> trampasInstanciadas = new();

    [Serializable]
    private class PosicionTrampa
    {
        public int kind;
        public float x;
        public float y;
    }

    [Serializable]
    private class MensajeTrampas
    {
        public string type;
        public string roomCode;
        public PosicionTrampa[] traps;
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
    }

    private void ProcesarMensaje(string json)
    {
        MensajeTrampas mensaje;

        try
        {
            mensaje = JsonUtility.FromJson<MensajeTrampas>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "Error al leer trampas: " + ex.Message);
            return;
        }

        if (mensaje == null || mensaje.type != "traps_spawned")
            return;

        if (mensaje.traps == null)
        {
            Debug.LogWarning(
                "El servidor no envió posiciones de trampas.");
            return;
        }

        GenerarTrampas(mensaje.traps);
    }

    private void GenerarTrampas(PosicionTrampa[] posiciones)
    {
        // Evita duplicados al recibir nuevamente el estado.
        LimpiarTrampas();

        foreach (var posicion in posiciones)
        {
        GameObject prefab = posicion.kind switch
        {
            0 => prefabCeldaElectrica,
            1 => prefabControlesInvertidos,
            2 => prefabTrampaCongelada,
            _ => null
        };

                    GameObject instancia = Instantiate(
                        prefab,
                        new Vector3(posicion.x, posicion.y, 0f),
                        Quaternion.identity,
                        transform
                    );

            trampasInstanciadas.Add(instancia);
        }

        Debug.Log(
            $"Se generaron {trampasInstanciadas.Count} de {posiciones.Length} trampas recibidas.");
    }

    private void LimpiarTrampas()
    {
        foreach (GameObject trampa in trampasInstanciadas)
        {
            if (trampa != null)
                Destroy(trampa);
        }

        trampasInstanciadas.Clear();
    }

    private void OnDestroy()
    {
        LimpiarTrampas();
    }
}