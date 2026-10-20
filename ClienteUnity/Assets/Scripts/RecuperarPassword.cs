
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class RecuperacionPasswordManager : MonoBehaviour
{
    [Header("Campos")]
    [SerializeField] private TMP_InputField inputCorreo;
    [SerializeField] private TMP_InputField inputCodigo;
    [SerializeField] private TMP_InputField inputNuevaPassword;
    [SerializeField] private TMP_InputField inputConfirmarPassword;

    [Header("Mensajes")]
    [SerializeField] private TMP_Text mensajeError;
    [SerializeField] private TMP_Text mensajeErrorCodigo;
    [SerializeField] private TMP_Text mensajeErrorPassword;

    [Header("Pantallas")]
    [SerializeField] private GameObject pantallaRecuperar;
    [SerializeField] private GameObject pantallaCodigo;
    [SerializeField] private GameObject pantallaNuevaPassword;
    [SerializeField] private GameObject pantallaLogin;

    private const string BASE_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/";

    private string correoRecuperacion;
    private string codigoRecuperacion;
    private bool procesando = false;

    [Serializable]
    private class SolicitudCorreo
    {
        public string correo;
    }

    [Serializable]
    private class SolicitudCodigo
    {
        public string correo;
        public string codigo;
    }

    [Serializable]
    private class SolicitudCambio
    {
        public string correo;
        public string codigo;
        public string nuevaPassword;
    }

    [Serializable]
    private class RespuestaServidor
    {
        public string mensaje;
    }

    // Abrir la pantalla de recuperación desde el login.
    public void AbrirPantallaRecuperar()
    {
        pantallaLogin.SetActive(false);
        pantallaRecuperar.SetActive(true);
        MostrarMensaje(mensajeError, "", Color.white);
    }

    // PASO 1: Solicitar el código por correo.
    public void SolicitarCodigo()
    {
        if (procesando) return;

        string correo = inputCorreo.text.Trim();

        if (string.IsNullOrWhiteSpace(correo))
        {
            MostrarMensaje(mensajeError, "Debes ingresar tu correo.", Color.red);
            return;
        }

        SolicitudCorreo datos = new SolicitudCorreo
        {
            correo = correo
        };

        StartCoroutine(EnviarSolicitud(
            "solicitar-recuperacion",
            JsonUtility.ToJson(datos),
            mensajeError,
            "Enviando código de recuperación...",
            "Si el correo está registrado, recibirás un código.",
            () =>
            {
                correoRecuperacion = correo;
                pantallaRecuperar.SetActive(false);
                pantallaCodigo.SetActive(true);
            }
        ));
    }

    // PASO 2: Verificar el código recibido.
    public void VerificarCodigo()
    {
        if (procesando) return;

        string codigo = inputCodigo.text.Trim();

        if (codigo.Length != 6)
        {
            MostrarMensaje(mensajeErrorCodigo,
                "El código debe tener 6 dígitos.", Color.red);
            return;
        }

        SolicitudCodigo datos = new SolicitudCodigo
        {
            correo = correoRecuperacion,
            codigo = codigo
        };

        StartCoroutine(EnviarSolicitud(
            "verificar-codigo-recuperacion",
            JsonUtility.ToJson(datos),
            mensajeErrorCodigo,
            "Verificando código...",
            "Código verificado correctamente.",
            () =>
            {
                codigoRecuperacion = codigo;
                pantallaCodigo.SetActive(false);
                pantallaNuevaPassword.SetActive(true);
            }
        ));
    }

    // PASO 3: Restablecer la contraseña.
    public void CambiarPassword()
    {
        if (procesando) return;

        string nueva = inputNuevaPassword.text;
        string confirmar = inputConfirmarPassword.text;

        if (nueva.Length < 8)
        {
            MostrarMensaje(mensajeErrorPassword,
                "La contraseña debe tener al menos 8 caracteres.", Color.red);
            return;
        }

        if (nueva != confirmar)
        {
            MostrarMensaje(mensajeErrorPassword,
                "Las contraseñas no coinciden.", Color.red);
            return;
        }

        SolicitudCambio datos = new SolicitudCambio
        {
            correo = correoRecuperacion,
            codigo = codigoRecuperacion,
            nuevaPassword = nueva
        };

        StartCoroutine(EnviarSolicitud(
            "cambiar-password",
            JsonUtility.ToJson(datos),
            mensajeErrorPassword,
            "Restableciendo contraseña...",
            "¡Contraseña restablecida correctamente!\nRegresando al inicio de sesión...",
            () =>
            {
                // Limpiar los datos temporales.
                correoRecuperacion = null;
                codigoRecuperacion = null;

                inputCodigo.text = "";
                inputNuevaPassword.text = "";
                inputConfirmarPassword.text = "";

                pantallaNuevaPassword.SetActive(false);
                pantallaLogin.SetActive(true);
            }
        ));
    }

    // Método compartido para enviar solicitudes HTTP.
    private IEnumerator EnviarSolicitud(
        string endpoint,
        string json,
        TMP_Text textoMensaje,
        string mensajeProceso,
        string mensajeExito,
        Action alCompletar)
    {
        procesando = true;

        MostrarMensaje(textoMensaje, mensajeProceso, Color.yellow);

        using (UnityWebRequest request =
            new UnityWebRequest(BASE_URL + endpoint, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string respuestaJson = request.downloadHandler.text;
            RespuestaServidor respuesta = null;

            if (!string.IsNullOrEmpty(respuestaJson))
            {
                try
                {
                    respuesta = JsonUtility.FromJson<RespuestaServidor>(respuestaJson);
                }
                catch (Exception)
                {
                    Debug.LogWarning("No se pudo interpretar la respuesta JSON.");
                }
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string mensajeErrorServidor =
                    !string.IsNullOrEmpty(respuesta?.mensaje)
                    ? respuesta.mensaje
                    : "No se pudo completar la operación.";

                MostrarMensaje(textoMensaje, mensajeErrorServidor, Color.red);
                Debug.LogError(mensajeErrorServidor);

                procesando = false;
                yield break;
            }

            string mensajeFinal =
                !string.IsNullOrEmpty(mensajeExito)
                ? mensajeExito
                : respuesta?.mensaje ?? "Operación exitosa.";

            MostrarMensaje(textoMensaje, mensajeFinal, Color.green);
            Debug.Log(mensajeFinal);

            // Esperar para que el jugador pueda leer el mensaje.
            yield return new WaitForSecondsRealtime(2f);

            alCompletar?.Invoke();
        }

        procesando = false;
    }

    // Mostrar mensajes con colores, igual que en RegisterManager.
    private void MostrarMensaje(TMP_Text texto, string mensaje, Color color)
    {
        texto.text = mensaje;
        texto.color = color;
    }
}
