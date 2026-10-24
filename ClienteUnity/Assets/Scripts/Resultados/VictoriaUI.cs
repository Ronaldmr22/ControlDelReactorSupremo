using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoriaUI : MonoBehaviour
{
    [SerializeField] private ClienteResultadoPartida cliente;
    [SerializeField] private TMP_Text txtTitulo;
    [SerializeField] private TMP_Text txtNombreGanador;
    [SerializeField] private TMP_Text txtPuntos;
    [SerializeField] private Image imagenGanador;
    [SerializeField] private Sprite[] personajes;

    [SerializeField] private int idPartidaPrueba;

    private bool consultando;

    private void Start()
    {
        Time.timeScale = 1f;

        if (cliente == null || txtTitulo == null ||
            txtNombreGanador == null || txtPuntos == null ||
            imagenGanador == null)
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


    // Permite probar desde el menú del componente durante Play.
    [ContextMenu("Consultar resultado de prueba")]
    public void ConsultarResultadoPrueba()
    {
        if (!Application.isPlaying || consultando)
            return;

        if (cliente == null || txtTitulo == null ||
            txtNombreGanador == null || txtPuntos == null ||
            imagenGanador == null)
        {
            Debug.LogError("Faltan referencias en VictoriaUI.", this);
            return;
        }

        StartCoroutine(CargarResultado(idPartidaPrueba));
    }

    private IEnumerator CargarResultado(int idPartida)
    {
        consultando = true;
        txtTitulo.text = "Consultando resultado...";
        txtNombreGanador.text = "";
        txtPuntos.text = "";
        imagenGanador.enabled = false;

        yield return cliente.ConsultarResultado(
            idPartida,
            MostrarResultado,
            MostrarError);

        consultando = false;
    }
    private void MostrarResultado(DatosResultadoPartida resultado)
    {
        var ganador = resultado.ganador;

        txtTitulo.text = "¡Felicidades al ganador!";
        txtNombreGanador.text = ganador.nombreUsuario;
        txtPuntos.text = $"{ganador.puntaje} puntos";

        // Busca el personaje del ganador.
        int indice = ganador.avatarSeleccionado - 1;

        if (personajes != null &&
            indice >= 0 &&
            indice < personajes.Length &&
            personajes[indice] != null)
        {
            imagenGanador.sprite = personajes[indice];
            imagenGanador.preserveAspect = true;
            imagenGanador.enabled = true;
        }
        else
        {
            imagenGanador.enabled = false;
            Debug.LogWarning("No se encontró el avatar del ganador.");
        }
    }


    private void MostrarError(string mensaje)
    {
        txtTitulo.text = "Resultado no disponible";
        txtNombreGanador.text = mensaje;
        txtPuntos.text = "";
        imagenGanador.enabled = false;
        Debug.LogWarning(mensaje, this);
    }
}
