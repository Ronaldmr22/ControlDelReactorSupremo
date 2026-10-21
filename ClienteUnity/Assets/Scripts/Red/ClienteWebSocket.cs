
using System;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

public class ClienteWebSocket : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private string urlServidor = "";

    private WebSocket websocket;
    private bool cerrando;

    public bool EstaConectado =>
        websocket != null &&
        websocket.State == WebSocketState.Open;

    public event Action<string> MensajeRecibido;
    public event Action ConexionAbierta;
    public event Action ConexionCerrada;
    public event Action<string> ErrorConexion;

    private async void Start()
    {
        Debug.Log("Intentando conectar al servidor WebSocket...");
        await Conectar();
    }

    public async Task Conectar()
    {
        if (string.IsNullOrWhiteSpace(urlServidor))
        {
            Debug.LogError("No se configuró la URL del servidor WebSocket.");
            return;
        }

        if (websocket != null)
        {
            Debug.LogWarning("Ya existe una conexión WebSocket.");
            return;
        }

        cerrando = false;
        websocket = new WebSocket(urlServidor);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket conectado.");
            ConexionAbierta?.Invoke();

            if (string.IsNullOrWhiteSpace(ContextoPartida.CodigoSala))
            {
                Debug.LogError("No hay código de sala para el WebSocket.");
                return;
            }

            string codigoSala = ContextoPartida.CodigoSala;
            string nombreJugador = SesionUsuario.NombreUsuario;

            string mensaje = JsonUtility.ToJson(new SuscripcionSala
            {
                type = "subscribe_room",
                roomCode = codigoSala,
                playerName = nombreJugador
            });

            EnviarMensaje(mensaje);
            Debug.Log("Suscripción enviada a la sala: " + codigoSala);
        };

        websocket.OnMessage += (bytes) =>
        {
            string mensaje = System.Text.Encoding.UTF8.GetString(bytes);

            Debug.Log("Mensaje recibido: " + mensaje);
            MensajeRecibido?.Invoke(mensaje);
        };

        websocket.OnError += (error) =>
        {
            Debug.LogError("Error WebSocket: " + error);
            ErrorConexion?.Invoke(error);
        };

        websocket.OnClose += (codigo) =>
        {
            Debug.Log("WebSocket cerrado. Código: " + codigo);
            ConexionCerrada?.Invoke();
        };

        try
        {
            await websocket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "No se pudo conectar al WebSocket: " + ex.Message
            );

            ErrorConexion?.Invoke(ex.Message);
        }
    }

    public async void EnviarMensaje(string mensaje)
    {
        if (!EstaConectado)
        {
            Debug.LogWarning(
                "No se puede enviar el mensaje: WebSocket desconectado."
            );
            return;
        }

        try
        {
            await websocket.SendText(mensaje);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Error al enviar mensaje: " + ex.Message
            );

            ErrorConexion?.Invoke(ex.Message);
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private async void OnDestroy()
    {
        if (cerrando)
            return;

        cerrando = true;

        if (websocket != null)
        {
            try
            {
                await websocket.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "Error al cerrar WebSocket: " + ex.Message
                );
            }

            websocket = null;
        }
    }

    [Serializable]
    private class SuscripcionSala
    {
        public string type;
        public string roomCode;
        public string playerName;
    }

}
