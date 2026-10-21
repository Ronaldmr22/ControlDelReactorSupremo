
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class ReenviarCodigo : MonoBehaviour
{
    [SerializeField]
    private CodigoManager codigoManager;
    [SerializeField]
    private TMP_Text mensajeEstado;

    [SerializeField]
    private GameObject pantallaLogin;

    [SerializeField]
    private TMP_Text mensajeLogin;

    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/reenviar-codigo";

    private string correoUsuario;
    private bool enviando;

    [Serializable]
    private class DatosReenvio
    {
        public string correo;
    }

    [Serializable]
    private class RespuestaServidor
    {
        public string mensaje;
    }

    // Guarda el correo del usuario.
    public void EstablecerCorreo(string correo)
    {
        correoUsuario = correo.Trim();
    }

    // Lo ejecuta el botón Reenviar Código.
    public void Reenviar()
    {
        if (enviando)
            return;

        if (string.IsNullOrWhiteSpace(correoUsuario))
        {
            MostrarMensaje("No hay un correo para verificar.", Color.red);
            return;
        }

        StartCoroutine(EnviarSolicitud(false));
    }

    // Lo utilizaremos desde InicioSesión.
    public void SolicitarDesdeLogin(string correo)
    {
        if (enviando)
            return;

        EstablecerCorreo(correo);

        if (string.IsNullOrWhiteSpace(correoUsuario))
        {
            MostrarMensaje("Ingresa tu correo electrónico.", Color.red);
            return;
        }

        StartCoroutine(EnviarSolicitud(true));
    }

    private IEnumerator EnviarSolicitud(bool abrirPantalla)
    {
        enviando = true;

        MostrarMensaje("Enviando código...", Color.yellow);

        DatosReenvio datos = new DatosReenvio
        {
            correo = correoUsuario
        };

        string json = JsonUtility.ToJson(datos);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(
                Encoding.UTF8.GetBytes(json));

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string mensaje = request.downloadHandler.text;

            try
            {
                RespuestaServidor respuesta = JsonUtility.FromJson<RespuestaServidor>(mensaje);

                if (!string.IsNullOrEmpty(respuesta?.mensaje))
                    mensaje = respuesta.mensaje;
            }
            catch (Exception)
            {
                // Se conserva la respuesta original.
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (string.IsNullOrWhiteSpace(mensaje))
                    mensaje = "No se pudo reenviar el código.";

                if (abrirPantalla)
                {
                    mensajeLogin.text = mensaje;
                    mensajeLogin.color = Color.red;
                }
                else
                {
                    MostrarMensaje(mensaje, Color.red);
                }

                enviando = false;
                yield break;
            }

            if (abrirPantalla)
            {
                codigoManager.AbrirPantalla(correoUsuario);
                pantallaLogin.SetActive(false);
            }

            MostrarMensaje("Código enviado. Revisa tu correo.", Color.green);
        }

        enviando = false;
    }

    private void MostrarMensaje(string mensaje, Color color)
    {
        if (mensajeEstado != null)
        {
            mensajeEstado.text = mensaje;
            mensajeEstado.color = color;
        }

        Debug.Log(mensaje);
    }
}
