
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField correoInput;
    [SerializeField]
    private TMP_InputField passwordInput;
    [SerializeField]
    private ReenviarCodigo reenviarCodigo;
    [SerializeField]
    private TMP_Text mensajeLogin;
    [SerializeField]
    private string escenaJuego= "Lobby";

    public void VerificarCorreo()
    {
        string correo = correoInput.text.Trim();

        if (string.IsNullOrWhiteSpace(correo))
        {
            mensajeLogin.text = "Ingresa tu correo electrónico.";
            mensajeLogin.color = Color.red;
            return;
        }

        reenviarCodigo.SolicitarDesdeLogin(correo);
    }

    public void IniciarSesion()
    {
        string correo = correoInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(correo) ||
            string.IsNullOrWhiteSpace(password))
        {
            mensajeLogin.text = "Ingresa tu correo y contraseña.";
            mensajeLogin.color = Color.red;
            return;
        }

        StartCoroutine(EnviarLogin(correo, password));
        mensajeLogin.text = "Iniciando sesión...";
        mensajeLogin.color = Color.yellow;
    }

    [System.Serializable]
    public class DatosLogin
    {
        public string correo;
        public string password;

        public DatosLogin(string correo, string password)
        {
            this.correo = correo;
            this.password = password;
        }
    }


    private IEnumerator EnviarLogin(string correo, string password)
    {
        DatosLogin datos = new DatosLogin(correo, password);

        string json = JsonUtility.ToJson(datos);

        string url = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/login";

        using (UnityWebRequest request = UnityWebRequest.Post(url, json, "application/json"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonRespuesta = request.downloadHandler.text;

                RespuestaLogin respuesta = JsonUtility.FromJson<RespuestaLogin>(jsonRespuesta);

                SesionUsuario.IdUsuario = respuesta.idUsuario;
                SesionUsuario.NombreUsuario = respuesta.nombreUsuario;
                Debug.Log("Usuario conectado: " + SesionUsuario.NombreUsuario);
                Debug.Log("ID del usuario: " + SesionUsuario.IdUsuario);

                mensajeLogin.text = "¡Bienvenido, " + respuesta.nombreUsuario + "!";
                mensajeLogin.color = Color.green;

                yield return new WaitForSeconds(5f);

                SceneManager.LoadScene(escenaJuego);
            }
            else
            {
                string jsonRespuesta = request.downloadHandler.text;

                if (request.responseCode == 401 && !string.IsNullOrEmpty(jsonRespuesta))
                {
                    RespuestaLogin respuesta = JsonUtility.FromJson<RespuestaLogin>(jsonRespuesta);

                    mensajeLogin.text = respuesta.mensaje;
                }
                else
                {
                    mensajeLogin.text = "No se pudo conectar con el servidor.";
                }

                mensajeLogin.color = Color.red;
            }
        }
    }

}

