using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

public class MenuPausaUI : MonoBehaviour
{
    [SerializeField] private GameObject panelMenuPausa;
    [SerializeField] private GameObject ventanaMenuPausa;
    [SerializeField] private GameObject panelConfirmarSalida;
    [SerializeField] private ClienteSalas clienteSalas;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (panelMenuPausa.activeSelf &&
                panelConfirmarSalida.activeSelf)
            {
                CancelarSalida();
            }
            else
            {
                panelMenuPausa.SetActive(!panelMenuPausa.activeSelf);
            }
        }
    }

    public void Continuar()
    {
        panelMenuPausa.SetActive(false);
    }

    public void MostrarConfirmacion()
    {
        ventanaMenuPausa.SetActive(false);
        panelConfirmarSalida.SetActive(true);
    }

    public void CancelarSalida()
    {
        panelConfirmarSalida.SetActive(false);
        ventanaMenuPausa.SetActive(true);
    }

    private bool abandonandoPartida = false;

    public void ConfirmarSalida()
    {
        if (abandonandoPartida)
            return;

        StartCoroutine(ProcesarSalida());
    }

    private IEnumerator ProcesarSalida()
    {
        if (clienteSalas == null)
        {
            Debug.LogError("No se asignó ClienteSalas.");
            yield break;
        }

        string codigoSala = ContextoPartida.CodigoSala;

        if (string.IsNullOrWhiteSpace(codigoSala))
        {
            Debug.LogError("No hay código de sala activo.");
            yield break;
        }

        abandonandoPartida = true;

        bool salidaExitosa = false;
        string errorSalida = null;

        yield return clienteSalas.AbandonarPartida(
            codigoSala,
            () => salidaExitosa = true,
            error => errorSalida = error
        );

        if (!salidaExitosa)
        {
            abandonandoPartida = false;
            Debug.LogError(
                "No se pudo abandonar la partida: " + errorSalida);
            yield break;
        }

        Debug.Log("Partida abandonada correctamente.");

        // Al cambiar de escena, se destruye ClienteRed
        // y su OnDestroy intenta cerrar el WebSocket.
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby");
    }

}
