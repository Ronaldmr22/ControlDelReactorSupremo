using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class RegisterManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField usernameInputField;
    [SerializeField]
    private TMP_InputField emailInputField;
    [SerializeField]
    private TMP_InputField passwordInputField;
    [SerializeField]
    private TMP_InputField confirmPasswordInputField;
    [SerializeField]
    private TMP_Text mensajeRegistro;
    [SerializeField] 
    private CodigoManager codigoManager;

    [SerializeField] 
    private GameObject panelRegistro;

    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/registro";

    [Serializable]
    private class RegistroUsuarioDto
    {
        public string nombreUsuario;
        public string correo;
        public string password;
        public string confirmarPassword;
    }

    [Serializable]
    private class RespuestaServidor
    {
        public string mensaje;
    }

    public void RegistrarUsuario()
    {
        string usuario = usernameInputField.text.Trim();
        string correo = emailInputField.text.Trim();
        string password = passwordInputField.text;
        string confirmar = confirmPasswordInputField.text;

        if (string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmar))
        {
            MostrarMensaje("Debes completar todos los campos.", Color.yellow);
            return;
        }

        if (password != confirmar)
        {
            MostrarMensaje("Las contraseñas no coinciden.", Color.red);
            return;
        }

        RegistroUsuarioDto datos = new RegistroUsuarioDto
        {
            nombreUsuario = usuario,
            correo = correo,
            password = password,
            confirmarPassword = confirmar
        };

        StartCoroutine(EnviarRegistro(datos));
    }


    private IEnumerator EnviarRegistro(RegistroUsuarioDto datos)
    {
        MostrarMensaje("Registrando usuario...", Color.yellow);

        string json = JsonUtility.ToJson(datos);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
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
                string mensajeError = !string.IsNullOrEmpty(respuesta?.mensaje)? respuesta.mensaje: "No se pudo completar el registro.";

                MostrarMensaje(mensajeError, Color.red);
                Debug.LogError(mensajeError);
                yield break;
            }

            string mensajeExito =!string.IsNullOrEmpty(respuesta?.mensaje)? respuesta.mensaje: "Usuario registrado correctamente.";

            MostrarMensaje(mensajeExito, Color.green);
            Debug.Log(mensajeExito);
            codigoManager.AbrirPantalla(datos.correo);
            panelRegistro.SetActive(false);
        }
    }

    private void MostrarMensaje(string mensaje, Color color)
    {
        mensajeRegistro.text = mensaje;
        mensajeRegistro.color = color;
    }
}
