
using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Security.Cryptography;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GoogleLoginReceiver : MonoBehaviour
{
    private HttpListener listener;

    public string IdSolicitud { get; private set; }

    public string Verificador { get; private set; }
    public string Desafio { get; private set; }
    private string codigoPendiente;
    private readonly object bloqueoCodigo = new object();

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
        if (listener != null)
            return;

        listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:8765/");
        listener.Start();

        Debug.Log("Esperando respuesta de Google...");

        _ = EscucharRespuesta();
    }

    private async Task EscucharRespuesta()
    {
        try
        {
            HttpListenerContext contexto = await listener.GetContextAsync();

            string codigo = contexto.Request.QueryString["code"];

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                lock (bloqueoCodigo)
                {
                    codigoPendiente = codigo;
                }
            }

            string mensaje = "<html><body><h2>Ya puedes regresar al juego.</h2></body></html>";
            byte[] respuesta = Encoding.UTF8.GetBytes(mensaje);

            contexto.Response.ContentType = "text/html; charset=utf-8";
            contexto.Response.ContentLength64 = respuesta.Length;

            await contexto.Response.OutputStream.WriteAsync(
                respuesta, 0, respuesta.Length);

            contexto.Response.Close();

            Debug.Log("Unity recibió la respuesta del navegador.");

            // Todavía no utilizamos el código.
            // Falta validarlo con el backend.
        }
        catch (Exception ex)
        {
            Debug.LogError("Error al recibir Google: " + ex.Message);
        }
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

        string url = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/google-canjear";

        using (UnityWebRequest request =
            UnityWebRequest.Post(url, json, "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al validar Google: " +
                               request.downloadHandler.text);
                yield break;
            }

            var respuesta = JsonUtility.FromJson<RespuestaCanjeGoogle>(
                request.downloadHandler.text);

            SesionUsuario.IdUsuario = respuesta.idUsuario;
            SesionUsuario.NombreUsuario = respuesta.nombreUsuario;

            Debug.Log("Google autenticado: " + respuesta.nombreUsuario);
        }
    }

    private void Update()
    {
        string codigo = null;

        lock (bloqueoCodigo)
        {
            if (codigoPendiente != null)
            {
                codigo = codigoPendiente;
                codigoPendiente = null;
            }
        }

        if (!string.IsNullOrEmpty(codigo))
        {
            StartCoroutine(CanjearCodigo(codigo));
        }
    }

    private void OnDestroy()
    {
        if (listener != null)
        {
            listener.Stop();
            listener.Close();
            listener = null;
        }
    }
}
