using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class VictoriaUI : MonoBehaviour
{
    [SerializeField] private ClienteResultadoPartida cliente;
    [SerializeField] private AnimacionPodio animacionPodio;
    [SerializeField] private TMP_Text txtTitulo;
    [SerializeField] private TMP_Text txtDuracionPartida;
    [SerializeField] private TMP_Text txtParticipantes;
    [SerializeField] private Sprite[] personajes;

    [Header("Podio")]
    [SerializeField] private GameObject primerLugar;
    [SerializeField] private GameObject segundoLugar;
    [SerializeField] private GameObject tercerLugar;

    [Header("Primer lugar")]
    [SerializeField] private Image avatarPrimerLugar;
    [SerializeField] private TMP_Text nombrePrimerLugar;
    [SerializeField] private TMP_Text puntosPrimerLugar;

    [Header("Segundo lugar")]
    [SerializeField] private Image avatarSegundoLugar;
    [SerializeField] private TMP_Text nombreSegundoLugar;
    [SerializeField] private TMP_Text puntosSegundoLugar;

    [Header("Tercer lugar")]
    [SerializeField] private Image avatarTercerLugar;
    [SerializeField] private TMP_Text nombreTercerLugar;
    [SerializeField] private TMP_Text puntosTercerLugar;


    [SerializeField] private int idPartidaPrueba;

    private bool consultando;

    private void Start()
    {
        Time.timeScale = 1f;

        if (cliente == null || txtTitulo == null)
        {
            Debug.LogError("Faltan referencias en VictoriaUI.", this);
            return;
        }

        int idPartida = ContextoPartida.IdPartida;

        Debug.Log("VICTORIA - ID PARTIDA: " + idPartida);

        if (idPartida <= 0)
        {
            MostrarError("No se encontró la partida actual.");
            return;
        }

        StartCoroutine(CargarResultado(idPartida));
    }

    // Permite probar desde el menú del componente durante Play
    [ContextMenu("Consultar resultado de prueba")]
    public void ConsultarResultadoPrueba()
    {
        if (!Application.isPlaying || consultando)
            return;

        if (cliente == null || txtTitulo == null)
        {
            Debug.LogError("Faltan referencias en VictoriaUI.", this);
            return;
        }

        StartCoroutine(CargarResultado(idPartidaPrueba));
    }
    private IEnumerator CargarResultado(int idPartida)
    {
        consultando = true;

        primerLugar.SetActive(false);
        segundoLugar.SetActive(false);
        tercerLugar.SetActive(false);

        txtTitulo.text = "Consultando resultado...";
        txtDuracionPartida.text = "Duración: --:--";
        txtParticipantes.text = "Participantes: --";

        yield return cliente.ConsultarResultado(
            idPartida,
            MostrarResultado,
            MostrarError);

        consultando = false;
    }

    private void MostrarResultado(DatosResultadoPartida resultado)
    {
        MostrarPodio(resultado.clasificacion);
        if (animacionPodio != null)
        {
            animacionPodio.ReproducirAnimacion();
        }

        txtTitulo.text = "¡Podio final!";
        int minutos = resultado.duracionSegundos / 60;
        int segundos = resultado.duracionSegundos % 60;

        txtDuracionPartida.text = $"Duración: {minutos:00}:{segundos:00}";
        int cantidadParticipantes = resultado.clasificacion?.Length ?? 0;

        txtParticipantes.text = $"Participantes: {cantidadParticipantes}";

        if (resultado.clasificacion != null)
        {
            foreach (var jugador in resultado.clasificacion)
            {
                Debug.Log(
                    $"Posición {jugador.posicion}: " +
                    $"{jugador.nombreUsuario} - " +
                    $"{jugador.puntaje} puntos"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "No se recibió la clasificación de la partida.");
        }
    }

    private void MostrarPodio(JugadorClasificacion[] clasificacion)
    {
        // Ocultar todos los puestos inicialmente.
        primerLugar.SetActive(false);
        segundoLugar.SetActive(false);
        tercerLugar.SetActive(false);

        if (clasificacion == null || clasificacion.Length == 0)
        {
            Debug.LogWarning("No hay jugadores para mostrar en el podio.");
            return;
        }

        foreach (var jugador in clasificacion)
        {
            GameObject puesto = null;
            Image avatar = null;
            TMP_Text nombre = null;
            TMP_Text puntos = null;

            switch (jugador.posicion)
            {
                case 1:
                    puesto = primerLugar;
                    avatar = avatarPrimerLugar;
                    nombre = nombrePrimerLugar;
                    puntos = puntosPrimerLugar;
                    break;

                case 2:
                    puesto = segundoLugar;
                    avatar = avatarSegundoLugar;
                    nombre = nombreSegundoLugar;
                    puntos = puntosSegundoLugar;
                    break;

                case 3:
                    puesto = tercerLugar;
                    avatar = avatarTercerLugar;
                    nombre = nombreTercerLugar;
                    puntos = puntosTercerLugar;
                    break;

                default:
                    continue;
            }

            puesto.SetActive(true);

            nombre.text = jugador.nombreUsuario;
            puntos.text = $"{jugador.puntaje} puntos";

            // Los IDs de avatar van del 1 al 5.
            int indiceAvatar = jugador.avatarSeleccionado - 1;

            if (personajes != null &&
                indiceAvatar >= 0 &&
                indiceAvatar < personajes.Length &&
                personajes[indiceAvatar] != null)
            {
                avatar.sprite = personajes[indiceAvatar];
                avatar.preserveAspect = true;
                avatar.enabled = true;
            }
            else
            {
                avatar.enabled = false;
                Debug.LogWarning(
                    $"Avatar inválido para {jugador.nombreUsuario}: " +
                    jugador.avatarSeleccionado);
            }
        }
    }
    private void MostrarError(string mensaje)
    {
        txtTitulo.text = "Resultado no disponible";
        txtDuracionPartida.text = "Duración: --:--";
        txtParticipantes.text = "Participantes: --";

        primerLugar.SetActive(false);
        segundoLugar.SetActive(false);
        tercerLugar.SetActive(false);

        Debug.LogWarning(mensaje, this);
    }

    public void VolverAlLobby()
    {
        ContextoPartida.Limpiar();
        SceneManager.LoadScene("Lobby");
    }
}
