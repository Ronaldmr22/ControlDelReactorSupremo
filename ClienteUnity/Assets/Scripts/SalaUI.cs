using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SalaUI : MonoBehaviour
{
    [SerializeField] private ClienteSalas cliente;
    [SerializeField] private GameObject pantallaLobby;
    [SerializeField] private GameObject pantallaUnirse;
    [SerializeField] private GameObject pantallaSala;

    [SerializeField] private GameObject pantallaConfiguracion;

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

        botonConfiguracion.SetActive(soyCreador);

        // Se habilitará cuando implementemos el inicio en el servidor.
        botonIniciar.interactable = false;
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

        while (cliente.SalaActual != null)
        {
            yield return new WaitForSecondsRealtime(5f);

            if (cliente.SalaActual == null)
                yield break;

            // Mantiene la presencia incluso mientras editamos la configuración.
            yield return cliente.RegistrarActividad(
                () => { },
                error => textoEstadoSala.text = error);

            if (cliente.SalaActual == null)
                yield break;

            yield return cliente.ConsultarSala(
                sala =>
                {
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
        }
    }
}