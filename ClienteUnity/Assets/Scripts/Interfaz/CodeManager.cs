using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CodigoManager : MonoBehaviour
{
    [SerializeField] 
    private TMP_InputField inputCodigo;
    [SerializeField] 
    private TMP_Text mensajeError;
    [SerializeField] 
    private string escenaJuego = "Lobby";
    [SerializeField] 
    private ReenviarCodigo reenviarCodigo;

    private string correoUsuario;
    private bool verificando;

    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/verificar-correo";

    [Serializable]
    private class DatosVerificacion
    {
        public string correo;
        public string codigo;
    }

    [Serializable]
    private class RespuestaServidor
    {
        public string mensaje;
    }

    public void AbrirPantalla(string correo)
    {
        correoUsuario = correo;
        reenviarCodigo.EstablecerCorreo(correo);
        inputCodigo.text = "";
        mensajeError.text = "";
        gameObject.SetActive(true);
    }

    public void VerificarCodigo()
    {
        if (verificando)
            return;

        string codigo = inputCodigo.text.Trim();

        if (string.IsNullOrWhiteSpace(codigo))
        {
            MostrarMensaje("Ingresa el código.", Color.red);
            return;
        }

        StartCoroutine(EnviarCodigo(codigo));
    }

    private IEnumerator EnviarCodigo(string codigo)
    {
        verificando = true;
        MostrarMensaje("Verificando código...", Color.yellow);

        DatosVerificacion datos = new DatosVerificacion
        {
            correo = correoUsuario,
            codigo = codigo
        };

        string json = JsonUtility.ToJson(datos);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

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
                // Conservamos la respuesta original.
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                MostrarMensaje(mensaje, Color.red);
                verificando = false;
                yield break;
            }

            MostrarMensaje("¡Correo verificado correctamente!", Color.green);

            yield return new WaitForSeconds(1f);

            SceneManager.LoadScene(escenaJuego);
        }

        verificando = false;
    }

    private void MostrarMensaje(string mensaje, Color color)
    {
        mensajeError.text = mensaje;
        mensajeError.color = color;
    }
}
