
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GoogleLoginReceiver : MonoBehaviour
{
    private const string UrlBase =
        "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net";

    public string IdSolicitud { get; private set; }
    public string Verificador { get; private set; }
    public string Desafio { get; private set; }

    private Coroutine consultaActiva;

    [Serializable]
    public class SolicitudConsultaGoogle
    {
        public string idSolicitud;
        public string verificador;
    }

    [Serializable]
    public class RespuestaConsultaGoogle
    {
        public bool completado;
        public string codigo;
    }

    [Serializable]
    public class SolicitudCanjeGoogle
    {
        public string codigo;
        public string idSolicitud;
        public string verificador;
    }

    [Serializable]
    public class RespuestaCanjeGoogle
    {
        public int idUsuario;
        public string nombreUsuario;
    }

    private string ConvertirBase64Url(byte[] datos)
    {
        return Convert.ToBase64String(datos)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public void PrepararSolicitud()
    {
        DetenerConsulta();

        IdSolicitud = Guid.NewGuid().ToString("N");

        byte[] bytes = new byte[32];

        using (var generador = RandomNumberGenerator.Create())
        {
            generador.GetBytes(bytes);
        }

        Verificador = ConvertirBase64Url(bytes);

        using (var sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(
                Encoding.ASCII.GetBytes(Verificador));

            Desafio = ConvertirBase64Url(hash);
        }
    }

    public void IniciarEscucha()
    {
        DetenerConsulta();

        if (string.IsNullOrEmpty(IdSolicitud) ||
            string.IsNullOrEmpty(Verificador))
        {
            Debug.LogError("Primero debes preparar la solicitud Google.");
            return;
        }

        consultaActiva = StartCoroutine(ConsultarGoogle());
    }

    private IEnumerator ConsultarGoogle()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < 120f)
        {
            var datos = new SolicitudConsultaGoogle
            {
                idSolicitud = IdSolicitud,
                verificador = Verificador
            };

            string json = JsonUtility.ToJson(datos);

            using (UnityWebRequest request = UnityWebRequest.Post(
                UrlBase + "/api/usuarios/google-consultar",
                json,
                "application/json"))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var respuesta =
                        JsonUtility.FromJson<RespuestaConsultaGoogle>(
                            request.downloadHandler.text);

                    if (respuesta != null &&
                        respuesta.completado &&
                        !string.IsNullOrEmpty(respuesta.codigo))
                    {
                        yield return CanjearCodigo(respuesta.codigo);
                        consultaActiva = null;
                        yield break;
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "Esperando autenticación Google. HTTP: " +
                        request.responseCode);
                }
            }

            yield return new WaitForSecondsRealtime(1f);
            tiempoTranscurrido += 1f;
        }

        Debug.LogWarning("Tiempo de autenticación Google agotado.");
        consultaActiva = null;
    }

    private IEnumerator CanjearCodigo(string codigo)
    {
        var datos = new SolicitudCanjeGoogle
        {
            codigo = codigo,
            idSolicitud = IdSolicitud,
            verificador = Verificador
        };

        string json = JsonUtility.ToJson(datos);

        using (UnityWebRequest request = UnityWebRequest.Post(
            UrlBase + "/api/usuarios/google-canjear",
            json,
            "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Error al validar Google.\n" +
                    "HTTP: " + request.responseCode + "\n" +
                    "Respuesta: " + request.downloadHandler.text);

                yield break;
            }

            var respuesta =
                JsonUtility.FromJson<RespuestaCanjeGoogle>(
                    request.downloadHandler.text);

            SesionUsuario.IdUsuario = respuesta.idUsuario;
            SesionUsuario.NombreUsuario = respuesta.nombreUsuario;

            Debug.Log("Google autenticado: " + respuesta.nombreUsuario);

            SceneManager.LoadScene("Lobby");
        }
    }

    private void DetenerConsulta()
    {
        if (consultaActiva != null)
        {
            StopCoroutine(consultaActiva);
            consultaActiva = null;
        }
    }

    private void OnDestroy()
    {
        DetenerConsulta();
    }
}
