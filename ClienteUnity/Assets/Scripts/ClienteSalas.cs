using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ClienteSalas : MonoBehaviour
{
    [SerializeField] private string urlServidor = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net";

    public DatosSala SalaActual { get; private set; }

    [Serializable]
    private class SolicitudSala
    {
        public int idUsuario;
        public string codigoSala;
    }

    [Serializable]
    private class RespuestaError
    {
        public string mensaje;
    }

    public IEnumerator CrearSala(
        Action<DatosSala> alCompletar,
        Action<string> alError)
    {
        var datos = new SolicitudSala
        {
            idUsuario = SesionUsuario.IdUsuario
        };

        yield return Enviar("/api/salas", datos, alCompletar, alError);
    }

    public IEnumerator UnirseSala(
        string codigo,
        Action<DatosSala> alCompletar,
        Action<string> alError)
    {
        var datos = new SolicitudSala
        {
            idUsuario = SesionUsuario.IdUsuario,
            codigoSala = (codigo ?? "").Trim().ToUpperInvariant()
        };

        yield return Enviar("/api/salas/unirse", datos, alCompletar, alError);
    }

    private IEnumerator Enviar(
        string ruta,
        SolicitudSala datos,
        Action<DatosSala> alCompletar,
        Action<string> alError)
    {
        if (datos.idUsuario <= 0)
        {
            alError?.Invoke("Debes iniciar sesión primero.");
            yield break;
        }

        string json = JsonUtility.ToJson(datos);

        using var solicitud = new UnityWebRequest(
            urlServidor.TrimEnd('/') + ruta, "POST");

        solicitud.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(json));
        solicitud.downloadHandler = new DownloadHandlerBuffer();
        solicitud.SetRequestHeader("Content-Type", "application/json");
        solicitud.timeout = 20;

        yield return solicitud.SendWebRequest();

        if (solicitud.result != UnityWebRequest.Result.Success)
        {
            string mensaje = "No se pudo completar la solicitud.";

            // Recupera el mensaje que devuelve la API.
            try
            {
                var error = JsonUtility.FromJson<RespuestaError>(
                    solicitud.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(error?.mensaje))
                    mensaje = error.mensaje;
            }
            catch (Exception)
            {
                mensaje = "No se pudo interpretar la respuesta del servidor.";
            }

            if (solicitud.result == UnityWebRequest.Result.ConnectionError)
                mensaje = "No se pudo conectar con el servidor.";

            alError?.Invoke(mensaje);
            yield break;
        }

        SalaActual = JsonUtility.FromJson<DatosSala>(
            solicitud.downloadHandler.text);

        alCompletar?.Invoke(SalaActual);
    }

    public IEnumerator ConsultarSala(
        Action<DatosSala> alCompletar,
        Action<string> alError)
    {
        if (SalaActual == null)
        {
            alError?.Invoke("No tienes una sala seleccionada.");
            yield break;
        }

        string codigo = UnityWebRequest.EscapeURL(SalaActual.codigoSala);
        string url = urlServidor.TrimEnd('/') + "/api/salas/" + codigo;

        using var solicitud = UnityWebRequest.Get(url);
        solicitud.timeout = 15;

        yield return solicitud.SendWebRequest();

        if (solicitud.result != UnityWebRequest.Result.Success)
        {
            alError?.Invoke("No se pudo actualizar la información de la sala.");
            yield break;
        }

        SalaActual = JsonUtility.FromJson<DatosSala>(
            solicitud.downloadHandler.text);

        alCompletar?.Invoke(SalaActual);
    }
}