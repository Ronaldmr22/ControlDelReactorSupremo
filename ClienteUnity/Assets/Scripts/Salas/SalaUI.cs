using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SalaUI : MonoBehaviour
{
    [SerializeField] private ClienteSalas cliente;
    [SerializeField] private GameObject pantallaLobby;
    [SerializeField] private GameObject pantallaUnirse;
    [SerializeField] private GameObject pantallaSala;

    [SerializeField] private GameObject pantallaConfiguracion;
    [SerializeField] private ConfiguracionPartidaUI configuracionPartida;
    [SerializeField] private Button botonGuardarConfiguracion;

    [SerializeField] private TMP_InputField inputCodigo;
    [SerializeField] private TMP_Text mensajeLobby;
    [SerializeField] private TMP_Text mensajeUnirse;
    [SerializeField] private TMP_Text textoCodigoSala;
    [SerializeField] private TMP_Text textoEstadoSala;

    [SerializeField] private JugadorSalaUI[] espaciosJugadores;
    [SerializeField] private Button botonCrear;
    [SerializeField] private Button botonUnirse;
    [SerializeField] private Button botonIniciar;
    [SerializeField] private GameObject botonConfiguracion;

    private bool ocupado;
    private Coroutine actualizacionSala;
    private bool entrandoPartida;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    public void CrearSala()
    {
        if (ocupado)
            return;

        mensajeLobby.text = "Creando sala...";
        CambiarOcupado(true);

        StartCoroutine(cliente.CrearSala(
            SalaRecibida,
            error =>
            {
                mensajeLobby.text = error;
                CambiarOcupado(false);
            }));
    }

    public void UnirseSala()
    {
        if (ocupado)
            return;

        if (string.IsNullOrWhiteSpace(inputCodigo.text))
        {
            mensajeUnirse.text = "Escribe el código de sala.";
            return;
        }

        mensajeUnirse.text = "Buscando sala...";
        CambiarOcupado(true);

        StartCoroutine(cliente.UnirseSala(
            inputCodigo.text,
            SalaRecibida,
            error =>
            {
                mensajeUnirse.text = error;
                CambiarOcupado(false);
            }));
    }

    private void SalaRecibida(DatosSala sala)
    {
        CambiarOcupado(false);
        mensajeLobby.text = "";
        mensajeUnirse.text = "";

        // Activa la pantalla antes de cargar las fotos.
        pantallaSala.SetActive(true);
        pantallaLobby.SetActive(false);
        pantallaUnirse.SetActive(false);

        MostrarSala(sala);
        if (actualizacionSala != null)
            StopCoroutine(actualizacionSala);

        actualizacionSala = StartCoroutine(ActualizarSalaPeriodicamente());
    }

    public void MostrarSala(DatosSala sala)
    {
        textoCodigoSala.text = "Código de sala: " + sala.codigoSala;

        int cantidad = sala.participantes.Length;
        bool soyCreador =
            sala.idUsuarioCreador == SesionUsuario.IdUsuario;

        for (int i = 0; i < espaciosJugadores.Length; i++)
        {
            if (i < cantidad)
            {
                var participante = sala.participantes[i];

                espaciosJugadores[i].Mostrar(
                    participante,
                    participante.idUsuario == sala.idUsuarioCreador);
            }
            else
            {
                espaciosJugadores[i].MostrarVacio();
            }
        }

        textoEstadoSala.text = cantidad < 2
            ? $"Esperando jugador... ({cantidad}/3)"
            : $"Participantes en la sala: {cantidad}/3";

        botonConfiguracion.SetActive(true);

        botonIniciar.interactable = soyCreador &&
        cantidad >= 2 &&
        sala.estado == "ESPERANDO" &&
        !ocupado &&
        !entrandoPartida;
    }

    private void CambiarOcupado(bool valor)
    {
        ocupado = valor;
        botonCrear.interactable = !valor;
        botonUnirse.interactable = !valor;
    }

    public void SalirSala()
    {
        if (ocupado)
            return;

        CambiarOcupado(true);
        textoEstadoSala.text = "Saliendo de la sala...";

        if (actualizacionSala != null)
        {
            StopCoroutine(actualizacionSala);
            actualizacionSala = null;
        }

        StartCoroutine(cliente.SalirSala(
            () => VolverAlLobby(""),
            error =>
            {
                CambiarOcupado(false);
                textoEstadoSala.text = error;

                actualizacionSala =
                    StartCoroutine(ActualizarSalaPeriodicamente());
            }));
    }

    private void VolverAlLobby(string mensaje)
    {
        cliente.LimpiarSala();

        if (actualizacionSala != null)
        {
            StopCoroutine(actualizacionSala);
            actualizacionSala = null;
        }

        pantallaLobby.SetActive(true);
        pantallaSala.SetActive(false);
        pantallaUnirse.SetActive(false);
        pantallaConfiguracion.SetActive(false);

        mensajeLobby.text = mensaje;
        CambiarOcupado(false);
    }

    private IEnumerator ActualizarSalaPeriodicamente()
    {
        string ultimoContenido = "";

        while (cliente.SalaActual != null && !entrandoPartida)
        {
            yield return new WaitForSecondsRealtime(5f);

            if (cliente.SalaActual == null || entrandoPartida)
                yield break;

            bool salaEnEspera = false;

            yield return cliente.ConsultarSala(
                sala =>
                {
                    Debug.Log("ESTADO RECIBIDO DE AZURE: " + sala.estado);

                    if (sala.estado == "EN_CURSO")
                    {
                        Debug.Log("Partida en curso. Cargando Mapa1...");
                        EntrarPartida(sala);
                        return;
                    }

                    if (sala.estado == "CANCELADA")
                    {
                        VolverAlLobby("El creador cerró la sala.");
                        return;
                    }

                    bool sigoEnSala = System.Array.Exists(
                        sala.participantes,
                        jugador => jugador.idUsuario == SesionUsuario.IdUsuario);

                    if (sala.estado == "ESPERANDO" && !sigoEnSala)
                    {
                        VolverAlLobby(
                            "Tu conexión expiró. Puedes volver a unirte.");
                        return;
                    }

                    salaEnEspera = sala.estado == "ESPERANDO";

                    string contenido = JsonUtility.ToJson(sala);

                    if (pantallaSala.activeInHierarchy &&
                        contenido != ultimoContenido)
                    {
                        MostrarSala(sala);
                        ultimoContenido = contenido;
                    }
                },
                error =>
                {
                    textoEstadoSala.text = error;
                    ultimoContenido = "";
                });

            if (!salaEnEspera ||
                cliente.SalaActual == null ||
                entrandoPartida)
            {
                continue;
            }

            yield return cliente.RegistrarActividad(
                () => { },
                error => textoEstadoSala.text = error);
        }
    }
    public void IniciarPartida()
    {
        Debug.Log("BOTÓN INICIAR PARTIDA PRESIONADO");

        if (ocupado || entrandoPartida)
            return;

        DatosSala sala = cliente.SalaActual;

        if (sala == null)
            return;

        bool soyCreador =
            sala.idUsuarioCreador == SesionUsuario.IdUsuario;

        if (!soyCreador || sala.participantes == null ||
            sala.participantes.Length < 2)
            return;

        CambiarOcupado(true);
        botonIniciar.interactable = false;
        textoEstadoSala.text = "Iniciando partida...";

        StartCoroutine(cliente.IniciarPartida(
        () =>
        {
            Debug.Log("AZURE: Partida iniciada correctamente");
            textoEstadoSala.text = "Esperando confirmación...";
            CambiarOcupado(false);
        },
        error =>
        {
            Debug.LogError("AZURE: Error al iniciar partida: " + error);
            textoEstadoSala.text = error;
            CambiarOcupado(false);
            MostrarSala(cliente.SalaActual);
        }));
    }

    private void EntrarPartida(DatosSala sala)
    {
        if (entrandoPartida)
            return;

        entrandoPartida = true;

        if (actualizacionSala != null)
        {
            StopCoroutine(actualizacionSala);
            actualizacionSala = null;
        }

        ContextoPartida.Guardar(sala);

        Debug.Log("===== JUGADORES DE LA PARTIDA =====");
        Debug.Log("Jugador 1: " + ContextoPartida.IdJugador1);
        Debug.Log("Jugador 2: " + ContextoPartida.IdJugador2);
        Debug.Log("Jugador 3: " + ContextoPartida.IdJugador3);
        Debug.Log("Mi usuario: " + SesionUsuario.IdUsuario);
        Debug.Log("==================================");

        SceneManager.LoadScene("Mapa1");
    }


    public void GuardarConfiguracion()
    {
        if (ocupado || cliente.SalaActual == null)
            return;

        if (configuracionPartida == null || botonGuardarConfiguracion == null)
        {
            Debug.LogError("Faltan referencias para guardar configuración.");
            return;
        }

        configuracionPartida.LimpiarError();

        string modo = configuracionPartida.ObtenerTipoFinalizacion();
        int minutos = configuracionPartida.ObtenerDuracion();
        int puntos = configuracionPartida.ObtenerPuntaje();
        int segundos = configuracionPartida.ObtenerTiempoParalisis();

        Debug.Log($"Guardando: {modo}, {minutos} min, {puntos} puntos, {segundos} s");

        CambiarOcupado(true);
        botonGuardarConfiguracion.interactable = false;

        StartCoroutine(cliente.GuardarConfiguracion(
            modo,
            minutos,
            puntos,
            segundos,
            () =>
            {
                StartCoroutine(cliente.ConsultarSala(
                    sala =>
                    {
                        CambiarOcupado(false);
                        botonGuardarConfiguracion.interactable = true;

                        // Activar la sala antes de actualizar los jugadores.
                        pantallaConfiguracion.SetActive(false);
                        pantallaSala.SetActive(true);

                        MostrarSala(sala);

                        Debug.Log("Configuración guardada correctamente.");
                    },
                    error =>
                    {
                        CambiarOcupado(false);
                        botonGuardarConfiguracion.interactable = true;

                        configuracionPartida.MostrarError(
                            "Se guardó la configuración, pero no se pudo actualizar la sala: " + error);

                        Debug.LogError("Error al consultar la sala: " + error);
                    }));
            },
            error =>
            {
                CambiarOcupado(false);
                botonGuardarConfiguracion.interactable = true;
                configuracionPartida.MostrarError(error);
                Debug.LogError("Error al guardar configuración: " + error);
            }));
    }

    public void AbrirConfiguracion()
    {
        if (ocupado || cliente.SalaActual == null)
            return;

        StartCoroutine(cliente.ConsultarSala(
            sala =>
            {
                if (sala.estado != "ESPERANDO")
                {
                    Debug.LogWarning(
                        "No se puede acceder a la configuración porque la partida ya comenzó."
                    );

                    textoEstadoSala.text =
                        "No se puede modificar la configuración porque la partida ya comenzó.";

                    return;
                }

                configuracionPartida?.LimpiarError();
                pantallaSala.SetActive(false);
                pantallaConfiguracion.SetActive(true);
            },
            error =>
            {
                textoEstadoSala.text = error;
            }));
    }

}