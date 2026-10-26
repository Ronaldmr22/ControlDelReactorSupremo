using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class SeleccionFotoPerfil : MonoBehaviour
{
    [SerializeField] private Image fotoLobby;
    [SerializeField] private Image fotoPerfil;
    [SerializeField] private Sprite[] fotosPredeterminadas;
    [SerializeField] private GameObject pantallaPerfil;
    [SerializeField] private GameObject pantallaFotosPerfil;
    [System.Serializable]
    private class FotoPredeterminadaDto
    {
        public int idUsuario;
        public int numeroFoto;
    }

    private Texture2D texturaSubida;
    private Sprite spriteSubido;
    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/foto-perfil";

    public void ElegirFoto(int indice)
    {
        if (indice < 0 || indice >= fotosPredeterminadas.Length || indice >= 5)
        {
            Debug.LogWarning("Foto predeterminada inválida.");
            return;
        }

        if (SesionUsuario.IdUsuario <= 0)
        {
            Debug.LogWarning("No hay sesión iniciada.");
            return;
        }

        AplicarFoto(fotosPredeterminadas[indice]);
        LiberarFotoSubida();

        int numeroFoto = indice + 1;

        StartCoroutine(GuardarFotoPredeterminada(numeroFoto));
    }

    public void SubirFoto()
    {
        // Muestra únicamente imágenes PNG y JPG.
        FileBrowser.SetFilters(false,
            new FileBrowser.Filter("Imagenes", ".png", ".jpg", ".jpeg"));

        FileBrowser.ShowLoadDialog(
            rutas => CargarFoto(rutas[0]),
            null,
            FileBrowser.PickMode.Files,
            false,
            null,
            null,
            "Elige tu foto",
            "Seleccionar"
        );
    }

    private void CargarFoto(string ruta)
    {
        Texture2D nuevaTextura = null;
        byte[] datos;

        try
        {
            datos = FileBrowserHelpers.ReadBytesFromFile(ruta);
            nuevaTextura = new Texture2D(2, 2);

            if (!nuevaTextura.LoadImage(datos))
            {
                Destroy(nuevaTextura);
                Debug.LogWarning("No se pudo abrir la imagen.");
                return;
            }
        }
        catch (System.Exception)
        {
            if (nuevaTextura != null)
                Destroy(nuevaTextura);

            Debug.LogWarning("No se pudo leer el archivo.");
            return;
        }

        LiberarFotoSubida();

        texturaSubida = nuevaTextura;
        spriteSubido = Sprite.Create(
            texturaSubida,
            new Rect(0, 0, texturaSubida.width, texturaSubida.height),
            new Vector2(0.5f, 0.5f)
        );

        AplicarFoto(spriteSubido);
        if (SesionUsuario.IdUsuario > 0)
        {
            StartCoroutine(EnviarFotoAzure(datos, System.IO.Path.GetFileName(ruta)));
        }
        else
        {
            Debug.LogWarning("No hay un usuario con sesión iniciada.");
        }
    }

    private void AplicarFoto(Sprite foto)
    {
        // Actualiza ambas imágenes y regresa al perfil.
        fotoLobby.sprite = foto;
        fotoPerfil.sprite = foto;

        fotoLobby.preserveAspect = true;
        fotoPerfil.preserveAspect = true;

        pantallaPerfil.SetActive(true);
        pantallaFotosPerfil.SetActive(false);
    }

    private IEnumerator EnviarFotoAzure(byte[] datos, string nombreArchivo)
    {
        WWWForm formulario = new WWWForm();

        formulario.AddField("idUsuario", SesionUsuario.IdUsuario);
        formulario.AddBinaryData("foto", datos, nombreArchivo, "image/png");

        using (UnityWebRequest request = UnityWebRequest.Post(API_URL, formulario))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Fotografía guardada en Azure: " +
                          request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error al subir fotografía: " +
                               request.downloadHandler.text);
            }
        }
    }

    private void LiberarFotoSubida()
    {
        if (spriteSubido != null)
            Destroy(spriteSubido);

        if (texturaSubida != null)
            Destroy(texturaSubida);

        spriteSubido = null;
        texturaSubida = null;
    }

    private void OnDestroy()
    {
        LiberarFotoSubida();
    }

    public void CargarFotoGuardada(string urlFoto)
    {
        if (string.IsNullOrEmpty(urlFoto))
        {
            Debug.Log("El usuario no tiene foto guardada.");
            return;
        }

        // Foto predeterminada guardada en PostgreSQL.
        if (urlFoto.StartsWith("predeterminada:"))
        {
            string numero = urlFoto.Substring("predeterminada:".Length);

            if (int.TryParse(numero, out int idFoto))
            {
                int indice = idFoto - 1;

                if (indice >= 0 &&
                    indice < fotosPredeterminadas.Length &&
                    indice < 5)
                {
                    LiberarFotoSubida();

                    fotoLobby.sprite = fotosPredeterminadas[indice];
                    fotoPerfil.sprite = fotosPredeterminadas[indice];

                    fotoLobby.preserveAspect = true;
                    fotoPerfil.preserveAspect = true;

                    Debug.Log("Foto predeterminada cargada: " + idFoto);
                    return;
                }
            }

            Debug.LogWarning("Foto predeterminada inválida: " + urlFoto);
            return;
        }

        // Foto personalizada subida desde la computadora.
        if (System.Uri.TryCreate(urlFoto, System.UriKind.Absolute, out var uri)
            && uri.Scheme == System.Uri.UriSchemeHttps
            && uri.Host == "control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net"
            && uri.AbsolutePath.StartsWith("/api/fotos-perfil/"))
        {
            StartCoroutine(DescargarFoto(urlFoto));
        }
        else
        {
            Debug.LogWarning("URL de foto no permitida.");
        }
    }

    private IEnumerator DescargarFoto(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al descargar foto: " + request.error);
                yield break;
            }

            Texture2D nuevaTextura =
                DownloadHandlerTexture.GetContent(request);

            LiberarFotoSubida();

            texturaSubida = nuevaTextura;

            spriteSubido = Sprite.Create(
                texturaSubida,
                new Rect(0, 0, texturaSubida.width, texturaSubida.height),
                new Vector2(0.5f, 0.5f)
            );

            fotoLobby.sprite = spriteSubido;
            fotoPerfil.sprite = spriteSubido;

            fotoLobby.preserveAspect = true;
            fotoPerfil.preserveAspect = true;

            Debug.Log("Foto de perfil cargada desde Azure.");
        }
    }
    private IEnumerator GuardarFotoPredeterminada(int numeroFoto)
    {
        FotoPredeterminadaDto datos = new FotoPredeterminadaDto
        {
            idUsuario = SesionUsuario.IdUsuario,
            numeroFoto = numeroFoto
        };

        string json = JsonUtility.ToJson(datos);

        string url =
            API_URL + "/predeterminada";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(
                Encoding.UTF8.GetBytes(json));

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Foto predeterminada " + numeroFoto +
                          " guardada correctamente en Azure.");
            }
            else
            {
                Debug.LogError("Error al guardar foto predeterminada: " +
                               request.responseCode + " - " +
                               request.downloadHandler.text);
            }
        }
    }
}