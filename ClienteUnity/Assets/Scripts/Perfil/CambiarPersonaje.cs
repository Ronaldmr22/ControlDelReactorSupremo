using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class SeleccionPersonaje : MonoBehaviour
{
    [SerializeField] private Image imagenPersonajeLobby;
    [SerializeField] private Sprite[] personajes;
    [SerializeField] private GameObject fondoLobby;
    [SerializeField] private GameObject pantallaPersonajes;

    private const string API_URL =
        "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/avatar";

    [System.Serializable]
    private class AvatarDto
    {
        public int idUsuario;
        public int avatarSeleccionado;
    }

    public void ElegirPersonaje(int indice)
    {
        if (indice < 0 || indice >= personajes.Length || indice >= 5)
        {
            Debug.LogWarning("Personaje inválido.");
            return;
        }

        if (SesionUsuario.IdUsuario <= 0)
        {
            Debug.LogWarning("No hay sesión iniciada.");
            return;
        }

        // Mostrar personaje en el lobby.
        imagenPersonajeLobby.sprite = personajes[indice];

        fondoLobby.SetActive(true);
        pantallaPersonajes.SetActive(false);

        // Convertir índice 0-4 a ID 1-5.
        int idAvatar = indice + 1;

        // Guardar avatar en PostgreSQL.
        StartCoroutine(GuardarAvatar(idAvatar));
    }

    private IEnumerator GuardarAvatar(int idAvatar)
    {
        AvatarDto datos = new AvatarDto
        {
            idUsuario = SesionUsuario.IdUsuario,
            avatarSeleccionado = idAvatar
        };

        string json = JsonUtility.ToJson(datos);

        using (UnityWebRequest request =
            new UnityWebRequest(API_URL, "PUT"))
        {
            byte[] cuerpo = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(cuerpo);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader(
                "Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Avatar " + idAvatar +
                          " guardado correctamente.");
            }
            else
            {
                Debug.LogError("Error al guardar avatar: " +
                               request.downloadHandler.text);
            }
        }
    }

    public void MostrarAvatarGuardado(int idAvatar)
    {
        int indice = idAvatar - 1;

        if (indice < 0 || indice >= personajes.Length)
        {
            Debug.LogWarning("Avatar guardado inválido: " + idAvatar);
            return;
        }

        imagenPersonajeLobby.sprite = personajes[indice];
        imagenPersonajeLobby.preserveAspect = true;

        Debug.Log("Avatar cargado desde Azure: " + idAvatar);
    }
}