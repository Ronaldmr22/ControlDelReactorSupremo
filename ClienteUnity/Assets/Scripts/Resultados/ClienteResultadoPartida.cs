using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ClienteResultadoPartida : MonoBehaviour
{
    [SerializeField]
    private string urlServidor ="https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net";

    public IEnumerator ConsultarResultado(
        int idPartida,
        Action<DatosResultadoPartida> alCompletar,
        Action<string> alError)
    {
        if (idPartida <= 0)
        {
            alError?.Invoke("El identificador de la partida no es válido.");
            yield break;
        }

        string url = urlServidor.TrimEnd('/') +
            $"/api/partidas/{idPartida}/resultado";

        using var solicitud = UnityWebRequest.Get(url);
        solicitud.timeout = 20;

        yield return solicitud.SendWebRequest();

        if (solicitud.result != UnityWebRequest.Result.Success)
        {
            string mensaje;

            if (solicitud.result == UnityWebRequest.Result.ConnectionError)
                mensaje = "No se pudo conectar con el servidor.";
            else if (solicitud.responseCode == 404)
                mensaje = "No se encontró el resultado o la ruta aún no está disponible.";
            else
                mensaje = $"No se pudo consultar el resultado. HTTP {solicitud.responseCode}.";

            alError?.Invoke(mensaje);
            yield break;
        }

        DatosResultadoPartida resultado;

        // Convierte el JSON al formato que acordamos.
        try
        {
            resultado = JsonUtility.FromJson<DatosResultadoPartida>(
                solicitud.downloadHandler.text);
        }
        catch (Exception)
        {
            alError?.Invoke("El servidor devolvió una respuesta inválida.");
            yield break;
        }

        if (resultado == null || resultado.idPartida != idPartida)
        {
            alError?.Invoke("La respuesta no corresponde a la partida solicitada.");
            yield break;
        }

        if (resultado.estado != "FINALIZADA")
        {
            alError?.Invoke("La partida todavía no ha finalizado.");
            yield break;
        }

        if (resultado.ganador == null)
        {
            alError?.Invoke("El resultado no incluye un ganador.");
            yield break;
        }

        alCompletar?.Invoke(resultado);
    }
}

