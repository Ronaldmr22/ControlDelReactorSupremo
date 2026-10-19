using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using System.Collections;
using UnityEngine.Networking;

public class SeleccionFotoPerfil : MonoBehaviour
{
    [SerializeField] private Image fotoLobby;
    [SerializeField] private Image fotoPerfil;
    [SerializeField] private Sprite[] fotosPredeterminadas;
    [SerializeField] private GameObject pantallaPerfil;
    [SerializeField] private GameObject pantallaFotosPerfil;

    private Texture2D texturaSubida;
    private Sprite spriteSubido;
    private const string API_URL = "https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/usuarios/foto-perfil";

    public void ElegirFoto(int indice)
    {
        AplicarFoto(fotosPredeterminadas[indice]);
        LiberarFotoSubida();
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
}