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
    private class SolicitudConfiguracion
    {
        public int idUsuario;
        public string tipoFinalizacion;
        public int limiteTiempo;
        public int puntajeObjetivo;
        public int segundosCongelamiento;
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
    public IEnumerator RegistrarActividad(
    Action alCompletar,
    Action<string> alError)
    {
        yield return EnviarAccionSala("/api/salas/actividad", alCompletar, alError);
    }

    public IEnumerator SalirSala(
        Action alCompletar,
        Action<string> alError)
    {
        yield return EnviarAccionSala("/api/salas/salir", alCompletar, alError);
    }

    public void LimpiarSala()
    {
        SalaActual = null;
    }

    private IEnumerator EnviarAccionSala(
        string ruta,
        Action alCompletar,
        Action<string> alError)
    {
        if (SalaActual == null || SesionUsuario.IdUsuario <= 0)
        {
            alError?.Invoke("No tienes una sesión de sala activa.");
            yield break;
        }

        var datos = new SolicitudSala
        {
            idUsuario = SesionUsuario.IdUsuario,
            codigoSala = SalaActual.codigoSala
        };

        string json = JsonUtility.ToJson(datos);

        using var solicitud = new UnityWebRequest(
            urlServidor.TrimEnd('/') + ruta, "POST");

        solicitud.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(json));
        solicitud.downloadHandler = new DownloadHandlerBuffer();
        solicitud.SetRequestHeader("Content-Type", "application/json");
        solicitud.timeout = 15;

        Debug.Log("URL SOLICITADA: " + solicitud.url);
        Debug.Log("MÉTODO HTTP: " + solicitud.method);
        yield return solicitud.SendWebRequest();


        Debug.Log(
        "HTTP: " + solicitud.responseCode +
        " | Error: " + solicitud.error +
        " | Respuesta: " + solicitud.downloadHandler.text
         );

        if (solicitud.result != UnityWebRequest.Result.Success)
        {
            string mensaje = "No se pudo completar la solicitud.";

            try
            {
                var respuesta = JsonUtility.FromJson<RespuestaError>(
                    solicitud.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(respuesta?.mensaje))
                    mensaje = respuesta.mensaje;
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

        alCompletar?.Invoke();
    }

    public IEnumerator GuardarConfiguracion(
        string tipoFinalizacion,
        int minutos,
        int puntos,
        int segundosCongelamiento,
        Action alCompletar,
        Action<string> alError)
    {
        if (SalaActual == null)
        {
            alError?.Invoke("No tienes una sala activa.");
            yield break;
        }

        if (SesionUsuario.IdUsuario != SalaActual.idUsuarioCreador)
        {
            alError?.Invoke("Solo el anfitrión puede configurar la partida.");
            yield break;
        }

        string modo = tipoFinalizacion.Trim().ToUpperInvariant();

        if (modo != "TIEMPO" && modo != "PUNTOS")
        {
            alError?.Invoke("Modo de finalización inválido.");
            yield break;
        }

        var datos = new SolicitudConfiguracion
        {
            idUsuario = SesionUsuario.IdUsuario,
            tipoFinalizacion = modo,
            limiteTiempo = modo == "TIEMPO" ? minutos : 0,
            puntajeObjetivo = modo == "PUNTOS" ? puntos : 0,
            segundosCongelamiento = segundosCongelamiento
        };

        string json = JsonUtility.ToJson(datos);

        string url = urlServidor.TrimEnd('/')
            + "/api/partidas/"
            + SalaActual.idPartida
            + "/configuracion";

        using var solicitud = new UnityWebRequest(url, "PUT");

        solicitud.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(json));

        solicitud.downloadHandler = new DownloadHandlerBuffer();
        solicitud.SetRequestHeader("Content-Type", "application/json");
        solicitud.timeout = 20;

        yield return solicitud.SendWebRequest();

        if (solicitud.result != UnityWebRequest.Result.Success)
        {
            string mensaje = "No se pudo guardar la configuración.";

            try
            {
                var respuesta = JsonUtility.FromJson<RespuestaError>(
                    solicitud.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(respuesta?.mensaje))
                    mensaje = respuesta.mensaje;
            }
            catch (Exception)
            {
                // Conservamos el mensaje general.
            }

            alError?.Invoke(mensaje);
            yield break;
        }

        alCompletar?.Invoke();
        }
    public IEnumerator IniciarPartida(
    Action alCompletar,
    Action<string> alError)
    {
        yield return EnviarAccionSala(
            "/api/salas/iniciar",
            alCompletar,
            alError);
    }
}