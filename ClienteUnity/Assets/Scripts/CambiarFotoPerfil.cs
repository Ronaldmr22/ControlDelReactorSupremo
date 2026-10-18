using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public class SeleccionFotoPerfil : MonoBehaviour
{
    [SerializeField] private Image fotoLobby;
    [SerializeField] private Image fotoPerfil;
    [SerializeField] private Sprite[] fotosPredeterminadas;
    [SerializeField] private GameObject pantallaPerfil;
    [SerializeField] private GameObject pantallaFotosPerfil;

    private Texture2D texturaSubida;
    private Sprite spriteSubido;

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

        try
        {
            byte[] datos = FileBrowserHelpers.ReadBytesFromFile(ruta);
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