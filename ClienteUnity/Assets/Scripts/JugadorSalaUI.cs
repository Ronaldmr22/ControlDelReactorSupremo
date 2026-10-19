using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class JugadorSalaUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nombre;
    [SerializeField] private TMP_Text estado;
    [SerializeField] private Image fotoPerfil;
    [SerializeField] private Image personaje;
    [SerializeField] private Sprite fotoPredeterminada;
    [SerializeField] private Sprite[] personajes;

    private Coroutine descarga;
    private Sprite fotoDescargada;
    private Texture2D texturaDescargada;

    public void Mostrar(ParticipanteSala participante, bool esCreador)
    {
        LimpiarFoto();

        nombre.text = participante.nombreUsuario;
        estado.text = esCreador ? "Creador de la sala" : "En la sala";

        fotoPerfil.gameObject.SetActive(true);
        fotoPerfil.sprite = fotoPredeterminada;
        fotoPerfil.preserveAspect = true;

        // Los avatares de la base de datos van del 1 al 5.
        int indice = participante.avatarSeleccionado - 1;
        bool avatarValido = indice >= 0 && indice < personajes.Length;

        personaje.gameObject.SetActive(avatarValido);
        personaje.sprite = avatarValido ? personajes[indice] : null;
        personaje.preserveAspect = true;

        if (!string.IsNullOrWhiteSpace(participante.fotoPerfil))
            descarga = StartCoroutine(CargarFoto(participante.fotoPerfil));
    }

    public void MostrarVacio()
    {
        LimpiarFoto();

        nombre.text = "Espacio disponible";
        estado.text = "Esperando jugador...";
        fotoPerfil.sprite = fotoPredeterminada;
        fotoPerfil.gameObject.SetActive(false);
        personaje.gameObject.SetActive(false);
    }

    private IEnumerator CargarFoto(string url)
    {
        using var solicitud = UnityWebRequestTexture.GetTexture(url);
        solicitud.timeout = 15;

        yield return solicitud.SendWebRequest();

        if (solicitud.result == UnityWebRequest.Result.Success)
        {
            texturaDescargada = DownloadHandlerTexture.GetContent(solicitud);
            texturaDescargada.filterMode = FilterMode.Point;

            fotoDescargada = Sprite.Create(
                texturaDescargada,
                new Rect(0, 0,
                    texturaDescargada.width, texturaDescargada.height),
                new Vector2(0.5f, 0.5f));

            fotoPerfil.sprite = fotoDescargada;
        }

        descarga = null;
    }

    private void LimpiarFoto()
    {
        if (descarga != null)
        {
            StopCoroutine(descarga);
            descarga = null;
        }

        fotoPerfil.sprite = fotoPredeterminada;

        // Libera las imágenes descargadas que ya no se utilizan.
        if (fotoDescargada != null)
            Destroy(fotoDescargada);

        if (texturaDescargada != null)
            Destroy(texturaDescargada);

        fotoDescargada = null;
        texturaDescargada = null;
    }

    private void OnDestroy()
    {
        LimpiarFoto();
    }
}


