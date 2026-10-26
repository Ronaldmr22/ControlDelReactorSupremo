using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class CargarPerfilLobby : MonoBehaviour
{
    [SerializeField] private TMP_Text txtNombreUsuario;
    [SerializeField] private TMP_Text txtUsuarioPerfil;
    [SerializeField] private TMP_Text txtCorreoPerfil;
    [SerializeField] private SeleccionPersonaje gestorPersonajes;
    [SerializeField] private SeleccionFotoPerfil gestorFotoPerfil;

    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/";

    [System.Serializable]
    private class PerfilDto
    {
        public int idUsuario;
        public string nombreUsuario;
        public string correo;
        public bool correoVerificado;
        public int avatarSeleccionado;
        public string urlFotoPerfil;
    }

    private void Start()
    {
        if (SesionUsuario.IdUsuario <= 0)
        {
            Debug.LogWarning("No hay usuario con sesión iniciada.");
            return;
        }

        StartCoroutine(CargarPerfil());
    }

    private IEnumerator CargarPerfil()
    {
        string url = API_URL + SesionUsuario.IdUsuario + "/perfil";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Error al cargar perfil: " + request.error);
                yield break;
            }

            PerfilDto perfil =
                JsonUtility.FromJson<PerfilDto>(
                    request.downloadHandler.text);

            if (perfil == null)
            {
                Debug.LogError("No se pudo interpretar el perfil.");
                yield break;
            }

            txtNombreUsuario.text = perfil.nombreUsuario;
            if (txtUsuarioPerfil != null)
            {
                txtUsuarioPerfil.text = perfil.nombreUsuario;
            }

            if (txtCorreoPerfil != null)
            {
                txtCorreoPerfil.text = perfil.correo;
            }

            if (perfil.avatarSeleccionado >= 1)
            {
                gestorPersonajes.MostrarAvatarGuardado(
                    perfil.avatarSeleccionado);
            }
            if (gestorFotoPerfil != null)
            {
                gestorFotoPerfil.CargarFotoGuardada(perfil.urlFotoPerfil);
            }

            Debug.Log("Perfil cargado: " + perfil.nombreUsuario);
        }
    }
}