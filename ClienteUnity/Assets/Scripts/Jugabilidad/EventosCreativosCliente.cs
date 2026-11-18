using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventosCreativosCliente : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ClienteWebSocket cliente;
    [SerializeField] private GeneradorJugadores generador;
    [SerializeField] private EventoEspecialUI interfazEvento;
    [SerializeField] private ReactorCentral reactor;
    [SerializeField] private GameObject prefabArenaMovediza;

    [Header("Arena movediza (4 zonas de 2 x 2 baldosas)")]
    [SerializeField] private float escalaInstancia = 2f;

    [Serializable]
    private class ZonaRed
    {
        public float x;
        public float y;
    }

    [Serializable]
    private class MensajeCreativo
    {
        public string type;
        public string roomCode;
        public int creativeDuration;
        public ZonaRed[] sandZones;
    }

    private readonly List<GameObject> arenas = new();
    private readonly HashSet<ArenaMovedizaZona> zonasPisadas = new();
    private MovimientoJugador jugadorRalentizado;
    private Coroutine respaldoArena;
    private Coroutine respaldoPuntosDobles;
    private bool arenasActivas;
    private bool doblesActivos;

    private void OnEnable()
    {
        if (cliente != null)
            cliente.MensajeRecibido += ProcesarMensaje;
    }

    private void OnDisable()
    {
        if (cliente != null)
            cliente.MensajeRecibido -= ProcesarMensaje;

        TerminarArenas(false);
        TerminarPuntosDobles(false);
    }

    private void ProcesarMensaje(string json)
    {
        MensajeCreativo mensaje;
        try
        {
            mensaje = JsonUtility.FromJson<MensajeCreativo>(json);
        }
        catch (Exception)
        {
            return;
        }

        if (mensaje == null || !string.Equals(mensaje.roomCode,
                ContextoPartida.CodigoSala, StringComparison.OrdinalIgnoreCase))
            return;

        switch (mensaje.type)
        {
            case "creative_sand_started":
                IniciarArenas(mensaje);
                break;
            case "creative_sand_ended":
                TerminarArenas(true);
                break;
            case "reactor_double_started":
                IniciarPuntosDobles(mensaje.creativeDuration);
                break;
            case "reactor_double_ended":
                TerminarPuntosDobles(true);
                break;
        }
    }

    private void IniciarArenas(MensajeCreativo mensaje)
    {
        if (arenasActivas)
            return;

        if (prefabArenaMovediza == null ||
            mensaje.sandZones == null || mensaje.sandZones.Length != 4)
        {
            Debug.LogError("CA-22.4: faltó el prefab o las 4 posiciones de arena.", this);
            return;
        }

        arenasActivas = true;
        foreach (var zona in mensaje.sandZones)
        {
            var instancia = Instantiate(prefabArenaMovediza,
                new Vector3(zona.x, zona.y, prefabArenaMovediza.transform.position.z),
                Quaternion.identity);

            instancia.transform.localScale = new Vector3(
                escalaInstancia, escalaInstancia, 1f);

            var collider = instancia.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = instancia.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            var zonaScript = instancia.GetComponent<ArenaMovedizaZona>();
            if (zonaScript == null)
                zonaScript = instancia.AddComponent<ArenaMovedizaZona>();
            zonaScript.Inicializar(this);

            arenas.Add(instancia);
        }

        int duracion = mensaje.creativeDuration > 0 ? mensaje.creativeDuration : 20;
        interfazEvento?.MostrarArenasMovedizas(duracion);
        respaldoArena = StartCoroutine(ExpirarArenas(duracion));
    }

    private IEnumerator ExpirarArenas(int segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        respaldoArena = null;
        TerminarArenas(false);
    }

    private void TerminarArenas(bool mostrarAviso)
    {
        bool estabaActivo = arenasActivas;
        arenasActivas = false;

        if (respaldoArena != null)
        {
            StopCoroutine(respaldoArena);
            respaldoArena = null;
        }

        zonasPisadas.Clear();
        if (jugadorRalentizado != null)
            jugadorRalentizado.EstablecerArenaMovediza(false);
        jugadorRalentizado = null;

        foreach (var arena in arenas)
            if (arena != null) Destroy(arena);
        arenas.Clear();

        if (estabaActivo && mostrarAviso)
            interfazEvento?.MostrarFinArenasMovedizas();
    }

    private void IniciarPuntosDobles(int segundos)
    {
        if (doblesActivos)
            return;

        doblesActivos = true;
        reactor?.EstablecerPuntosDobles(true);
        int duracion = segundos > 0 ? segundos : 15;
        interfazEvento?.MostrarPuntosDobles(duracion);
        respaldoPuntosDobles = StartCoroutine(ExpirarPuntosDobles(duracion));
    }

    private IEnumerator ExpirarPuntosDobles(int segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        respaldoPuntosDobles = null;
        TerminarPuntosDobles(false);
    }

    private void TerminarPuntosDobles(bool mostrarAviso)
    {
        bool estabaActivo = doblesActivos;
        doblesActivos = false;

        if (respaldoPuntosDobles != null)
        {
            StopCoroutine(respaldoPuntosDobles);
            respaldoPuntosDobles = null;
        }

        reactor?.EstablecerPuntosDobles(false);

        if (estabaActivo && mostrarAviso)
            interfazEvento?.MostrarFinPuntosDobles();
    }

    public bool EsJugadorLocal(Collider2D otro)
    {
        if (generador == null || generador.JugadorLocal == null)
            return false;

        var jugador = otro.GetComponentInParent<MovimientoJugador>();
        return jugador != null && jugador.transform == generador.JugadorLocal;
    }

    public void EntrarArena(ArenaMovedizaZona zona)
    {
        if (!arenasActivas || !zonasPisadas.Add(zona))
            return;

        if (generador != null && generador.JugadorLocal != null)
        {
            jugadorRalentizado = generador.JugadorLocal.GetComponent<MovimientoJugador>();
            jugadorRalentizado?.EstablecerArenaMovediza(true);
        }
    }

    public void SalirArena(ArenaMovedizaZona zona)
    {
        zonasPisadas.Remove(zona);
        if (zonasPisadas.Count == 0 && jugadorRalentizado != null)
        {
            jugadorRalentizado.EstablecerArenaMovediza(false);
            jugadorRalentizado = null;
        }
    }
}
