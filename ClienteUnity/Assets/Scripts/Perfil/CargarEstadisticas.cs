using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;
using System.Collections;
using System.Text;

public class CargarEstadisticas : MonoBehaviour
{
    [Header("Estadísticas")]
    [SerializeField] private TMP_Text txtPartidasJugadas;
    [SerializeField] private TMP_Text txtPartidasGanadas;
    [SerializeField] private TMP_Text txtPartidasPerdidas;
    [SerializeField] private TMP_Text txtLogrosObtenidos;

    [Header("Historial")]
    [SerializeField] private TMP_Text txtHistorialPartidas;

    private const string API_URL =
        "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/";

    [Serializable]
    private class PartidaDto
    {
        public int idPartida;
        public string fechaInicio;
        public string fechaFin;
        public int puntaje;
        public bool esGanador;
    }

    [Serializable]
    private class EstadisticasDto
    {
        public int partidasJugadas;
        public int partidasGanadas;
        public int partidasPerdidas;
        public int puntosTotales;
        public int posicionRanking;
        public int logrosObtenidos;
        public PartidaDto[] historialPartidas;
    }

    private void OnEnable()
    {
        if (SesionUsuario.IdUsuario <= 0)
        {
            Debug.LogWarning("No hay un usuario con sesión iniciada.");
            return;
        }

        StartCoroutine(ObtenerEstadisticas());
    }

    private IEnumerator ObtenerEstadisticas()
    {
        string url = API_URL + SesionUsuario.IdUsuario + "/estadisticas";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Error al cargar estadísticas: " +
                    request.responseCode + " - " +
                    request.error
                );

                yield break;
            }

            EstadisticasDto datos =
                JsonUtility.FromJson<EstadisticasDto>(
                    request.downloadHandler.text
                );

            if (datos == null)
            {
                Debug.LogError("No se pudieron interpretar las estadísticas.");
                yield break;
            }

            // Estadísticas generales
            txtPartidasJugadas.text =
                "Partidas Jugadas: " + datos.partidasJugadas;

            txtPartidasGanadas.text =
                "Partidas Ganadas: " + datos.partidasGanadas;

            txtPartidasPerdidas.text =
                "Partidas Perdidas: " + datos.partidasPerdidas;

            txtLogrosObtenidos.text =
                "Logros Obtenidos: " + datos.logrosObtenidos;

            // Historial de partidas
            MostrarHistorial(datos.historialPartidas);

            Debug.Log("Estadísticas e historial cargados desde Azure.");
        }
    }

    private void MostrarHistorial(PartidaDto[] partidas)
    {
        if (txtHistorialPartidas == null)
        {
            Debug.LogWarning("No se asignó el texto del historial.");
            return;
        }

        if (partidas == null || partidas.Length == 0)
        {
            txtHistorialPartidas.text =
                "Todavía no has jugado ninguna partida.";

            return;
        }

        StringBuilder historial = new StringBuilder();

        foreach (PartidaDto partida in partidas)
        {
            string resultado = partida.esGanador
                ? "<color=#65FF9B>VICTORIA</color>"
                : "<color=#FF7777>DERROTA</color>";

            string fecha = partida.fechaFin;

            if (DateTime.TryParse(partida.fechaFin, out DateTime fechaPartida))
            {
                fecha = fechaPartida.ToString("dd/MM/yyyy");
            }

            historial.AppendLine(
                $"<b>PARTIDA #{partida.idPartida}</b>      {resultado}"
            );

            historial.AppendLine(
                $"{fecha}                  {partida.puntaje} puntos"
            );

            historial.AppendLine(
                "<color=#7893B8>────────────────────────────</color>"
            );

            historial.AppendLine();
        }

        txtHistorialPartidas.text = historial.ToString();
    }
}